using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using JavaScriptEngineSwitcher.Extensions.MsDependencyInjection;
using React.AspNet;
using JavaScriptEngineSwitcher.ChakraCore;
using FluentValidation.AspNetCore;
using Hellang.Middleware.ProblemDetails;
using Szlem.Persistence.EF;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Szlem.AspNetCore.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using NodaTime.Serialization.SystemTextJson;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Szlem.SharedKernel;
using Szlem.Engine.Infrastructure;
using Szlem.Domain;
using Microsoft.AspNetCore.Authentication;
using Szlem.Recruitment;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Szlem.Engine;
using NodaTime.Serialization.JsonNet;
using Microsoft.Extensions.Options;
using HtmlTags;
using Hangfire;

namespace Szlem.AspNetCore
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var domainMarker = typeof(Szlem.Domain.Marker);
            var engineMarker = typeof(Szlem.Engine.Marker);

            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo(Configuration["Culture"]);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            #region MVC
            var mvcBuilder = services
                .AddRazorPages()
                .AddMvcOptions(options => {
                    options.ModelBinderProviders.Insert(0, new Common.Infrastructure.ModelBinders.DelimiterSeparatedStringModelBinderProvider());
                    options.ModelBinderProviders.Insert(0, new Common.Infrastructure.ModelBinders.PhoneNumberModelBinderProvider());
                    options.ModelBinderProviders.Insert(0, new Common.Infrastructure.ModelBinders.EmailAddressModelBinderProvider());
                    options.ModelBinderProviders.Insert(0, new Common.Infrastructure.ModelBinders.NodaTimeModelBinderProvider());
                    options.ModelBinderProviders.Insert(0, new Common.Infrastructure.ModelBinders.SmartEnumModelBinderProvider());
                    options.ModelMetadataDetailsProviders.Add(new Infrastructure.ZonedDateTimeMetadataProvider());
                    options.Filters.Add<Szlem.AspNetCore.Common.Infrastructure.SerializeModelStateFilter>();
                })
                .AddRazorOptions(options => { })
                .AddJsonOptions(x => x.JsonSerializerOptions.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb))
                .AddNewtonsoftJson(jsonOptions => {
                    jsonOptions.SerializerSettings.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
                    jsonOptions.SerializerSettings.Converters.Add(new Szlem.SharedKernel.MaybeConverters.NewtonsoftJsonConverter());
                })
                .AddFluentValidation(config => {
                    config.RegisterValidatorsFromAssemblyContaining<Startup>();
                    config.RegisterValidatorsFromAssemblyContaining<Szlem.Models.Marker>();
                    config.RegisterValidatorsFromAssemblyContaining<Szlem.Engine.Marker>();
                    config.RegisterValidatorsFromAssemblyContaining<Szlem.Domain.Marker>();
                    config.ImplicitlyValidateChildProperties = true;
                    config.ConfigureClientsideValidation(clientConfig => {
                        clientConfig.Add(typeof(Szlem.Domain.EmailAddress.Validator), (context, rule, validator) => new Infrastructure.ClientValidators.EmailClientValidator(rule, validator));
                        clientConfig.Add(typeof(Infrastructure.ClientValidators.PhoneNumberClientValidator), (context, rule, validator) => new Infrastructure.ClientValidators.PhoneNumberClientValidator(rule, validator));
                    });
                });

            services.AddTransient(sp => sp.GetRequiredService<IOptions<MvcNewtonsoftJsonOptions>>().Value.SerializerSettings);
            services.AddTransient(sp => sp.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions);

            if (Environment.IsDevelopment())
                mvcBuilder = mvcBuilder.AddRazorRuntimeCompilation();

            var razorPageConventionBuilders = System.Reflection.Assembly.GetExecutingAssembly().ExportedTypes
                .Where(x => x.IsClass && x.Implements<Infrastructure.IRazorPagesConventionBuilder>()).ToList();

            mvcBuilder.AddRazorPagesOptions(options => {
                foreach (var builder in razorPageConventionBuilders)
                    (Activator.CreateInstance(builder) as Infrastructure.IRazorPagesConventionBuilder).Configure(options.Conventions);
            });
            #endregion

            services.AddHtmlTags();

            services.AddProblemDetails(options => {
                bool IsApiRequest(HttpContext httpContext) => httpContext.Request.Path.StartsWithSegments($"/{Routes.Root}");
                options.IsProblem = (httpContext) => httpContext.Response.StatusCode >= 400 && httpContext.Response.StatusCode < 600 && IsApiRequest(httpContext);
                options.ShouldLogUnhandledException = (httpContext, ex, problemDetails) =>
                    IsApiRequest(httpContext);
                options.Map<Szlem.Engine.Exceptions.AuthorizationFailureException>((httpContext, ex) => {
                    if (IsApiRequest(httpContext) == false)
                        return null;
                    if (httpContext.User.Identity.IsAuthenticated)
                        return new StatusCodeProblemDetails(StatusCodes.Status403Forbidden) { Detail = ex.Message };
                    else
                        return new StatusCodeProblemDetails(StatusCodes.Status401Unauthorized) { Detail = ex.Message };
                });
                options.Map<Szlem.Domain.Exceptions.ValidationException>((httpContext, ex) => {
                    if (IsApiRequest(httpContext) == false)
                        return null;
                    return new ValidationProblemDetails(ex.Failures.ToDictionary(x => x.PropertyName, x => x.Errors.ToArray())) { Title = ex.Message, Detail = ex.Message, Status = StatusCodes.Status400BadRequest };
                });
                options.Map<Exception>((httpContext, ex) => {
                    if (IsApiRequest(httpContext) == false)
                        return null;
                    return options.MapStatusCode(httpContext);
                });
            });

            var emailOptions = new EmailOptions();
            Configuration.Bind(nameof(EmailOptions), emailOptions);
            services.AddSingleton(emailOptions);

            if (EmailAddress.TryParse(emailOptions.Username, out var _) == false)
                throw new ApplicationException($"option {nameof(EmailOptions)}:{nameof(EmailOptions.Username)} is either not set, unreadable, or not a valid {nameof(EmailAddress)}");

            #region identity, authentication & authorization

            services
                .AddIdentity<Szlem.Models.Users.ApplicationUser, Szlem.Models.Users.ApplicationIdentityRole>(
                    options => {
                        if (Environment.IsDevelopment())
                        {
                            options.Password.RequireDigit = options.Password.RequireLowercase = options.Password.RequireUppercase = options.Password.RequireNonAlphanumeric = false;
                            options.Password.RequiredLength = 1;
                        }
                        options.User.RequireUniqueEmail = true;
						options.SignIn.RequireConfirmedAccount = options.SignIn.RequireConfirmedEmail = true;
                    })
                .AddDefaultUI()
                .AddEFStores()
                .AddDefaultTokenProviders();

            var jwtOptions = new Options.JwtOptions();
            Configuration.Bind(nameof(Options.JwtOptions), jwtOptions);
            services.AddSingleton(jwtOptions);

            if (jwtOptions.TokenLifetime == default || jwtOptions.TokenLifetime.TotalSeconds == 0)
                throw new ApplicationException($"option {nameof(Options.JwtOptions)}:{nameof(jwtOptions.TokenLifetime)} is either not set, unreadable or set to zero. {nameof(Options.JwtOptions)}:{nameof(jwtOptions.TokenLifetime)} setting must be set to positive value.");
            if (string.IsNullOrWhiteSpace(jwtOptions.Secret))
                throw new ApplicationException($"option {nameof(Options.JwtOptions)}:{nameof(jwtOptions.Secret)} is not set. Before running the application, set {nameof(Options.JwtOptions)}:{nameof(jwtOptions.Secret)} to a securely-generated secret value.");

            var tokenValidationParams = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtOptions.Secret)),
                ValidateIssuer = false,
                ValidateAudience = false,
                RequireExpirationTime = false,
                ValidateLifetime = true
            };
            services.AddSingleton(tokenValidationParams);

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
                })
                .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.SaveToken = true;
                    options.TokenValidationParameters = tokenValidationParams;
                })
                .AddGoogle(options =>
                {
                    options.ClientId = Configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = Configuration["Authentication:Google:ClientSecret"];
                })
                .AddFacebook(options =>
                {
                    options.AppId = Configuration["Authentication:Facebook:AppId"];
                    options.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                });

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
            });

            services.AddSingleton<IAuthorizationHandler, OrAuthorizationRequirementHandler>();
            services.Add(new ServiceDescriptor(typeof(Lazy<IAuthorizationHandlerProvider>), sp => new Lazy<IAuthorizationHandlerProvider>(() => sp.GetRequiredService<IAuthorizationHandlerProvider>()), ServiceLifetime.Singleton));

            #endregion

            services.AddEFUnitOfWork(options =>
                options
                    .UseSqlite(
                        System.Environment.ExpandEnvironmentVariables(Configuration.GetConnectionString("SQLite")),
                        x => x.MigrationsAssembly(typeof(Szlem.Persistence.EF.AppDbContext).Assembly.GetName().Name))
                    .EnableSensitiveDataLogging(Environment.IsDevelopment())
                    .EnableDetailedErrors(Environment.IsDevelopment()));

            #region swagger
            services.AddSwaggerGen(config =>
            {
                config.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo() { Version = "v1" });
                config.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new Microsoft.OpenApi.Models.OpenApiSecurityScheme()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
                });
                config.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement()
                {
                    { new Microsoft.OpenApi.Models.OpenApiSecurityScheme() { Reference = new Microsoft.OpenApi.Models.OpenApiReference() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
                });
                config.CustomSchemaIds(type => type.FullName);
            });
            #endregion

            services.AddSzlemApplication(Configuration);

            services.AddScoped<Engine.Interfaces.IUserAccessor, Infrastructure.UserAccessor>();
            services.AddScoped<Infrastructure.IIdentityService, Infrastructure.IdentityService>();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
            services.AddScoped<IUrlHelper>(sp =>
            {
                var actionContext = sp.GetService<IActionContextAccessor>().ActionContext;
                return new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(actionContext);
            });

            ValidateRequestPermissions(services);
        }

        private void ValidateRequestPermissions(IServiceCollection services)
        {
            var requestHandlerInterfaces = new[] { typeof(MediatR.IRequestHandler<,>), typeof(MediatR.IRequestHandler<>) };
            var requestHandlerTypes = services.Where(x => x.ServiceType.IsGenericType && requestHandlerInterfaces.Contains(x.ServiceType.GetGenericTypeDefinition()));
            var requestTypes = requestHandlerTypes.Select(x => new { request = x.ServiceType.GenericTypeArguments[0], attributes = x.ServiceType.GenericTypeArguments[0].CustomAttributes });

            var authorizationAttributes = new[] { typeof(AuthorizeAttribute), typeof(AllowAnonymousAttribute) };
            var unauthorizedRequests = requestTypes.Where(x => x.attributes.None(attr => authorizationAttributes.Contains(attr.AttributeType))).ToArray();

            if (unauthorizedRequests.Any())
                throw new ApplicationException(
                    $"All request classes must have proper attibutes associated with them.\n" +
                    $"If a request should be restricted to a certain group of users, add [Authorize(AuthorizationPolicies.MyPolicy)] attribute to it. " +
                    $"If it should be available to any authenticated user, use plain [Authorize] attribute. " +
                    $"If it should be freely available to everyone, use [AllowAnonymous] attribute.\n" +
                    $"The following classes have missing attributes:\n" +
                    string.Join("\n", unauthorizedRequests.Select(x => x.request.FullName)));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IAuthorizationService authService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseProblemDetails();

            if (Environment.IsDevelopment() == false)
                app.UseHttpsRedirection();

            app.UseForwardedHeaders(
                new ForwardedHeadersOptions { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto }
            );

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseMiddleware<Middleware.SwaggerAuthorizationMiddleware>();
            app.UseSwagger();
            app.UseSwaggerUI(settings =>
            {
                settings.SwaggerEndpoint("/swagger/v1/swagger.json", "Versioned API v1.0");
            });

            app.UseHangfireDashboard(options: new DashboardOptions() { Authorization = new[] {
                new Szlem.AspNetCore.Common.Infrastructure.HangfireDashboardAuthorizationFilter(authService) } });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet(".well-known/acme-challenge/{id}", async (httpRequest) =>
                {
                    var id = httpRequest.GetRouteValue("id") as string;
                    var file = System.IO.Path.Combine(env.ContentRootPath, "..", ".well-known", "acme-challenge", id);
                    await httpRequest.Response.SendFileAsync(file);
                });
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapHangfireDashboard();
            });
        }
    }
}

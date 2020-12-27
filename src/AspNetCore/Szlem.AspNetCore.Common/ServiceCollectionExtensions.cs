using EventFlow.DependencyInjection.Extensions;
using EventFlow.Extensions;
using EventFlow.SQLite.Extensions;
using Hangfire;
using Hangfire.Storage.SQLite;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Engine;
using Szlem.Engine.Behaviors;
using Szlem.Recruitment.Impl;
using Szlem.SchoolManagement.Impl;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Common
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSzlemApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDefaultEnginePipeline();
            services.AddScoped<Engine.Infrastructure.IRequestAuthorizationAnalyzer, Engine.Infrastructure.RequestAuthorizationAnalyzer>();

            var mockableClock = new Engine.Infrastructure.MockableClock(new NodaTime.ZonedClock(NodaTime.SystemClock.Instance, Szlem.Domain.Consts.MainTimezone, NodaTime.CalendarSystem.Iso));
            services.AddSingleton(mockableClock);
            services.AddSingleton<NodaTime.IClock>(mockableClock);

            services.AddScoped<Szlem.Recruitment.DependentServices.IEditionProvider, Szlem.Persistence.EF.Services.EditionProvider>();
            services.AddScoped<Szlem.Recruitment.DependentServices.ITrainerProvider, Szlem.Persistence.EF.Services.TrainerProvider>();
            services.AddSingleton<Szlem.Engine.Infrastructure.IFluidTemplateRenderer, Szlem.Engine.Infrastructure.FluidTemplateRenderer>();

            services.AddAsyncInitialization();

            services.AddSzlemAuthorization();

            #region EventFlow
            services.AddEventFlow(options => options
                .ConfigureJson(jsonConfig => jsonConfig
                    .AddSingleValueObjects()
                    .Configure(serializer => serializer
                        .ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)
                        .ConstructorHandling = Newtonsoft.Json.ConstructorHandling.AllowNonPublicDefaultConstructor))
                .Configure(config => config.ThrowSubscriberExceptions = true)
                .ConfigureSzlemSchoolManagement()
                .UseLibLog(LibLogProviders.NLog)
                .UseSQLiteEventStore()
                    .ConfigureSQLite(EventFlow.SQLite.Connections.SQLiteConfiguration.New
                        .SetConnectionString(Environment.ExpandEnvironmentVariables(configuration["EventFlow:SQLiteEventStorePath"])))
            );
            services.Decorate<EventFlow.ReadStores.IReadModelPopulator, Szlem.Engine.Infrastructure.ReadModelPopulator>();
            services.AddEventInitializer();
            #endregion

            #region Hangfire
            services.AddHangfire(options => options
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseSQLiteStorage(Environment.ExpandEnvironmentVariables(configuration["Hangfire:ConnectionString"]))
                .UseRecommendedSerializerSettings()
                .UseNLogLogProvider()
            );
            services.AddHangfireServer();
            #endregion

            services.RegisterValidatorsFromAssembliesContaining(
                typeof(ISzlemEngine), typeof(Szlem.Models.Marker), typeof(Szlem.Engine.Marker), typeof(Szlem.Domain.Marker));

            services.AddOptions();

            var emailOptionsSection = configuration.GetSection("EmailOptions");
            services.Configure<Engine.Infrastructure.EmailOptions>(emailOptionsSection);
            services.AddScoped<Engine.Infrastructure.IEmailService, Engine.Infrastructure.MailKitEmailService>();

            var recruitmentConfigSection = configuration.GetSection("Recruitment");
            services.Configure<Szlem.Recruitment.Config>(recruitmentConfigSection);
            var recruitmentConfig = recruitmentConfigSection.Get<Szlem.Recruitment.Config>();

            return services
                .AddMediatR(typeof(Szlem.Engine.Marker).Assembly)
                .AddSzlemRecruitmentImpl(recruitmentConfig)
                .AddSzlemSchoolManagementImpl();
        }

        public static IServiceCollection AddDefaultEnginePipeline(this IServiceCollection services)
        {
            return services
                .AddScoped<ISzlemEngine, SzlemEngine>()
                .AddMediatR(typeof(ISzlemEngine), typeof(Szlem.Persistence.EF.AppDbContext), typeof(Szlem.Persistence.NHibernate.Marker))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>))
                .AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        }

        public static IServiceCollection AddSzlemAuthorization(this IServiceCollection services, Action<AuthorizationOptions> options = null)
        {
            return services.AddAuthorizationCore(opt => {
                    opt.ConfigureSzlemPolicies();
                    options?.Invoke(opt);
                })
                .AddSingleton<IAuthorizationHandler, Szlem.Engine.Infrastructure.IsOwnerAuthorizationHandler>();
        }

        [Obsolete("niebezpieczne, nie używać")]
        public static IServiceCollection AddSzlemApplicationWithoutAuthorization(this IServiceCollection services)
        {
            services.AddScoped<ISzlemEngine, SzlemEngine>();
            services.AddMediatR(typeof(ISzlemEngine), typeof(Szlem.Persistence.EF.AppDbContext), typeof(Szlem.Persistence.NHibernate.Marker));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddScoped<Engine.Infrastructure.IRequestAuthorizationAnalyzer, Engine.Infrastructure.DummyRequestAuthorizationAnalyzer>();
            services.AddScoped<NodaTime.IClock>(sp => new NodaTime.ZonedClock(NodaTime.SystemClock.Instance, Szlem.Domain.Consts.MainTimezone, NodaTime.CalendarSystem.Iso));
            services.AddScoped<Szlem.Recruitment.DependentServices.IEditionProvider, Szlem.Persistence.EF.Services.EditionProvider>();
            services.AddScoped<Szlem.Recruitment.DependentServices.ITrainerProvider, Szlem.Persistence.EF.Services.TrainerProvider>();

            services.RegisterValidatorsFromAssembliesContaining(typeof(ISzlemEngine), typeof(Szlem.Models.Marker), typeof(Szlem.Engine.Marker), typeof(Szlem.Domain.Marker));

            return services;
        }
    }
}

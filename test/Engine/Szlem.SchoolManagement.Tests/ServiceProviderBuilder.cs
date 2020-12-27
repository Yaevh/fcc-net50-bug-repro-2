using Extensions.Hosting.AsyncInitialization;
using EventFlow;
using EventFlow.Aggregates;
using EventFlow.AspNetCore.Extensions;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Szlem.AspNetCore.Common;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Engine.Infrastructure;
using Szlem.Recruitment.Impl;
using Szlem.Domain;
using Microsoft.AspNetCore.Identity;
using Szlem.Models.Users;
using NLog.Extensions.Logging;
using Microsoft.Extensions.Logging;
using EventFlow.Logs;
using Szlem.SchoolManagement.Impl;

namespace Szlem.SchoolManagement.Tests
{
    class ServiceProviderBuilder
    {
        public IServiceProvider BuildServiceProvider(Action<IServiceCollection> registerServices = null)
        {
            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<SharedKernel.ISzlemEngine, Engine.SzlemEngine>();
            services.AddMediatR(typeof(Szlem.Recruitment.Impl.Marker));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Engine.Behaviors.ValidationBehavior<,>));
            services.AddSingleton<FluentValidation.IValidatorFactory, ServiceProviderValidatorFactory>();
            services.AddSingleton<NodaTime.IClock>(NodaTime.SystemClock.Instance);
            services.AddSingleton(Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(MockBehavior.Strict));
            services.AddSingleton(Moq.Mock.Of<Szlem.Engine.Infrastructure.IRequestAuthorizationAnalyzer>(Moq.MockBehavior.Strict));
            services.AddSzlemSchoolManagementImpl();
            services.AddSingleton<IEmailService, SucceedingEmailService>();

            services.AddSzlemAuthorization();

            services.AddEventInitializer();

            var domainAssembly = typeof(Szlem.Recruitment.Impl.Marker).Assembly;

            services.AddEventFlow(options => options
                .AddAspNetCore()
                .ConfigureJson(jsonConfig => jsonConfig
                    .AddSingleValueObjects()
                    .Configure(serializer => serializer
                        .ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)
                        .ConstructorHandling = Newtonsoft.Json.ConstructorHandling.AllowNonPublicDefaultConstructor))
                .ConfigureSzlemSchoolManagement()
                .Configure(config => config.ThrowSubscriberExceptions = true)
            );

            registerServices?.Invoke(services);

            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                var initializers = scope.ServiceProvider.GetRequiredService<IEnumerable<IAsyncInitializer>>();
                foreach (var initializer in initializers)
                    initializer.InitializeAsync().Wait();
            }

            foreach (var hostedService in sp.GetServices<Microsoft.Extensions.Hosting.IHostedService>())
                hostedService.StartAsync(System.Threading.CancellationToken.None).Wait();

            return sp;
        }

        internal class ServiceProviderValidatorFactory : FluentValidation.ValidatorFactoryBase
        {
            private readonly IServiceProvider _serviceProvider;

            public ServiceProviderValidatorFactory(IServiceProvider serviceProvider)
            {
                _serviceProvider = serviceProvider;
            }

            public override FluentValidation.IValidator CreateInstance(Type validatorType)
            {
                return (FluentValidation.IValidator)_serviceProvider.GetService(validatorType);
            }
        }
    }
}

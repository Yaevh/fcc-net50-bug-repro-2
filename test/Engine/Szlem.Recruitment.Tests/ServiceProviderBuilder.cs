using EventFlow.AspNetCore.Extensions;
using EventFlow.DependencyInjection.Extensions;
using EventFlow.Extensions;
using Extensions.Hosting.AsyncInitialization;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NLog.Extensions.Logging;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Engine.Infrastructure;
using Szlem.Models.Users;
using Szlem.Recruitment.Impl;

namespace Szlem.Recruitment.Tests
{
    class ServiceProviderBuilder
    {
        public IServiceProvider BuildServiceProvider(Action<IServiceCollection> registerServices = null)
        {
            var config = new Szlem.Recruitment.Config()
            {
                DbConnectionString = "DataSource=:memory:",
                KeepDbSessionOpen = true,
                GreetingEmail = new Config.EmailMessageConfig() {
                    Body = "Dziękujemy za zgłoszenie do projektu LEM",
                    Subject = "Dziękujemy za zgłoszenie do projektu LEM",
                    IsBodyHtml = false
                },
                TrainingReminderEmail = new Config.EmailMessageConfig()
                {
                    Body = "Przypominamy o jutrzejszym szkoleniu",
                    Subject = "Przypominamy o jutrzejszym szkoleniu",
                    IsBodyHtml = false
                }
            };

            var services = new ServiceCollection();

            services.AddLogging();
            services.AddSingleton<SharedKernel.ISzlemEngine, Engine.SzlemEngine>();
            services.AddMediatR(typeof(Szlem.Recruitment.Impl.Marker));
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(Engine.Behaviors.ValidationBehavior<,>));
            services.AddSingleton<FluentValidation.IValidatorFactory, ServiceProviderValidatorFactory>();
            services.AddSingleton<NodaTime.IClock>(NodaTime.SystemClock.Instance);
            services.AddSingleton(Mock.Of<IUserClaimsPrincipalFactory<ApplicationUser>>(MockBehavior.Strict));
            services.AddSzlemRecruitmentImpl(config);
            services.AddSingleton(Moq.Mock.Of<DependentServices.IEditionProvider>(Moq.MockBehavior.Strict));
            services.AddSingleton(Moq.Mock.Of<DependentServices.ITrainerProvider>(Moq.MockBehavior.Strict));
            services.AddSingleton(Moq.Mock.Of<Szlem.Engine.Infrastructure.IRequestAuthorizationAnalyzer>(Moq.MockBehavior.Strict));
            services.AddSingleton<IFluidTemplateRenderer, FluidTemplateRenderer>();
            services.AddSingleton<IEmailService, SucceedingEmailService>();
            
            services.AddEventInitializer();

            var mockRunner = new Mock<FluentMigrator.Runner.IMigrationRunner>();
            mockRunner.Setup(runner => runner.MigrateUp());
            services.AddSingleton<FluentMigrator.Runner.IMigrationRunner>(mockRunner.Object);

            var domainAssembly = typeof(Szlem.Recruitment.Impl.Marker).Assembly;

            services.AddEventFlow(options => options
                .AddAspNetCore()
                .ConfigureJson(jsonConfig => jsonConfig
                    .AddSingleValueObjects()
                    .Configure(serializer => serializer
                        .ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb)
                        .ConstructorHandling = Newtonsoft.Json.ConstructorHandling.AllowNonPublicDefaultConstructor))
                .Configure(config => config.ThrowSubscriberExceptions = true)
            );

            registerServices?.Invoke(services);

            var sp = services.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<DbSessionProvider>().UnsafeCleanAndRebuildSchema();
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

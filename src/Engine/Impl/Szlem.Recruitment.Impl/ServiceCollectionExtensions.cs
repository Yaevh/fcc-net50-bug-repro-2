using FluentMigrator.Runner;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NHibernate.Dialect;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Models.Users;

namespace Szlem.Recruitment.Impl
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddSzlemRecruitmentImpl(this IServiceCollection services, Config config)
        {
            var thisMarkerType = typeof(Marker);

            ValidateConfig(config);

            services.AddOptions<Config>().Configure(options => {
                options.AllowBreakingMigrations = config.AllowBreakingMigrations;
                options.DbConnectionString = config.DbConnectionString;
                options.KeepDbSessionOpen = config.KeepDbSessionOpen;
                options.GreetingEmail = config.GreetingEmail;
            });

            services.AddAsyncInitializer<Initializer>();

            foreach (var entry in FluentValidation.AssemblyScanner.FindValidatorsInAssemblyContaining(typeof(Szlem.Recruitment.Marker)))
                services.AddTransient(entry.InterfaceType, entry.ValidatorType);
            foreach (var entry in new FluentValidation.AssemblyScanner(typeof(Szlem.Recruitment.Impl.Marker).Assembly.DefinedTypes))
                services.AddTransient(entry.InterfaceType, entry.ValidatorType);
            
            var sqliteConfig = FluentNHibernate.Cfg.Db.SQLiteConfiguration.Standard
                .ConnectionString(config.DbConnectionString)
                .ShowSql();

            if (config.KeepDbSessionOpen)
                sqliteConfig = sqliteConfig.InMemory().Provider<Szlem.Persistence.NHibernate.AlwaysOpenConnectionProvider>();

            services.Configure<FluentMigrator.Runner.Initialization.RunnerOptions>(options => options.AllowBreakingChange = config.AllowBreakingMigrations);

            return services
                .AddMediatR(thisMarkerType)
                .AddTransient<Enrollments.GetSubmissionsQueryHandler>()
                .AddEventsFrom(thisMarkerType.Assembly)
                .AddEventSubscribersFrom(thisMarkerType.Assembly)
                .Decorate<IUserClaimsPrincipalFactory<ApplicationUser>, Authorization.CandidateUserClaimPrincipalFactory>()
                .AddSingleton(container => new DbSessionProvider(sqliteConfig, container.GetRequiredService<ILogger<DbSessionProvider>>()))
                .AddScoped<Repositories.ICampaignRepository, Repositories.CampaignRepository>()
                .AddScoped<Repositories.ITrainingRepository, Repositories.TrainingRepository>()
                .AddSingleton<Enrollments.IEnrollmentRepository, Enrollments.EnrollmentRepository>()
                .AddScoped<EventFlow.ReadStores.IReadStoreManager, Enrollments.ReadStoreManager>()
                .AddSingleton<IAuthorizationHandler, Authorization.OwningCandidateAuthorizationHandler>()
                .AddFluentMigratorCore()
                    .ConfigureRunner(rb => rb
                        .AddSQLite()
                        .WithGlobalConnectionString(config.DbConnectionString)
                        // Define the assembly containing the migrations
                        .ScanIn(thisMarkerType.Assembly).For.Migrations())
                    // Enable logging to console in the FluentMigrator way
                    .AddLogging(lb => lb.AddFluentMigratorConsole());
        }

        private static void ValidateConfig(Config config)
        {
            if (config is null)
                throw new ArgumentNullException(nameof(config));

            var validationResult = new Config.Validator().Validate(config);
            if (validationResult.IsValid)
                return;
            throw new ArgumentException($"Recruitment Config validation errors: {string.Join(", ", validationResult.Errors)}");
        }
    }
}

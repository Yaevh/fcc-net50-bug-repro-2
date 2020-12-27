using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.AspNetCore.Common;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Models.Users;
using Szlem.Persistence.EF;
using Szlem.Recruitment.Impl;

namespace Szlem.Engine.Tests
{
    public class EngineContextBuilder
    {
        public Task<EngineContext> BuildContext(string sqliteDbPath, Action<IServiceCollection> configureServices = null)
        {
            var services = new ServiceCollection();

            var domainMarker = typeof(Szlem.Domain.Marker);
            var engineMarker = typeof(Szlem.Engine.Marker);

            var guid = Guid.NewGuid();
            var dbFileName = GenerateSqliteDbName(sqliteDbPath, guid);
            System.IO.File.Copy(sqliteDbPath, dbFileName);

            var recruitmentConfig = new Szlem.Recruitment.Config() { DbConnectionString = "DataSource=:memory:", KeepDbSessionOpen = true };

            services.AddEFUnitOfWork(options =>
                options
                    .UseSqlite(
                        $"Data Source={dbFileName}",
                        x => x.MigrationsAssembly(typeof(Szlem.Persistence.EF.AppDbContext).Assembly.GetName().Name))
                    .EnableSensitiveDataLogging(true));

#pragma warning disable CS0618 // for testing purposes only
            services.AddSzlemApplicationWithoutAuthorization();
#pragma warning restore CS0618 // Type or member is obsolete
            services.AddSzlemRecruitmentImpl(recruitmentConfig);
            services.AddScoped<Engine.Interfaces.IUserAccessor, UserAccessor>();

            configureServices?.Invoke(services);

            var serviceProvider = services.BuildServiceProvider();

            serviceProvider.GetRequiredService<DbSessionProvider>().UnsafeCleanAndRebuildSchema();

            return Task.FromResult(new EngineContext(guid, dbFileName, serviceProvider));
        }

        public Task<EngineContext> BuildContextFromSolutionRoot(string sqliteDbPath, Action<IServiceCollection> configureServices = null)
        {
            return BuildContextFromProjectRoot($"../../{sqliteDbPath}", configureServices);
        }

        public Task<EngineContext> BuildContextFromProjectRoot(string sqliteDbPath, Action<IServiceCollection> configureServices = null)
        {
            return BuildContext($"../../../{sqliteDbPath}", configureServices);
        }

        public Task<EngineContext> BuildContextFromTestDatabase(Action<IServiceCollection> configureServices = null)
        {
            return BuildContextFromProjectRoot("test.sqlite", configureServices);
        }


        private string GenerateSqliteDbName(string source, Guid guid)
        {
            var fileNamePart = System.IO.Path.GetFileNameWithoutExtension(source);
            var extensionPart = System.IO.Path.GetExtension(source);
            var directory = System.IO.Path.GetDirectoryName(source);

            return $"{directory}{System.IO.Path.DirectorySeparatorChar}{fileNamePart}-{guid.ToString()}{extensionPart}";
        }

        private class UserAccessor : Interfaces.IUserAccessor
        {
            public Task<ClaimsPrincipal> GetClaimsPrincipal()
            {
                return Task.FromResult(new ClaimsPrincipal());
            }

            public Task<ApplicationUser> GetUser()
            {
                return Task.FromResult(new ApplicationUser());
            }
        }
    }
}

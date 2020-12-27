using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Szlem.AspNetCore;
using Szlem.DependencyInjection.AspNetCore;
using Xunit.Abstractions;

namespace Szlem.IntegrationTestsInfrastructure
{
    public class TestFixture : IDisposable
    {
        private bool _initialized = false;

        private ITestOutputHelper _output;
        private Action<IServiceCollection> _services = services => { };
        private Func<IServiceProvider, Task> _initializer = services => Task.CompletedTask;
        private Func<IServiceProvider, Task> _testSetup = services => Task.CompletedTask;

        private WebApplicationFactory<Startup> _appFactory;

        private readonly NodaTime.Instant _testStartInstant = NodaTime.SystemClock.Instance.GetCurrentInstant();
        public NodaTime.Instant TestStart => _testStartInstant;

        public IServiceProvider Services => _appFactory.Services;


        #region test setup
        public TestFixture ConfigureOutput(ITestOutputHelper output)
        {
            ThrowIfInitialized();
            _output = output;
            return this;
        }

        public TestFixture ConfigureServices(Action<IServiceCollection> services)
        {
            ThrowIfInitialized();
            Guard.Against.Null(services, nameof(services));
            _services = services;
            return this;
        }

        public TestFixture ConfigureInitialization(Func<IServiceProvider, Task> initializer)
        {
            ThrowIfInitialized();
            Guard.Against.Null(initializer, nameof(initializer));
            _initializer = initializer;
            return this;
        }

        public TestFixture BeforeEachTest(Func<IServiceProvider, Task> testSetup)
        {
            ThrowIfInitialized();
            Guard.Against.Null(testSetup, nameof(testSetup));
            _testSetup = testSetup;
            return this;
        }
        #endregion


        public async Task<HttpClient> BuildClient()
        {
            if (_appFactory == null)
            {
                var appFactoryStopwatch = System.Diagnostics.Stopwatch.StartNew();
                _appFactory = new WebApplicationFactory<Startup>()
                    .WithWebHostBuilder(config =>
                    {
                        config.UseContentRoot("../../../../../../config");
                        config.ConfigureServices(ConfigureServicesInternal);
                    });
                await InitializeInternal(_appFactory.Services);

                appFactoryStopwatch.Stop();
                _output.WriteLine($"{nameof(WebApplicationFactory<Startup>)} built in {appFactoryStopwatch.Elapsed}");
            }

            await _testSetup.Invoke(_appFactory.Services);

            var clientStopwatch = System.Diagnostics.Stopwatch.StartNew();
            var client = _appFactory.CreateClient();
            clientStopwatch.Stop();
            _output.WriteLine($"{nameof(HttpClient)} built in {clientStopwatch.Elapsed}");

            return client;
        }


        private void ThrowIfInitialized()
        {
            if (_initialized)
                throw new InvalidOperationException("Fixture already initialized. Remember to configure the fixture only before initialization");
        }


        protected virtual void ConfigureServicesInternal(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<EventFlow.EventStores.IEventPersistence, EventFlow.EventStores.InMemory.InMemoryEventPersistence>();

            var dbName = $"{this.GetType().FullName}-{Guid.NewGuid()}";
            services.RemoveAll<Persistence.EF.AppDbContext>();
            services.RemoveAll<DbContextOptions<Persistence.EF.AppDbContext>>();
            services.AddDbContext<Persistence.EF.AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            _services.Invoke(services);
        }

        private async Task InitializeInternal(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var initializers = scope.ServiceProvider.GetServices<Extensions.Hosting.AsyncInitialization.IAsyncInitializer>();

                foreach (var initializer in initializers)
                    await initializer.InitializeAsync();

                await Initialize(scope.ServiceProvider);

                await _initializer.Invoke(scope.ServiceProvider);
            }
        }

        protected virtual Task Initialize(IServiceProvider serviceProvider) => Task.CompletedTask;



        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            _appFactory.Dispose();
        }
    }
}

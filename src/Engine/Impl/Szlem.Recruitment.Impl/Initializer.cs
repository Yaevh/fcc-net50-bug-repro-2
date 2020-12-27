using Extensions.Hosting.AsyncInitialization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.Recruitment.Impl
{
    internal class Initializer : IAsyncInitializer
    {
        private readonly ILogger<Initializer> _logger;
        private readonly FluentMigrator.Runner.IMigrationRunner _migrationRunner;
        private readonly EventFlow.Configuration.Bootstraps.IBootstrapper _bootstrapper;
        private readonly EventFlow.ReadStores.IReadModelPopulator _readModelPopulator;
        private readonly Enrollments.IEnrollmentRepository _repository;
        
        public Initializer(
            ILogger<Initializer> logger,
            FluentMigrator.Runner.IMigrationRunner migrationRunner,
            EventFlow.Configuration.Bootstraps.IBootstrapper bootstrapper,
            EventFlow.ReadStores.IReadModelPopulator readModelPopulator,
            Enrollments.IEnrollmentRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _migrationRunner = migrationRunner ?? throw new ArgumentNullException(nameof(migrationRunner));
            _bootstrapper = bootstrapper ?? throw new ArgumentNullException(nameof(bootstrapper));
            _readModelPopulator = readModelPopulator ?? throw new ArgumentNullException(nameof(readModelPopulator));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task InitializeAsync()
        {
            _migrationRunner.MigrateUp();
            await _bootstrapper.StartAsync(CancellationToken.None);
            await _readModelPopulator.PopulateAsync<Enrollments.EnrollmentReadModel>(CancellationToken.None);
            _logger.LogInformation($"loaded {_repository.Query().Count()} enrollments");
        }
    }
}

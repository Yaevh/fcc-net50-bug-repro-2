using Extensions.Hosting.AsyncInitialization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.SchoolManagement.Impl
{
    internal class Initializer : IAsyncInitializer
    {
        private readonly EventFlow.Configuration.Bootstraps.IBootstrapper _eventFlowBootstapper;
        private readonly EventFlow.ReadStores.IReadModelPopulator _readModelPopulator;

        public Initializer(EventFlow.Configuration.Bootstraps.IBootstrapper eventFlowBootstrapper, EventFlow.ReadStores.IReadModelPopulator readModelPopulator)
        {
            _eventFlowBootstapper = eventFlowBootstrapper ?? throw new ArgumentNullException(nameof(eventFlowBootstrapper));
            _readModelPopulator = readModelPopulator ?? throw new ArgumentNullException(nameof(readModelPopulator));
        }

        public async Task InitializeAsync()
        {
            await _eventFlowBootstapper.StartAsync(CancellationToken.None);
            await _readModelPopulator.PopulateAsync<SchoolReadModel>(CancellationToken.None);
        }
    }
}

using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.Engine.Infrastructure
{
    public class ReadModelPopulator : IReadModelPopulator
    {
        private readonly IReadModelPopulator _impl;
        private readonly ILog _log;
        private readonly IEventFlowConfiguration _configuration;
        private readonly IEventStore _eventStore;
        private readonly IResolver _resolver;

        public ReadModelPopulator(IReadModelPopulator impl, ILog log, IEventFlowConfiguration configuration, IEventStore eventStore, IResolver resolver)
        {
            _impl = impl ?? throw new ArgumentNullException(nameof(impl));
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public Task DeleteAsync(string id, Type readModelType, CancellationToken cancellationToken)
        {
            return _impl.DeleteAsync(id, readModelType, cancellationToken);
        }

        public Task PopulateAsync<TReadModel>(
            CancellationToken cancellationToken)
            where TReadModel : class, IReadModel
        {
            return PopulateAsync(typeof(TReadModel), cancellationToken);
        }

        public async Task PopulateAsync(
            Type readModelType,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var readStoreManagers = ResolveReadStoreManagers(readModelType);

            _log.Verbose(() => string.Format(
                "Read model '{0}' is interested in all aggregate events",
                readModelType.PrettyPrint()));

            long totalEvents = 0;
            long relevantEvents = 0;
            var currentPosition = GlobalPosition.Start;

            while (true)
            {
                _log.Verbose(() => string.Format(
                    "Loading events starting from {0} and the next {1} for populating '{2}'",
                    currentPosition,
                    _configuration.PopulateReadModelEventPageSize,
                    readModelType.PrettyPrint()));
                var allEventsPage = await _eventStore.LoadAllEventsAsync(
                    currentPosition,
                    _configuration.PopulateReadModelEventPageSize,
                    cancellationToken)
                    .ConfigureAwait(false);
                totalEvents += allEventsPage.DomainEvents.Count;
                currentPosition = allEventsPage.NextGlobalPosition;

                if (!allEventsPage.DomainEvents.Any())
                {
                    _log.Verbose(() => $"No more events in event store, stopping population of read model '{readModelType.PrettyPrint()}'");
                    break;
                }

                var domainEvents = allEventsPage.DomainEvents
                    .ToList();
                relevantEvents += domainEvents.Count;

                if (!domainEvents.Any())
                {
                    continue;
                }

                var applyTasks = readStoreManagers
                    .Select(m => m.UpdateReadStoresAsync(domainEvents, cancellationToken));
                await Task.WhenAll(applyTasks).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _log.Information(
                "Population of read model '{0}' took {1:0.###} seconds, in which {2} events was loaded and {3} was relevant",
                readModelType.PrettyPrint(),
                stopwatch.Elapsed.TotalSeconds,
                totalEvents,
                relevantEvents);
        }

        public Task PurgeAsync(Type readModelType, CancellationToken cancellationToken)
        {
            return _impl.PurgeAsync(readModelType, cancellationToken);
        }

        Task IReadModelPopulator.PurgeAsync<TReadModel>(CancellationToken cancellationToken)
        {
            return _impl.PurgeAsync<TReadModel>(cancellationToken);
        }

        private IReadOnlyCollection<IReadModelStore> ResolveReadModelStores(
            Type readModelType)
        {
            var readModelStoreType = typeof(IReadModelStore<>).MakeGenericType(readModelType);
            var readModelStores = _resolver.ResolveAll(readModelStoreType)
                .Select(s => (IReadModelStore)s)
                .ToList();

            if (!readModelStores.Any())
            {
                throw new ArgumentException($"Could not find any read stores for read model '{readModelType.PrettyPrint()}'");
            }

            return readModelStores;
        }

        private IReadOnlyCollection<IReadStoreManager> ResolveReadStoreManagers(
            Type readModelType)
        {
            var readStoreManagers = _resolver.Resolve<IEnumerable<IReadStoreManager>>()
                .Where(m => m.ReadModelType == readModelType)
                .ToList();

            if (!readStoreManagers.Any())
            {
                throw new ArgumentException($"Did not find any read store managers for read model type '{readModelType.PrettyPrint()}'");
            }

            return readStoreManagers;
        }
    }
}

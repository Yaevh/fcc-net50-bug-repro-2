using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Szlem.Recruitment.Tests
{
    public class MockAggregateStore<TActualAggregate, TActualIdentity> : IAggregateStore
        where TActualAggregate : IAggregateRoot<TActualIdentity>
        where TActualIdentity : IIdentity
    {
        private readonly IDomainEventFactory _domainEventFactory;
        private readonly TActualAggregate _aggregate;
        public MockAggregateStore(TActualAggregate aggregate)
        {
            _domainEventFactory = new DomainEventFactory();
            _aggregate = aggregate ?? throw new ArgumentNullException(nameof(aggregate));
        }

        public Task<TAggregate> LoadAsync<TAggregate, TIdentity>(TIdentity id, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            EnsureValidAggregate(id);

            return Task.FromResult((TAggregate)(object)_aggregate);
        }

        public Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate, TIdentity>(TAggregate aggregate, ISourceId sourceId, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            throw new NotSupportedException();
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> UpdateAsync<TAggregate, TIdentity>(TIdentity id, ISourceId sourceId, Func<TAggregate, CancellationToken, Task> updateAggregate, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            EnsureValidAggregate(id);
            var aggregate = (TAggregate)(object)_aggregate;
            
            await updateAggregate.Invoke(aggregate, cancellationToken);

            return aggregate.UncommittedEvents
                .Select((x, i) => _domainEventFactory.Create(x.AggregateEvent, new Metadata() { Timestamp = DateTimeOffset.Now }, id.Value, aggregate.Version + i))
                .ToArray();
        }

        public async Task<IAggregateUpdateResult<TExecutionResult>> UpdateAsync<TAggregate, TIdentity, TExecutionResult>(TIdentity id, ISourceId sourceId, Func<TAggregate, CancellationToken, Task<TExecutionResult>> updateAggregate, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            EnsureValidAggregate(id);

            var aggregate = (TAggregate)(object)_aggregate;

            var result = await updateAggregate.Invoke(aggregate, cancellationToken);

            var domainEvents = aggregate.UncommittedEvents
                .Select((x, i) => _domainEventFactory.Create(x.AggregateEvent, new Metadata() { Timestamp = DateTimeOffset.Now }, id.Value, aggregate.Version + i))
                .ToArray();

            return new AggregateUpdateResult<TExecutionResult>(result, domainEvents);
        }

        private void EnsureValidAggregate<TIdentity>(TIdentity id) where TIdentity : IIdentity
        {
            if (id.Value != _aggregate.Id.Value)
                throw new NotSupportedException($"invalid aggregate requested");
        }


        internal class AggregateUpdateResult<TExecutionResult> : IAggregateUpdateResult<TExecutionResult>
            where TExecutionResult : IExecutionResult
        {
            public TExecutionResult Result { get; }
            public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

            public AggregateUpdateResult(
                TExecutionResult result,
                IReadOnlyCollection<IDomainEvent> domainEvents)
            {
                Result = result;
                DomainEvents = domainEvents;
            }
        }
    }
}

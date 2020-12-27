using CSharpFunctionalExtensions;
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Aggregates
{
    public static class AggregateStoreExtensions
    {
        public static async Task<IAggregateUpdateResultEx<TResult>> UpdateAsync<TAggregate, TIdentity, TResult>(
            this IAggregateStore aggregateStore,
            TIdentity id, ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task<TResult>> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IResult
        {
            var updateResult = await aggregateStore.UpdateAsync<TAggregate, TIdentity, ExecutionResultWrapper<TResult>>(
                id, sourceId,
                async (aggregate, token) => new ExecutionResultWrapper<TResult>(await updateAggregate(aggregate, token)),
                cancellationToken
            );
            return new AggregateUpdateResult<TResult>(updateResult.Result.Impl, updateResult.DomainEvents);
        }

        public static async Task<IAggregateUpdateResultEx<TResult>> Update<TAggregate, TIdentity, TResult>(
            this IAggregateStore aggregateStore,
            TIdentity id, ISourceId sourceId,
            Func<TAggregate, TResult> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IResult
        {
            var updateResult = await aggregateStore.UpdateAsync<TAggregate, TIdentity, ExecutionResultWrapper<TResult>>(
                id, sourceId,
                (aggregate, token) => Task.FromResult(new ExecutionResultWrapper<TResult>(updateAggregate(aggregate))),
                cancellationToken
            );
            return new AggregateUpdateResult<TResult>(updateResult.Result.Impl, updateResult.DomainEvents);
        }

        public static async Task<IAggregateUpdateResultEx<TResult>> Update<TAggregate, TIdentity, TResult>(
            this IAggregateStore aggregateStore,
            TIdentity id, ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task<TResult>> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TResult : IResult
        {
            var updateResult = await aggregateStore.UpdateAsync<TAggregate, TIdentity, ExecutionResultWrapper<TResult>>(
                id, sourceId,
                async (aggregate, token) => new ExecutionResultWrapper<TResult>(await updateAggregate(aggregate, token)),
                cancellationToken
            );
            return new AggregateUpdateResult<TResult>(updateResult.Result.Impl, updateResult.DomainEvents);
        }


        internal class ExecutionResultWrapper<TResult> : IExecutionResult where TResult : IResult
        {
            public TResult Impl { get; }

            public ExecutionResultWrapper(TResult impl)
            {
                Impl = impl;
            }

            public bool IsSuccess => Impl.IsSuccess;
        }

        private class AggregateUpdateResult<TResult> : IAggregateUpdateResultEx<TResult>
        {
            public TResult Result { get; }
            public IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
            public AggregateUpdateResult(TResult result, IReadOnlyCollection<IDomainEvent> domainEvents)
            {
                if (result == null)
                    throw new ArgumentNullException(nameof(result));
                Result = result;
                DomainEvents = domainEvents ?? throw new ArgumentNullException(nameof(domainEvents));
            }
            public TResult Unwrap() => Result;
        }
    }

    public interface IAggregateUpdateResultEx<out TResult>
    {
        TResult Result { get; }
        TResult Unwrap();
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    }

}

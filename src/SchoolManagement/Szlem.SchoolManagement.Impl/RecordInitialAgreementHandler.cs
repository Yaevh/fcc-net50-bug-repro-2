using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Commands;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.SharedKernel;

namespace Szlem.SchoolManagement.Impl
{
    internal class RecordInitialAgreementHandler : IRequestHandler<RecordInitialAgreement.Command, Result<Nothing, Error>>
    {
        private readonly IClock _clock;
        private readonly IUserAccessor _userAccessor;
        private readonly IAggregateStore _aggregateStore;
        public RecordInitialAgreementHandler(IClock clock, IUserAccessor userAccessor, IAggregateStore aggregateStore)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
        }

        public async Task<Result<Nothing, Error>> Handle(RecordInitialAgreement.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();

            var result = await _aggregateStore.Update<SchoolAggregate, SchoolId, Result<Nothing, Error>>(
                SchoolId.With(request.SchoolId), CommandId.New,
                (aggregate) => aggregate.RecordInitialAgreement(_clock.GetCurrentInstant(), request, user),
                cancellationToken);

            return result.Unwrap();
        }
    }
}

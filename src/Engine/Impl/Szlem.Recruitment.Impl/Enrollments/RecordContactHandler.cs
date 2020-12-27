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
using Szlem.Recruitment.Enrollments;
using Szlem.SharedKernel;
using static Szlem.Recruitment.Impl.Enrollments.EnrollmentAggregate;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class RecordContactHandler : IRequestHandler<RecordContact.Command, Result<Nothing, Error>>
    {
        private readonly IUserAccessor _userAccessor;
        private readonly IAggregateStore _aggregateStore;

        public RecordContactHandler(IUserAccessor userAccessor, IAggregateStore aggregateStore)
        {
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
        }

        public async Task<Result<Nothing, Error>> Handle(RecordContact.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentId, Result<Nothing, Error>>(
                EnrollmentId.With(request.EnrollmentId), CommandId.New,
                (aggregate) => aggregate.RecordContact(request, user),
                cancellationToken);
            return result.Unwrap();
        }
    }
}

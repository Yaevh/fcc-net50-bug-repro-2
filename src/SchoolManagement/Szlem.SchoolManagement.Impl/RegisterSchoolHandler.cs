using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Core;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine;
using Szlem.Engine.Interfaces;
using Szlem.SharedKernel;

namespace Szlem.SchoolManagement.Impl
{
    internal class RegisterSchoolHandler : IRequestHandler<RegisterSchool.Command, Result<Guid, Error>>
    {
        private readonly IClock _clock;
        private readonly IUserAccessor _userAccessor;
        private readonly IAggregateStore _aggregateStore;

        public RegisterSchoolHandler(IClock clock, IUserAccessor userAccessor, IAggregateStore aggregateStore)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
        }


        public async Task<Result<Guid, Error>> Handle(RegisterSchool.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();
            var schoolId = SchoolId.New;
            var result = await _aggregateStore.Update<SchoolAggregate, SchoolId, Result<Nothing, Error>>(
                    schoolId, SourceId.New,
                    (aggregate) => aggregate.RegisterSchool(_clock.GetCurrentInstant(), request, user),
                    cancellationToken);
            return result.Unwrap()
                .Map(success => schoolId.GetGuid());
        }
    }
}

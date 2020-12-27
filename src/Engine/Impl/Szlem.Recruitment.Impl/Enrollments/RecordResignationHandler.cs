using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using static Szlem.Recruitment.Impl.Enrollments.EnrollmentAggregate;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class RecordResignationHandler : IRequestHandler<RecordResignation.Command, Result<Nothing, Domain.Error>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IUserAccessor _userAccessor;
        private readonly IAuthorizationService _authService;
        private readonly IClock _clock;

        public RecordResignationHandler(IAggregateStore aggregateStore, IUserAccessor userAccessor, IAuthorizationService authService, IClock clock)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<Result<Nothing, Error>> Handle(RecordResignation.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();

            var authResult = await _authService.AuthorizeAsync(await _userAccessor.GetClaimsPrincipal(), EnrollmentId.With(request.EnrollmentId), AuthorizationPolicies.OwningCandidateOrCoordinator);
            if (authResult.Succeeded == false)
                return Result.Failure<Nothing, Error>(new Error.AuthorizationFailed());

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentId, Result<Nothing, Error>>(
                EnrollmentId.With(request.EnrollmentId), CommandId.New,
                (aggregate) => aggregate.RecordResignation(request, user, _clock.GetCurrentInstant()),
                cancellationToken);

            return result.Unwrap();
        }
    }
}

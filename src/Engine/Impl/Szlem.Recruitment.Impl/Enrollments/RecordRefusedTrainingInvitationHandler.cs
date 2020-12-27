using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Extensions;
using MediatR;
using Org.BouncyCastle.Ocsp;
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
    internal class RecordRefusedTrainingInvitationHandler : IRequestHandler<RecordRefusedTrainingInvitation.Command, Result<Nothing, Error>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IUserAccessor _userAccessor;
        private readonly NodaTime.IClock _clock;
        private readonly ITrainingRepository _trainingRepository;

        public RecordRefusedTrainingInvitationHandler(
            IAggregateStore aggregateStore,
            IUserAccessor userAccessor,
            NodaTime.IClock clock,
            ITrainingRepository trainingRepository)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trainingRepository = trainingRepository ?? throw new ArgumentNullException(nameof(trainingRepository));
        }

        public async Task<Result<Nothing, Error>> Handle(RecordRefusedTrainingInvitation.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();

            var enrollment = await _aggregateStore.LoadAsync<EnrollmentAggregate, EnrollmentId>(EnrollmentId.With(request.EnrollmentId), cancellationToken);
            var preferredTrainings = await _trainingRepository.GetByIds(enrollment.PreferredTrainingIds);

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentId, Result<Nothing, Error>>(
                EnrollmentId.With(request.EnrollmentId), CommandId.New,
                (aggregate) => aggregate.RecordCandidateRefusedTrainingInvitation(request, user, preferredTrainings, _clock.GetCurrentInstant()),
                cancellationToken);

            return result.Unwrap();
        }
    }
}

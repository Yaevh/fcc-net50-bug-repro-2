using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Extensions;
using Hangfire;
using MediatR;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
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
    internal class RecordAcceptedTrainingInvitationHandler : IRequestHandler<RecordAcceptedTrainingInvitation.Command, Result<Nothing, Error>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly ITrainingRepository _trainingRepository;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly NodaTime.IClock _clock;
        private readonly IUserAccessor _userAccessor;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ISzlemEngine _engine;

        public RecordAcceptedTrainingInvitationHandler(
            IAggregateStore aggregateStore,
            ITrainingRepository trainingRepository,
            IEnrollmentRepository enrollmentRepo,
            NodaTime.IClock clock,
            IUserAccessor userAccessor,
            IBackgroundJobClient backgroundJobClient,
            ISzlemEngine engine)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _trainingRepository = trainingRepository ?? throw new ArgumentNullException(nameof(trainingRepository));
            _enrollmentRepo = enrollmentRepo ?? throw new ArgumentNullException(nameof(enrollmentRepo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _backgroundJobClient = backgroundJobClient ?? throw new ArgumentNullException(nameof(backgroundJobClient));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public async Task<Result<Nothing, Error>> Handle(RecordAcceptedTrainingInvitation.Command request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();
            var enrollmentId = EnrollmentId.With(request.EnrollmentId);
            var enrollmentReadModel = _enrollmentRepo.Query()
                .SingleOrDefault(x => x.Id == enrollmentId);
            if (enrollmentReadModel == null)
                return Result.Failure<Nothing, Error>(new Error.ResourceNotFound());

            var preferredTrainings = await _trainingRepository.GetByIds(enrollmentReadModel.PreferredTrainings.Select(x => x.ID).ToArray());

            var result = await _aggregateStore.Update<EnrollmentAggregate, EnrollmentId, Result<Nothing, Error>>(
                enrollmentId, CommandId.New,
                (aggregate) => aggregate.RecordCandidateAcceptedTrainingInvitation(request, user, preferredTrainings, _clock.GetCurrentInstant()),
                cancellationToken);

            if (result.Unwrap().IsSuccess)
            {
                var selectedTraining = preferredTrainings.Single(x => x.ID == request.SelectedTrainingID);
                _backgroundJobClient.Schedule(() => _engine.Execute(
                        new SendTrainingReminder.Command() { EnrollmentId = request.EnrollmentId, TrainingId = request.SelectedTrainingID }),
                    selectedTraining.StartDateTime.Minus(NodaTime.Duration.FromHours(24)).ToDateTimeOffset());
            }

            return result.Unwrap();
        }
    }
}

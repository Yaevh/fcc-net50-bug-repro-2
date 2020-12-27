using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class GetEnrollmentDetailsHandler :
        IRequestHandler<GetEnrollmentDetails.QueryByEnrollmentId, Result<GetEnrollmentDetails.Details, Error>>,
        IRequestHandler<GetEnrollmentDetails.QueryByEmail, Result<GetEnrollmentDetails.Details, Error>>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IEventStore _eventStore;
        private readonly Repositories.ITrainingRepository _trainingRepository;
        private readonly Repositories.ICampaignRepository _campaignRepository;
        private readonly ITrainerProvider _trainerProvider;
        private readonly IClock _clock;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IUserAccessor _userAccessor;
        private readonly IAuthorizationService _authorizationService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.Users.ApplicationUser> _userManager;

        public GetEnrollmentDetailsHandler(
            IAggregateStore aggregateStore,
            IEventStore eventStore,
            Repositories.ITrainingRepository trainingRepository,
            Repositories.ICampaignRepository campaignRepository,
            ITrainerProvider trainerProvider,
            IClock clock,
            IEnrollmentRepository enrollmentRepo,
            IUserAccessor userAccessor,
            IAuthorizationService authorizationService,
            Microsoft.AspNetCore.Identity.UserManager<Models.Users.ApplicationUser> userManager)
        {
            _aggregateStore = aggregateStore ?? throw new ArgumentNullException(nameof(aggregateStore));
            _eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
            _trainingRepository = trainingRepository ?? throw new ArgumentNullException(nameof(trainingRepository));
            _campaignRepository = campaignRepository ?? throw new ArgumentNullException(nameof(campaignRepository));
            _trainerProvider = trainerProvider ?? throw new ArgumentNullException(nameof(trainerProvider));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _enrollmentRepo = enrollmentRepo ?? throw new ArgumentNullException(nameof(enrollmentRepo));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<Result<GetEnrollmentDetails.Details, Error>> Handle(GetEnrollmentDetails.QueryByEnrollmentId request, CancellationToken cancellationToken)
        {
            var id = EnrollmentAggregate.EnrollmentId.With(request.EnrollmentID);

            var events = await _eventStore.LoadEventsAsync<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>(id, cancellationToken);
            var enrollment = await _aggregateStore.LoadAsync<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>(id, cancellationToken);

            if (enrollment.IsNew)
                return Result.Failure<GetEnrollmentDetails.Details, Error>(new Error.ResourceNotFound());

            var preferredTrainings = await _trainingRepository.GetByIds(enrollment.PreferredTrainingIds);
            var trainers = await _trainerProvider.GetTrainerDetails(preferredTrainings.Select(x => x.CoordinatorID).ToArray());
            var latestCampaign = (await _campaignRepository.GetAll())
                .OrderByDescending(x => x.StartDateTime, OffsetDateTime.Comparer.Instant)
                .FirstOrDefault();
            var now = _clock.GetCurrentInstant();

            var details = await BuildDetails(enrollment, events, preferredTrainings, trainers, now, latestCampaign);

            return Result.Success<GetEnrollmentDetails.Details, Error>(details);
        }

        public async Task<Result<GetEnrollmentDetails.Details, Error>> Handle(GetEnrollmentDetails.QueryByEmail request, CancellationToken cancellationToken)
        {
            var user = await _userAccessor.GetUser();
            if (user == null)
                return Result.Failure<GetEnrollmentDetails.Details, Error>(new Error.BadRequest());

            var submission = _enrollmentRepo.Query().Where(x => x.Email == request.Email).SingleOrDefault();
            
            var authResult = await _authorizationService.AuthorizeAsync(await _userAccessor.GetClaimsPrincipal(), submission, AuthorizationPolicies.OwningCandidateOrCoordinator);
            if (authResult.Succeeded == false)
                return Result.Failure<GetEnrollmentDetails.Details, Error>(new Error.AuthorizationFailed());

            if (submission == null)
                return Result.Failure<GetEnrollmentDetails.Details, Error>(new Error.ResourceNotFound());

            return await Handle(new GetEnrollmentDetails.QueryByEnrollmentId() { EnrollmentID = submission.Id.GetGuid() }, cancellationToken);
        }

        private async Task<GetEnrollmentDetails.Details> BuildDetails(
            EnrollmentAggregate enrollment,
            IReadOnlyCollection<IDomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>> events,
            IReadOnlyCollection<Training> preferredTrainings,
            IReadOnlyCollection<TrainerDetails> trainers,
            Instant now,
            Campaign latestCampaign)
        {
            return new GetEnrollmentDetails.Details()
            {
                ID = enrollment.Id.GetGuid(),
                SubmissionDateTime = enrollment.SubmissionDateTime.InMainTimezone(),
                FirstName = enrollment.FirstName,
                LastName = enrollment.LastName,
                FullName = enrollment.FullName,
                Email = enrollment.Email,
                PhoneNumber = enrollment.PhoneNumber,
                Region = enrollment.Region,
                PreferredLecturingCities = enrollment.PreferredLecturingCities,
                PreferredTrainings = preferredTrainings.Select(x => BuildTrainingSummary(x, trainers)).ToArray(),
                SelectedTraining = enrollment.SelectedTrainingID.HasValue ? BuildTrainingSummary(preferredTrainings.Single(x => x.ID == enrollment.SelectedTrainingID.Value), trainers) : null,
                Events = (await Task.WhenAll(events.Select(async x => await BuildEventData(enrollment, x, preferredTrainings, trainers)))).ToArray(),
                CanInviteToTraining = enrollment.CanAcceptTrainingInvitation(preferredTrainings, now).IsSuccess || enrollment.CanRefuseTrainingInvitation(preferredTrainings, now).IsSuccess,
                CanRefuseTrainingInvitation = enrollment.CanRefuseTrainingInvitation(preferredTrainings, now).IsSuccess,
                CanRecordTrainingResults = enrollment.CanRecordTrainingResults(preferredTrainings, now).IsSuccess,
                CanResign = enrollment.CanResign,
                HasLecturerRights = enrollment.HasLecturerRights,
                HasResigned = enrollment.HasResignedPermanently || enrollment.HasResignedEffectively(now),
                HasResignedPermanently = enrollment.HasResignedPermanently,
                HasResignedTemporarily = enrollment.HasResignedTemporarily && enrollment.HasResignedEffectively(now),
                ResumeDate = enrollment.ResumeDate,
                IsCurrentSubmission = enrollment.CampaignId == latestCampaign?.Id,
                IsOldSubmission = enrollment.CampaignId != latestCampaign?.Id
            };
        }

        private Recruitment.Trainings.TrainingSummary BuildTrainingSummary(Training training, IReadOnlyCollection<TrainerDetails> trainers)
        {
            return new Recruitment.Trainings.TrainingSummary()
            {
                ID = training.ID,
                CoordinatorID = training.CoordinatorID,
                CoordinatorName = trainers.SingleOrDefault(y => y.Guid == training.CoordinatorID)?.Name,
                StartDateTime = training.StartDateTime,
                EndDateTime = training.EndDateTime,
                Address = training.Address,
                City = training.City
            };
        }

        private async Task<GetEnrollmentDetails.EventData> BuildEventData(EnrollmentAggregate aggregate, IDomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId> @event, IReadOnlyCollection<Training> trainings, IReadOnlyCollection<TrainerDetails> trainers)
        {
            switch (@event.GetAggregateEvent())
            {
                case RecruitmentFormSubmitted e:
                    return new GetEnrollmentDetails.RecruitmentFormSubmittedEventData()
                    {
                        DateTime = e.SubmissionDate.InMainTimezone(),
                        AboutMe = e.AboutMe,
                        EmailAddress = e.Email.ToString(),
                        FullName = $"{e.FirstName} {e.LastName}",
                        PhoneNumber = e.PhoneNumber.ToString(),
                        PreferredLecturingCities = e.PreferredLecturingCities,
                        PreferredTrainings = trainings
                            .Where(x => e.PreferredTrainingIds.Contains(x.ID))
                            .Select(x => BuildTrainingSummary(x, trainers)).ToArray()
                    };
                case EmailSent e:
                    return new GetEnrollmentDetails.EmailSentEventData()
                    {
                        DateTime = e.Instant.InMainTimezone(),
                        To = aggregate.Email.ToString(),
                        Body = e.Body,
                        Subject = e.Subject,
                        IsBodyHtml = e.IsBodyHtml
                    };
                case EmailSendingFailed e:
                    return new GetEnrollmentDetails.EmailFailedToSendEventData()
                    {
                        DateTime = e.Instant.InMainTimezone(),
                        To = aggregate.Email.ToString(),
                        Body = e.Body,
                        Subject = e.Subject,
                        IsBodyHtml = e.IsBodyHtml
                    };
                case CandidateAcceptedTrainingInvitation e:
                    return new GetEnrollmentDetails.CandidateAcceptedTrainingInvitationEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingCoordinatorID),
                        SelectedTraining = BuildTrainingSummary(trainings.Single(x => x.ID == e.SelectedTrainingID), trainers),
                        AdditionalNotes = e.AdditionalNotes
                    };
                case CandidateRefusedTrainingInvitation e:
                    return new GetEnrollmentDetails.CandidateRefusedTrainingInvitationEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingCoordinatorID),
                        AdditionalNotes = e.AdditionalNotes,
                        RefusalReason = e.RefusalReason
                    };
                case CandidateAttendedTraining e:
                    return new GetEnrollmentDetails.CandidateAttendedTrainingEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingCoordinatorID),
                        Training = BuildTrainingSummary(trainings.Single(x => x.ID == e.TrainingID), trainers),
                        AdditionalNotes = e.AdditionalNotes
                    };
                case CandidateWasAbsentFromTraining e:
                    return new GetEnrollmentDetails.CandidateWasAbsentFromTrainingEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingCoordinatorID),
                        Training = BuildTrainingSummary(trainings.Single(x => x.ID == e.TrainingID), trainers),
                        AdditionalNotes = e.AdditionalNotes
                    };
                case CandidateObtainedLecturerRights e:
                    return new GetEnrollmentDetails.CandidateObtainedLecturerRightsEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.GrantingCoordinatorID),
                        AdditionalNotes = e.AdditionalNotes
                    };
                case CandidateResignedPermanently e:
                    return new GetEnrollmentDetails.CandidateResignedPermanentlyEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingCoordinatorID),
                        ResignationReason = e.ResignationReason,
                        AdditionalNotes = e.AdditionalNotes
                    };
                case CandidateResignedTemporarily e:
                    return new GetEnrollmentDetails.CandidateResignedTemporarilyEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingCoordinatorID),
                        ResignationReason = e.ResignationReason,
                        AdditionalNotes = e.AdditionalNotes,
                        ResumeDate = e.ResumeDate
                    };
                case ContactOccured e:
                    return new GetEnrollmentDetails.ContactOccuredEventData()
                    {
                        DateTime = @event.Timestamp.ToZonedDateTime(),
                        RecordingUser = await GetUserName(e.RecordingUserId),
                        CommunicationChannel = e.CommunicationChannel,
                        Content = e.Content,
                        AdditionalNotes = e.AdditionalNotes
                    };
                default:
                    throw new NotSupportedException();
            }
        }

        private async Task<string> GetUserName(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            return user.ToString();
        }
    }
}

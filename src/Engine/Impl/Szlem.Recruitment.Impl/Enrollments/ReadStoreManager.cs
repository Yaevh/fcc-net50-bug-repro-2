using EventFlow.Aggregates;
using EventFlow.ReadStores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Repositories;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class ReadStoreManager : IReadStoreManager
    {
        private readonly IEnrollmentRepository _repo;
        private readonly ICampaignRepository _campaignRepo;
        private readonly ITrainingRepository _trainingRepo;

        public ReadStoreManager(IEnrollmentRepository repo, ICampaignRepository campaignRepo, ITrainingRepository trainingRepo)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _campaignRepo = campaignRepo ?? throw new ArgumentNullException(nameof(campaignRepo));
            _trainingRepo = trainingRepo ?? throw new ArgumentNullException(nameof(trainingRepo));
        }


        public Type ReadModelType => typeof(EnrollmentReadModel);

        public async Task UpdateReadStoresAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            var aggregateType = typeof(EnrollmentAggregate);
            var interestingEvents = domainEvents.Where(x => x.AggregateType == aggregateType).ToArray();

            foreach (var @event in interestingEvents)
            {
                switch (@event)
                {
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, EmailSent> _:
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, EmailSendingFailed> _:
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAttendedTraining> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateWasAbsentFromTraining> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateObtainedLecturerRights> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedPermanently> ev:
                        await UpdateReadStore(ev);
                        break;
                    case DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily> ev:
                        await UpdateReadStore(ev);
                        break;
                    default:
                        break;
                }
            }
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted> ev)
        {
            var campaign = await _campaignRepo.GetById(ev.AggregateEvent.CampaignID);

            await _repo.Insert(new EnrollmentReadModel()
            {
                Id = ev.AggregateIdentity,
                Timestamp = ev.AggregateEvent.SubmissionDate,
                FirstName = ev.AggregateEvent.FirstName,
                LastName = ev.AggregateEvent.LastName,
                FullName = ev.AggregateEvent.FirstName + " " + ev.AggregateEvent.LastName,
                Email = ev.AggregateEvent.Email,
                PhoneNumber = ev.AggregateEvent.PhoneNumber,
                AboutMe = ev.AggregateEvent.AboutMe,
                Campaign = new EnrollmentReadModel.CampaignSummary()
                {
                    Id = campaign.Id,
                    Name = campaign.Name,
                    StartDateTime = campaign.StartDateTime,
                    EndDateTime = campaign.EndDateTime
                },
                Region = ev.AggregateEvent.Region,
                PreferredLecturingCities = ev.AggregateEvent.PreferredLecturingCities,
                PreferredTrainings = campaign.Trainings
                    .Where(x => ev.AggregateEvent.PreferredTrainingIds.Contains(x.ID))
                    .Select(x => new EnrollmentReadModel.TrainingSummary()
                    {
                        ID = x.ID,
                        Address = x.Address,
                        City = x.City,
                        StartDateTime = x.StartDateTime,
                        EndDateTime = x.EndDateTime,
                        CoordinatorID = x.CoordinatorID
                    })
                    .ToArray(),
                CanReportTrainingResults = false,
                CanReportTrainingResultsConditionally = true
            });
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation> ev)
        {
            var maybeTraining = await _trainingRepo.GetById(ev.AggregateEvent.SelectedTrainingID);
            if (maybeTraining.HasNoValue)
                throw new ApplicationException($"inconsistend data in DB, cannot find training with ID={ev.AggregateEvent.SelectedTrainingID}");

            var training = maybeTraining.Value;
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.SelectedTraining = new EnrollmentReadModel.TrainingSummary() {
                ID = training.ID, Address = training.Address, City = training.City,
                StartDateTime = training.StartDateTime, EndDateTime = training.EndDateTime,
                CoordinatorID = training.CoordinatorID
            };
            readModel.HasRefusedTraining = false;
            readModel.TrainingRefusalReason = null;
            readModel.CanReportTrainingResults = readModel.CanReportTrainingResultsConditionally = true;
            await _repo.Update(readModel);
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation> ev)
        {
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.SelectedTraining = null;
            readModel.HasRefusedTraining = true;
            readModel.TrainingRefusalReason = ev.AggregateEvent.RefusalReason;
            readModel.CanReportTrainingResults = false;
            readModel.CanReportTrainingResultsConditionally = true;
            await _repo.Update(readModel);
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAttendedTraining> ev)
        {
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.TrainingResult = Recruitment.Enrollments.RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer;
            readModel.CanReportTrainingResults = false;
            readModel.CanReportTrainingResultsConditionally = false;
            await _repo.Update(readModel);
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateWasAbsentFromTraining> ev)
        {
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.TrainingResult = Recruitment.Enrollments.RecordTrainingResults.TrainingResult.Absent;
            readModel.CanReportTrainingResults = false;
            readModel.CanReportTrainingResultsConditionally = true;
            await _repo.Update(readModel);
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateObtainedLecturerRights> ev)
        {
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.HasLecturerRights = true;
            readModel.TrainingResult = Recruitment.Enrollments.RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer;
            readModel.CanReportTrainingResults = false;
            readModel.CanReportTrainingResultsConditionally = true;
            await _repo.Update(readModel);
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedPermanently> ev)
        {
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.HasResignedPermanently = true;
            readModel.SelectedTraining = null;
            readModel.CanReportTrainingResults = false;
            readModel.CanReportTrainingResultsConditionally = false;
            await _repo.Update(readModel);
        }

        private async Task UpdateReadStore(DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily> ev)
        {
            var readModel = _repo.Query().Single(x => x.Id == ev.AggregateIdentity);
            readModel.HasResignedTemporarily = true;
            readModel.ResumeDate = ev.AggregateEvent.ResumeDate;
            readModel.SelectedTraining = null;
            readModel.CanReportTrainingResults = false;
            readModel.CanReportTrainingResultsConditionally = true;
            await _repo.Update(readModel);
        }
    }
}

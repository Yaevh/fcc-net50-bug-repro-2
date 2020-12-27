using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Trainings;

namespace Szlem.Recruitment.Impl.Trainings
{
    internal class DetailsHandler : IRequestHandler<Details.Query, Maybe<Details.TrainingDetails>>
    {
        private readonly IClock _clock;
        private readonly ITrainingRepository _trainingRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ITrainerProvider _trainerProvider;

        public DetailsHandler(IClock clock, ITrainingRepository trainingRepo, ITrainerProvider trainerProvider, IEnrollmentRepository enrollmentRepo)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _trainingRepo = trainingRepo ?? throw new ArgumentNullException(nameof(trainingRepo));
            _trainerProvider = trainerProvider ?? throw new ArgumentNullException(nameof(trainerProvider));
            _enrollmentRepo = enrollmentRepo ?? throw new ArgumentNullException(nameof(enrollmentRepo));
        }


        public async Task<Maybe<Details.TrainingDetails>> Handle(Details.Query request, CancellationToken cancellationToken)
        {
            var now = _clock.GetCurrentInstant();

            var maybeTraining = await _trainingRepo.GetById(request.TrainingId);
            if (maybeTraining.HasNoValue)
                return Maybe<Details.TrainingDetails>.None;

            var training = maybeTraining.Value;
            var trainer = await _trainerProvider.GetTrainerDetails(training.CoordinatorID);
            if (trainer.HasNoValue)
                throw new ApplicationException($"inconsistent data in DB: cannot find trainer with Id={training.CoordinatorID}");

            if (now < training.StartDateTime.ToInstant())
                return Maybe<Details.TrainingDetails>.From(await BuildFutureTrainingDetails(training, trainer.Value, now));
            else if (training.EndDateTime.ToInstant() < now)
                return Maybe<Details.TrainingDetails>.From(await BuildPastTrainingDetails(training, trainer.Value));
            else
                return Maybe<Details.TrainingDetails>.From(await BuildCurrentTrainingDetails(training, trainer.Value));
        }

        private async Task<Details.TrainingDetails> BuildFutureTrainingDetails(Training training, TrainerDetails trainer, Instant now)
        {
            var invitedCandidates = _enrollmentRepo.Query()
                .Where(x => x.SelectedTraining != null && x.SelectedTraining.ID == training.ID)
                .Select(x => BuildFutureParticipant(x, training))
                .ToArray();

            var preferringCandidates = _enrollmentRepo.Query()
                .Where(x => x.PreferredTrainings.Select(y => y.ID).Contains(training.ID))
                .Select(x => BuildFutureParticipant(x, training))
                .ToArray();

            var details = new Details.FutureTrainingDetails()
            {
                Timing = TrainingTiming.Future,
                InvitedCandidates = invitedCandidates,
                PreferringCandidates = preferringCandidates.ToArray(),
                AvailableCandidates = preferringCandidates
                    .Where(x => x.HasAccepted == false && x.HasResignedPermanently == false
                        && (x.HasResignedTemporarily == false || x.ResignationEndDate == null || x.ResignationEndDate < training.StartDateTime.Date))
                    .ToArray()
            };
            await FixupCommonProperties(details, training, trainer);
            return details;
        }

        private async Task<Details.TrainingDetails> BuildCurrentTrainingDetails(Training training, TrainerDetails trainer)
        {
            var invitedCandidates = _enrollmentRepo.Query()
                .Where(x => x.SelectedTraining != null && x.SelectedTraining.ID == training.ID)
                .Select(x => BuildCurrentTrainingParticipant(x, training))
                .ToArray();
            var preferringCandidates = _enrollmentRepo.Query()
                .Where(x => x.PreferredTrainings.Select(y => y.ID).Contains(training.ID))
                .Select(x => BuildCurrentTrainingParticipant(x, training))
                .ToArray();

            var details = new Details.CurrentTrainingDetails()
            {
                Timing = TrainingTiming.Current,
                InvitedCandidates = invitedCandidates,
                PreferringCandidates = preferringCandidates
            };
            await FixupCommonProperties(details, training, trainer);
            return details;
        }

        private async Task<Details.TrainingDetails> BuildPastTrainingDetails(Training training, TrainerDetails trainer)
        {
            var invitedCandidates = _enrollmentRepo.Query()
                .Where(x => x.SelectedTraining != null && x.SelectedTraining.ID == training.ID)
                .Select(x => BuildPastParticipant(x, training))
                .ToArray();

            var preferringCandidates = _enrollmentRepo.Query()
                .Where(x => x.PreferredTrainings.Select(y => y.ID).Contains(training.ID))
                .Select(x => BuildPastParticipant(x, training))
                .ToArray();

            var details = new Details.PastTrainingDetails()
            {
                Timing = TrainingTiming.Past,
                InvitedCandidates = invitedCandidates,
                UnreportedCandidates = invitedCandidates
                    .Where(x => x.IsUnreported).ToArray(),
                PresentCandidates = invitedCandidates
                    .Where(x => x.WasPresentAndAcceptedAsLecturer || x.WasPresentButDidNotAcceptedAsLecturer).ToArray(),
                AbsentCandidates = invitedCandidates
                    .Where(x => x.WasAbsent).ToArray(),
                PreferringCandidates = preferringCandidates
            };
            await FixupCommonProperties(details, training, trainer);
            return details;
        }

        private async Task FixupCommonProperties(Details.TrainingDetails details, Training training, TrainerDetails trainer)
        {
            details.Id = training.ID;
            details.City = training.City;
            details.Address = training.Address;
            details.Start = training.StartDateTime;
            details.End = training.EndDateTime;
            details.CoordinatorId = trainer.Guid;
            details.CoordinatorName = trainer.Name;
            details.Duration = training.EndDateTime - training.StartDateTime;
            details.Notes = await Task.WhenAll(training.Notes
                .Select(BuildTrainingNote));
        }

        private async Task<Details.TrainingNote> BuildTrainingNote(Training.Note note)
        {
            Guard.Against.Null(note, nameof(note));
            var trainer = await _trainerProvider.GetTrainerDetails(note.AuthorId);

            if (trainer.HasNoValue)
                throw new ApplicationException($"inconsistent data, cannot find Trainer with ID={note.AuthorId}");

            return new Details.TrainingNote() { AuthorName = trainer.Value.Name, Content = note.Content, Timestamp = note.Timestamp.InMainTimezone() };
        }

        

        private Details.FutureTrainingParticipant BuildFutureParticipant(EnrollmentReadModel readModel, Training subject)
        {
            return new Details.FutureTrainingParticipant()
            {
                Id = readModel.Id.GetGuid(), FullName = readModel.FullName,
                Email = readModel.Email, Phone = readModel.PhoneNumber,
                HasAccepted = readModel.SelectedTraining != null,
                HasLecturerRights = readModel.HasLecturerRights,
                HasResignedPermanently = readModel.HasResignedPermanently,
                HasResignedTemporarily = readModel.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()),
                ResignationEndDate = readModel.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()) ? readModel.ResumeDate : null,
                CanBeInvited = readModel.SelectedTraining == null && readModel.HasResignedPermanently == false && readModel.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()) == false,
                ChoseAnotherTraining = readModel.SelectedTraining != null && readModel.SelectedTraining.ID != subject.ID
            };
        }

        private Details.CurrentTrainingParticipant BuildCurrentTrainingParticipant(EnrollmentReadModel readModel, Training subject)
        {
            return new Details.CurrentTrainingParticipant()
            {
                Id = readModel.Id.GetGuid(),
                FullName = readModel.FullName,
                Email = readModel.Email,
                Phone = readModel.PhoneNumber,
                HasLecturerRights = readModel.HasLecturerRights,
                ChoseAnotherTraining = readModel.SelectedTraining?.ID != subject.ID,
                HasResignedPermanently = readModel.HasResignedPermanently,
                HasResignedTemporarily = readModel.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()),
                ResignationEndDate = readModel.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()) ? readModel.ResumeDate : null
            };
        }

        private Details.PastTrainingParticipant BuildPastParticipant(EnrollmentReadModel readModel, Training subject)
        {
            return new Details.PastTrainingParticipant()
            {
                Id = readModel.Id.GetGuid(),
                FullName = readModel.FullName,
                Email = readModel.Email,
                Phone = readModel.PhoneNumber,
                HasLecturerRights = readModel.HasLecturerRights,
                IsUnreported = readModel.TrainingResult == null,
                IsInvited = readModel.SelectedTraining != null,
                CanRecordTrainingResults = readModel.CanReportTrainingResults
                    && readModel.TrainingResult == null
                    && readModel.SelectedTraining != null
                    && readModel.SelectedTraining.ID == subject.ID,
                CanRecordTrainingResultsConditionally = readModel.CanReportTrainingResultsConditionally
                    && (readModel.SelectedTraining == null || readModel.SelectedTraining.ID != subject.ID)
                    && readModel.TrainingResult == null,
                HasResignedPermanently = readModel.HasResignedPermanently,
                HasResignedTemporarily = readModel.HasResignedTemporarilyAsOf(subject.StartDateTime.ToInstant()),
                ResignationEndDate = readModel.HasResignedTemporarilyAsOf(subject.StartDateTime.ToInstant()) ? readModel.ResumeDate : null,
                ChoseAnotherTraining = readModel.SelectedTraining != null && readModel.SelectedTraining.ID != subject.ID,
                WasAbsent = readModel.TrainingResult == Recruitment.Enrollments.RecordTrainingResults.TrainingResult.Absent,
                WasPresentAndAcceptedAsLecturer = readModel.TrainingResult == Recruitment.Enrollments.RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer,
                WasPresentButDidNotAcceptedAsLecturer = readModel.TrainingResult == Recruitment.Enrollments.RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer,
            };
        }
    }
}

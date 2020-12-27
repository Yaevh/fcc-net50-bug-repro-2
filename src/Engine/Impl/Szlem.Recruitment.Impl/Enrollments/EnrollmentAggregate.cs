using Ardalis.GuardClauses;
using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Core;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Engine.Infrastructure;
using Szlem.Models.Users;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Impl.Enrollments
{
    [AggregateName("Szlem.Recruitment.Enrollment")]
    internal class EnrollmentAggregate : AggregateRoot<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Instant SubmissionDateTime { get; private set; }
        public EmailAddress Email { get; private set; }
        public PhoneNumber PhoneNumber { get; private set; }
        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName => $"{FirstName} {LastName}";

        public string Region { get; private set; }
        public IReadOnlyCollection<string> PreferredLecturingCities { get; private set; } = Array.Empty<string>();
        public IReadOnlyCollection<int> PreferredTrainingIds { get; private set; } = Array.Empty<int>();
        public int CampaignId { get; private set; }

        private readonly List<string> _additionalNotes = new List<string>();
        public IReadOnlyCollection<string> AdditionalNotes => _additionalNotes.AsReadOnly();

        public bool CanResign => true;
        public bool HasResignedPermanently { get; private set; } = false;
        public bool HasResignedTemporarily { get; private set; } = false;
        public LocalDate? ResumeDate { get; private set; } = null;
        public bool HasResignedEffectively(Instant time) => HasResignedPermanently || (HasResignedTemporarily && (ResumeDate == null || ResumeDate.Value >= time.InMainTimezone().Date));
        public bool HasLecturerRights { get; private set; } = false;
        

        private enum TrainingInvitationStateEnum
        {
            NotInvited,
            Refused,
            Accepted
        }
        private TrainingInvitationStateEnum _trainingInvitationState = TrainingInvitationStateEnum.NotInvited;

        public int? SelectedTrainingID { get; private set; }


        public class EnrollmentId : Identity<EnrollmentId>
        {
            public EnrollmentId(string value) : base(value) { }
        }


        public EnrollmentAggregate(EnrollmentId id) : base(id) { }


        #region SubmitRecruitmentForm & RecruitmentFormSubmitted
        public Result<Nothing, Error> SubmitRecruitmentForm(SubmitRecruitmentForm.Command command, IReadOnlyCollection<Training> preferredTrainings, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(preferredTrainings, nameof(preferredTrainings));
            Guard.Against.Default(now, nameof(now));

            return
                Validate(new SubmitRecruitmentForm.Validator(), command)
                .Ensure(
                    _ => preferredTrainings.Select(x => x.ID).OrderBy(x => x).SequenceEqual(command.PreferredTrainingIds.OrderBy(x => x)),
                    new Error.ResourceNotFound(SubmitRecruitmentForm_ErrorMessages.SomeTrainingsWereNotFound))
                .Ensure(
                    _ => preferredTrainings.Select(x => x.Campaign).Distinct().Count() == 1,
                    new Error.DomainError(SubmitRecruitmentForm_ErrorMessages.PreferredTrainingsMustBelongToTheSameCampaign))
                .Ensure(
                    _ => preferredTrainings.First().Campaign.Interval.Contains(now),
                    new Error.DomainError(SubmitRecruitmentForm_ErrorMessages.SubmissionMustOccurDuringCampaign))
                .Ensure(
                    _ => preferredTrainings.All(x => x.StartDateTime.ToInstant() > now),
                    new Error.DomainError(SubmitRecruitmentForm_ErrorMessages.PreferredTrainingsMustOccurInTheFuture))
                .Tap(() => Emit(RecruitmentFormSubmitted.From(command, now, preferredTrainings.First().Campaign.Id)));
        }

        protected void Apply(RecruitmentFormSubmitted e)
        {
            ResetResignationData();

            SubmissionDateTime = e.SubmissionDate;
            FirstName = e.FirstName;
            LastName = e.LastName;
            Email = e.Email;
            PhoneNumber = e.PhoneNumber;
            Region = e.Region;
            PreferredLecturingCities = e.PreferredLecturingCities;
            PreferredTrainingIds = e.PreferredTrainingIds;
            CampaignId = e.CampaignID;
        }
        #endregion


        #region EmailSent
        public void RecordEmailSent(Instant instant, EmailMessage message)
        {
            Guard.Against.Default(instant, nameof(instant));
            Guard.Against.Null(message, nameof(message));
            ValidateEmailOrThrow(message);
            Emit(new EmailSent(instant, message));
        }

        protected void Apply(EmailSent e) { }
        #endregion


        #region EmailSendingFailed
        public void RecordEmailSendingFailed(Instant instant, EmailMessage message)
        {
            Guard.Against.Default(instant, nameof(instant));
            Guard.Against.Null(message, nameof(message));
            ValidateEmailOrThrow(message);
            Emit(new EmailSendingFailed(instant, message));
        }

        protected void Apply(EmailSendingFailed e) { }
        #endregion


        #region CandidateRefusedTrainingInvitation
        public Result<Nothing, Error> RecordCandidateRefusedTrainingInvitation(
            RecordRefusedTrainingInvitation.Command command,
            ApplicationUser recordingCoordinator,
            IReadOnlyCollection<Training> preferredTrainings,
            Instant currentInstant)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingCoordinator, nameof(recordingCoordinator));
            Guard.Against.Null(preferredTrainings, nameof(preferredTrainings));
            Guard.Against.Default(currentInstant, nameof(currentInstant));
            ValidateIdMatchOrThrow(command.EnrollmentId);

            return
                Validate(new RecordRefusedTrainingInvitation.Validator(), command)
                .Ensure(_ => CanRefuseTrainingInvitation(preferredTrainings, currentInstant))
                .Tap(_ => Emit(new CandidateRefusedTrainingInvitation(
                    recordingCoordinatorID: recordingCoordinator.Id,
                    communicationChannel: command.CommunicationChannel,
                    refusalReason: command.RefusalReason ?? string.Empty,
                    additionalNotes: command.AdditionalNotes ?? string.Empty)));
        }

        public Result<Nothing, Error> CanRefuseTrainingInvitation(IReadOnlyCollection<Training> preferredTrainings, Instant currentInstant)
        {
            ValidatePreferredTrainingsMatchOrThrow(preferredTrainings);

            return Result.Success<Nothing, Error>(Nothing.Value)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(CommonErrorMessages.CandidateNotFound))
                .Ensure(
                    _ => SelectedTrainingID == null || preferredTrainings.Single(x => x.ID == SelectedTrainingID.Value).StartDateTime.ToInstant() > currentInstant,
                    new Error.DomainError(RecordRefusedTrainingInvitation_ErrorMessages.SelectedTrainingHasAlreadyPassed))
                .Ensure(
                    _ => preferredTrainings.Any(x => x.StartDateTime.ToInstant() > currentInstant),
                    new Error.DomainError(RecordRefusedTrainingInvitation_ErrorMessages.AllPreferredTrainingsHaveAlreadyPassed));
        }

        protected void Apply(CandidateRefusedTrainingInvitation e)
        {
            if (e.AdditionalNotes.IsNullOrWhiteSpace() == false)
                _additionalNotes.Add(e.AdditionalNotes);
            _trainingInvitationState = TrainingInvitationStateEnum.Refused;
            SelectedTrainingID = null;
        }
        #endregion


        #region CandidateAcceptedTrainingInvitation
        public Result<Nothing, Error> RecordCandidateAcceptedTrainingInvitation(
            RecordAcceptedTrainingInvitation.Command command,
            ApplicationUser recordingCoordinator,
            IReadOnlyCollection<Training> availableTrainings,
            Instant currentInstant)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingCoordinator, nameof(recordingCoordinator));
            Guard.Against.Null(availableTrainings, nameof(availableTrainings));
            Guard.Against.Default(currentInstant, nameof(currentInstant));
            ValidateIdMatchOrThrow(command.EnrollmentId);

            var preferredTrainings = availableTrainings.Where(x => PreferredTrainingIds.Contains(x.ID)).ToArray();
            var selectedTraining = preferredTrainings.SingleOrDefault(x => x.ID == command.SelectedTrainingID);

            return
                Validate(new RecordAcceptedTrainingInvitation.Validator(), command)
                .Ensure(_ => CanAcceptTrainingInvitation(preferredTrainings, currentInstant))
                .Ensure(
                    _ => availableTrainings.Any(x => x.ID == command.SelectedTrainingID),
                    new Error.ResourceNotFound(RecordAcceptedTrainingInvitation_ErrorMessages.TrainingNotFound))
                .Ensure(
                    _ => selectedTraining != null && PreferredTrainingIds.Contains(selectedTraining.ID),
                    new Error.ValidationFailed(nameof(command.SelectedTrainingID), RecordAcceptedTrainingInvitation_ErrorMessages.TrainingWasNotSpecifiedAsPreferred))
                .Ensure(
                    _ => availableTrainings.Single(x => x.ID == command.SelectedTrainingID).StartDateTime.ToInstant() > currentInstant,
                    new Error.ValidationFailed(nameof(command.SelectedTrainingID), RecordAcceptedTrainingInvitation_ErrorMessages.TrainingTimeAlreadyPassed))
                .Tap(_ => Emit(new CandidateAcceptedTrainingInvitation(
                    recordingCoordinatorID: recordingCoordinator.Id,
                    communicationChannel: command.CommunicationChannel,
                    selectedTrainingID: command.SelectedTrainingID,
                    additionalNotes: command.AdditionalNotes ?? string.Empty)));
        }

        public Result<Nothing, Error> CanAcceptTrainingInvitation(IReadOnlyCollection<Training> preferredTrainings, Instant currentInstant)
        {
            ValidatePreferredTrainingsMatchOrThrow(preferredTrainings);

            return Result.Success<Nothing, Error>(Nothing.Value)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(CommonErrorMessages.CandidateNotFound))
                .Ensure(
                    _ => preferredTrainings.Any(x => x.StartDateTime.ToInstant() > currentInstant),
                    new Error.DomainError(RecordAcceptedTrainingInvitation_ErrorMessages.TrainingTimeAlreadyPassed));
        }

        public bool HasSignedUpForTraining(Training training)
        {
            return SelectedTrainingID == training.ID;
        }

        protected void Apply(CandidateAcceptedTrainingInvitation e)
        {
            ResetResignationData();
            if (e.AdditionalNotes.IsNullOrWhiteSpace() == false)
                _additionalNotes.Add(e.AdditionalNotes);
            _trainingInvitationState = TrainingInvitationStateEnum.Accepted;
            SelectedTrainingID = e.SelectedTrainingID;
        }
        #endregion


        #region RecordTrainingResults
        public Result<Nothing, Error> RecordTrainingResults(
            RecordTrainingResults.Command command, ApplicationUser recordingCoordinator,
            IReadOnlyCollection<Training> preferredTrainings, Training selectedTraining, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingCoordinator, nameof(recordingCoordinator));
            Guard.Against.Null(preferredTrainings, nameof(preferredTrainings));
            Guard.Against.Null(selectedTraining, nameof(selectedTraining));
            Guard.Against.Null(now, nameof(now));
            ValidateIdMatchOrThrow(command.EnrollmentId);
            if (command.TrainingId != selectedTraining.ID)
                throw new InvalidOperationException($"{nameof(command)}.{nameof(command.TrainingId)} must be the same as {nameof(selectedTraining)}.{nameof(Id)}");

            var absent = Recruitment.Enrollments.RecordTrainingResults.TrainingResult.Absent;

            return
                Validate(new RecordTrainingResults.Validator(), command)
                .Ensure(_ => CanRecordTrainingResultsFor(selectedTraining, now))
                .Ensure(
                    _ => PreferredTrainingIds.Contains(selectedTraining.ID),
                    new Error.DomainError(RecordTrainingResults_Messages.TrainingNotSelectedAsPreferred))
                .Ensure(
                    _ => selectedTraining.EndDateTime.ToInstant() < now,
                    new Error.DomainError(RecordTrainingResults_Messages.CannotRecordTrainingAttendanceBeforeTrainingEnd))
                .EnsureNot(_ => command.TrainingResult == absent && _trainingInvitationState != TrainingInvitationStateEnum.Accepted,
                    new Error.DomainError(RecordTrainingResults_Messages.CannotRecordCandidateAsAbsentIfTheyDidNotAcceptTrainingInvitation))
                .TapIf(
                    command.TrainingResult == absent,
                    () => Emit(new CandidateWasAbsentFromTraining(recordingCoordinator.Id, command.TrainingId, command.AdditionalNotes ?? string.Empty)))
                .TapIf(
                    command.TrainingResult == Recruitment.Enrollments.RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer,
                    () => Emit(new CandidateAttendedTraining(recordingCoordinator.Id, command.TrainingId, command.AdditionalNotes ?? string.Empty)))
                .TapIf(
                    command.TrainingResult == Recruitment.Enrollments.RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer,
                    () => {
                        Emit(new CandidateAttendedTraining(recordingCoordinator.Id, command.TrainingId, command.AdditionalNotes ?? string.Empty));
                        Emit(new CandidateObtainedLecturerRights(recordingCoordinator.Id, command.AdditionalNotes ?? string.Empty));
                    });
        }

        public Result<Nothing, Error> CanRecordTrainingResults(IReadOnlyCollection<Training> preferredTrainings, Instant currentInstant)
        {
            ValidatePreferredTrainingsMatchOrThrow(preferredTrainings);

            return Result.Success<Nothing, Error>(Nothing.Value)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(CommonErrorMessages.CandidateNotFound))
                .Ensure(
                    _ => preferredTrainings.Any(x => x.EndDateTime.ToInstant() < currentInstant),
                    new Error.DomainError(RecordTrainingResults_Messages.CannotRecordTrainingAttendanceBeforeTrainingEnd));
        }

        public Result<Nothing, Error> CanRecordTrainingResultsFor(Training training, Instant currentInstant)
        {
            return Result.Success<Nothing, Error>(Nothing.Value)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(CommonErrorMessages.CandidateNotFound))
                .Ensure(
                    _ => PreferredTrainingIds.Contains(training.ID),
                    new Error.DomainError(RecordTrainingResults_Messages.TrainingNotSelectedAsPreferred))
                .Ensure(
                    _ => training.EndDateTime.ToInstant() < currentInstant,
                    new Error.DomainError(RecordTrainingResults_Messages.CannotRecordTrainingAttendanceBeforeTrainingEnd));
        }

        
        protected void Apply(CandidateWasAbsentFromTraining e)
        {
            _additionalNotes.Add(e.AdditionalNotes);
        }

        protected void Apply(CandidateAttendedTraining e)
        {
            _additionalNotes.Add(e.AdditionalNotes);
        }

        protected void Apply(CandidateObtainedLecturerRights e)
        {
            HasLecturerRights = true;
        }
        #endregion


        #region RecordResignation
        public Result<Nothing, Error> RecordResignation(RecordResignation.Command command, ApplicationUser recordingCoordinator, Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingCoordinator, nameof(recordingCoordinator));
            ValidateIdMatchOrThrow(command.EnrollmentId);

            return
                Validate(new RecordResignation.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(CommonErrorMessages.CandidateNotFound))
                .Ensure(
                    _ => {
                        if (command.ResignationType == Recruitment.Enrollments.RecordResignation.ResignationType.Temporary && command.ResumeDate.HasValue)
                            return command.ResumeDate.Value >= now.InMainTimezone().Date;
                        else
                            return true;
                    },
                    new Error.DomainError(RecordResignation_Messages.ResumeDateCannotBeEarlierThanToday))
                .TapIf(
                    command.ResignationType == Recruitment.Enrollments.RecordResignation.ResignationType.Permanent,
                    () => Emit(new CandidateResignedPermanently(
                        recordingCoordinatorID: recordingCoordinator.Id,
                        communicationChannel: command.CommunicationChannel,
                        resignationReason: command.ResignationReason ?? string.Empty,
                        additionalNotes: command.AdditionalNotes ?? string.Empty)))
                .TapIf(
                    command.ResignationType == Recruitment.Enrollments.RecordResignation.ResignationType.Temporary,
                    () => Emit(new CandidateResignedTemporarily(
                        recordingCoordinatorID: recordingCoordinator.Id,
                        communicationChannel: command.CommunicationChannel,
                        resignationReason: command.ResignationReason ?? string.Empty,
                        additionalNotes: command.AdditionalNotes ?? string.Empty,
                        resumeDate: command.ResumeDate)));
        }

        protected void Apply(CandidateResignedPermanently e)
        {
            ResetResignationData();
            HasResignedPermanently = true;
        }

        protected void Apply(CandidateResignedTemporarily e)
        {
            ResetResignationData();
            HasResignedTemporarily = true;
            ResumeDate = e.ResumeDate;
        }
        #endregion


        #region RecordContact
        public Result<Nothing, Error> RecordContact(RecordContact.Command command, ApplicationUser recordingUser)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(recordingUser, nameof(recordingUser));
            ValidateIdMatchOrThrow(command.EnrollmentId);

            return
                Validate(new RecordContact.Validator(), command)
                .Ensure(
                    _ => this.IsNew == false,
                    new Error.ResourceNotFound(CommonErrorMessages.CandidateNotFound))
                .Tap(() => Emit(new ContactOccured(
                    recordingUser.Id,
                    command.CommunicationChannel,
                    command.Content,
                    command.AdditionalNotes ?? string.Empty)));
        }

        protected void Apply(ContactOccured e) { }
        #endregion


        public Result<Nothing, Error> CanSendTrainingReminder(SendTrainingReminder.Command command, Training training, NodaTime.Instant now)
        {
            Guard.Against.Null(command, nameof(command));
            Guard.Against.Null(training, nameof(training));
            ValidateIdMatchOrThrow(command.EnrollmentId);

            if (SelectedTrainingID != null && training.ID != SelectedTrainingID)
                return Result.Failure<Nothing, Error>(new Error.DomainError(SendTrainingReminder_Messages.Reminder_cannot_be_sent_if_the_candidate_is_not_invited_to_training));
            if (HasResignedEffectively(training.StartDateTime.ToInstant()) || HasResignedEffectively(now))
                return Result.Failure<Nothing, Error>(new Error.DomainError(SendTrainingReminder_Messages.Reminder_cannot_be_sent_if_the_candidate_resigned));
            if (SelectedTrainingID == null)
                return Result.Failure<Nothing, Error>(new Error.DomainError(SendTrainingReminder_Messages.Reminder_cannot_be_sent_if_the_candidate_is_not_invited_to_training));
            if (training.StartDateTime.ToInstant().Minus(now) > Duration.FromHours(24))
                return Result.Failure<Nothing, Error>(new Error.DomainError(SendTrainingReminder_Messages.Reminder_cannot_be_sent_earlier_than_24h_before_training));
            if (training.StartDateTime.ToInstant() < now)
                return Result.Failure<Nothing, Error>(new Error.DomainError(SendTrainingReminder_Messages.Reminder_cannot_be_sent_after_training_start));

            return Result.Success<Nothing, Error>(Nothing.Value);
        }

        private void ResetResignationData()
        {
            HasResignedPermanently = HasResignedTemporarily = false;
            ResumeDate = null;
        }


        #region supporting code

        private Result<Nothing, Error> Validate<T>(FluentValidation.IValidator<T> validator, T instance)
        {
            Guard.Against.Null(instance, nameof(instance));
            var result = validator.Validate(instance);
            if (result.IsValid)
                return Result.Success<Nothing, Error>(Nothing.Value);
            else
                return Result.Failure<Nothing, Error>(new Error.ValidationFailed(result));
        }

        private void ValidateEmailOrThrow(EmailMessage message)
        {
            if (message.To.None(x => x == Email))
                throw new InvalidOperationException($"Specified {nameof(message)} must be sent to this aggregate ({Email})");
        }

        /// <summary>
        /// Validates whether given GUID matches this aggregate's ID
        /// </summary>
        /// <param name="guid"></param>
        private void ValidateIdMatchOrThrow(Guid guid)
        {
            if (EnrollmentId.With(guid) != this.Id)
                throw new AggregateMismatchException($"ID mismatch in {nameof(EnrollmentAggregate)}; expected {Id.GetGuid()}, got {guid}");
        }

        private void ValidatePreferredTrainingsMatchOrThrow(IReadOnlyCollection<Training> preferredTrainings)
        {
            if (preferredTrainings.Select(x => x.ID).OrderBy(x => x).SequenceEqual(PreferredTrainingIds.OrderBy(x => x)) == false)
                throw new ArgumentException($"{nameof(preferredTrainings)} collection contains different trainings than specified by {nameof(PreferredTrainingIds)}", nameof(preferredTrainings));
        }

        #endregion
    }
}

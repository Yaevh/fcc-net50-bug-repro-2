using EventFlow.Aggregates;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Test.Helpers;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class RecordTrainingResultsTests
    {
        #region supporting code
        private Training BuildTraining(NodaTime.LocalDateTime from, NodaTime.LocalDateTime to, int id)
        {
            var training = new Training(
                "Papieska 21/37", "Wadowice",
                 from.InMainTimezone().ToOffsetDateTime(),
                 to.InMainTimezone().ToOffsetDateTime(),
                 Guid.NewGuid()
            );
            training.GetType().GetProperty(nameof(Training.ID)).SetValue(training, id);
            return training;
        }

        private Training BuildDefaultTraining() =>
            BuildTraining(new NodaTime.LocalDateTime(2019, 09, 01, 10, 00), new NodaTime.LocalDateTime(2019, 09, 01, 12, 00), 1);
        #endregion


        [Theory(DisplayName = "Jeśli kandydat był obecny na szkoleniu, to agregat zawiera event CandidateAttendedTraining")]
        [InlineData(RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer)]
        public void If_candidate_was_present_on_the_training_aggregate_contains_CandidateAttendedTraining_event(RecordTrainingResults.TrainingResult trainingResult)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });

            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = trainingResult,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateAttendedTraining);
            var @event = Assert.IsType<CandidateAttendedTraining>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(1, @event.TrainingID);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
        }

        [Fact(DisplayName = "Jeśli kandydat nie był obecny na szkoleniu, to agregat zawiera event CandidateWasAbsentFromTraining")]
        public void If_candidate_was_absent_from_the_training_aggregate_contains_CandidateWasAbsentFromTraining_event()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });

            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command()
                {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents);
            var @event = Assert.IsType<CandidateWasAbsentFromTraining>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(1, @event.TrainingID);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
        }

        [Fact(DisplayName = "Jeśli kandydat był obecny na pełnym szkoleniu i został zatwierdzony przez prowadzącego, to agregat zawiera event CandidateObtainedLecturerRights")]
        public void If_candidate_was_present_on_the_training_and_obtained_lecturer_permissions_then_aggregate_contains_CandidateObtainedLecturerRights()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateObtainedLecturerRights);
            var @event = Assert.IsType<CandidateObtainedLecturerRights>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.GrantingCoordinatorID);
        }


        [Fact(DisplayName = "Jeśli kandydat był obecny na pełnym szkoleniu i został zatwierdzony przez prowadzacego, to zdobył uprawnienia do prowadzenia zajęć")]
        public void If_candidate_was_present_and_was_accepted_as_lecturer_then_he_has_premission_to_conduct_classes()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(enrollment.HasLecturerRights);
        }

        [Fact(DisplayName = "Jeśli kandydat nie został oznaczony jako uprawniony do prowadzenia zajęć, to nie ma uprawnień do prowadzenia zajęć")]
        public void If_candidate_was_present_but_was_not_accepted_as_lecturer_then_he_has_no_permission_to_conduct_classes()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer,
                    AdditionalNotes = "kandydat nie spełnia wymagań"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(enrollment.HasLecturerRights);
        }

        [Fact(DisplayName = "Jeśli kandydat był obecny na szkoleniu, ale nie zdobył uprawnień do prowadzenia zajęć, to komenda musi zawierać niepuste wyjaśnienie")]
        public void If_candidate_was_present_but_was_not_accepted_as_lecturer_then_command_must_contain_additional_notes()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer,
                    AdditionalNotes = null
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            AssertHelpers.SingleError(
                nameof(RecordTrainingResults.Command.AdditionalNotes),
                RecordTrainingResults_Messages.IfCandidateWasNotAccepted_CommandMustContainExplanation,
                failure);
        }



        [Fact(DisplayName = "Oznaczyć obecność na szkoleniu można tylko dla zarejestrowanych kandydatów")]
        public void Candidate_must_be_registered_to_record_their_training_attendance()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    AdditionalNotes = "brak notatek"
                },
                recordingCoordinator,
                Array.Empty<Training>(),
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(CommonErrorMessages.CandidateNotFound, error.Message);
        }

        [Theory(DisplayName = "Kandydat który nie został zaproszony na szkolenie może być oznaczony jako obecny na nim")]
        [InlineData(RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer)]
        public void Candidate_needs_not_to_have_been_invited_to_training_invitation_to_be_recorded_as_present(RecordTrainingResults.TrainingResult trainingResult)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    AdditionalNotes = "brak notatek",
                    TrainingResult = trainingResult
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "Kandydat który nie został zaproszony na szkolenie nie może być oznaczony jako nieobecny na nim")]
        public void Candidate_must_have_been_invited_to_training_invitation_to_be_recorded_as_absent()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    AdditionalNotes = "brak notatek",
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordTrainingResults_Messages.CannotRecordCandidateAsAbsentIfTheyDidNotAcceptTrainingInvitation, error.Message);
        }

        [Fact]
        public void Training_passed_to_command_must_have_the_same_ID_as_preferred_training_or_command_throws()
        {
            // Arrange
            var training = BuildTraining(new NodaTime.LocalDateTime(2019, 09, 01, 10, 00), new NodaTime.LocalDateTime(2019, 09, 01, 12, 00), 1);
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 2,
                    AdditionalNotes = "brak notatek",
                    TrainingResult = RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer
                },
                recordingCoordinator,
                new[] { training },
                training,
                NodaTime.SystemClock.Instance.GetCurrentInstant()));
        }


        [Theory(DisplayName = "Jeśli kandydat odmówił udziału w szkoleniu, a następnie się na nie zgodził, to można oznaczyć go jako obecnego/nieobecnego")]
        [InlineData(RecordTrainingResults.TrainingResult.Absent)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer)]
        public void If_candidate_refused_and_then_accepted_training_invitation_then_he_can_be_recorded_as_present_on_that_training(RecordTrainingResults.TrainingResult trainingResult)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                new CandidateRefusedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, "kandydat nie ma czasu", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var event3 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, "kandydat jednak znalazł czas"),
                new Metadata(), DateTimeOffset.Now, id, 3
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2, event3 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = trainingResult,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact(DisplayName = "Jeśli kandydat zaakceptował zaproszenie na szkolenie, a następnie odmówił, to nie można oznaczyć go jako nieobecnego")]
        public void If_candidate_accepted_and_then_refused_training_invitation_then_he_cannot_be_recorded_as_absent_on_that_training()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var event3 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                new CandidateRefusedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, "kandydat nie ma czasu", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 3
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2, event3 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordTrainingResults_Messages.CannotRecordCandidateAsAbsentIfTheyDidNotAcceptTrainingInvitation, error.Message);
        }


        [Theory(DisplayName = "Kandydat może być oznaczony jako obecny na szkoleniu, nawet jeśli jego ostatnia odpowiedź na zaproszenie była negatywna")]
        [InlineData(RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer)]
        public void Candidate_can_be_recorded_as_present_on_the_training_if_his_last_answer_to_invitation_was_negative(RecordTrainingResults.TrainingResult trainingResult)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                new CandidateRefusedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, "kandydat nie ma czasu", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = trainingResult,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            enrollment.UncommittedEvents.Should().Contain(x => x.AggregateEvent.GetType() == typeof(CandidateAttendedTraining));
        }

        [Fact(DisplayName = "Kandydat nie może być oznaczony jako nieobecny na szkoleniu, jeśli jego ostatnia odpowiedź na zaproszenie była negatywna")]
        public void Candidate_cannot_be_recorded_as_absent_on_the_training_if_his_last_answer_to_invitation_was_negative()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                new CandidateRefusedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, "kandydat nie ma czasu", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            result.Error.Should().BeOfType<Error.DomainError>().Which.Message.Should().Be(RecordTrainingResults_Messages.CannotRecordCandidateAsAbsentIfTheyDidNotAcceptTrainingInvitation);
        }


        [Theory(DisplayName = "Szkolenie musi znajdować się na liście szkoleń preferowanych przez kandydata")]
        [InlineData(RecordTrainingResults.TrainingResult.Absent)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer)]
        [InlineData(RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer)]
        public void Attended_training_must_be_among_trainings_preferred_by_candidate(RecordTrainingResults.TrainingResult trainingResult)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            var training = new Training(
                "Papieska 21/37", "Wadowice",
                 new NodaTime.LocalDateTime(2019, 09, 01, 10, 00).InMainTimezone().ToOffsetDateTime(),
                 new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                 Guid.NewGuid()
            );
            training.GetType().GetProperty(nameof(Training.ID)).SetValue(training, 2);

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 2,
                    TrainingResult = trainingResult,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                training,
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordTrainingResults_Messages.TrainingNotSelectedAsPreferred, error.Message);
        }

        [Fact(DisplayName = "Nie można zarejestrować obecności kandydata na szkoleniu przez zakończeniem szkolenia")]
        public void Cannot_record_training_attendance_before_trainings_end()
        {
            // Arrange
            var training = new Training(
                "Papieska 21/37", "Wadowice",
                 new NodaTime.LocalDateTime(2019, 09, 01, 10, 00).InMainTimezone().ToOffsetDateTime(),
                 new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                 Guid.NewGuid()
            );
            training.GetType().GetProperty(nameof(Training.ID)).SetValue(training, 1);

            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { training },
                training,
                new NodaTime.LocalDateTime(2019, 09, 01, 11, 00).InMainTimezone().ToInstant()
            );

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordTrainingResults_Messages.CannotRecordTrainingAttendanceBeforeTrainingEnd, error.Message);
        }

        [Fact(DisplayName = "Nie można zarejestrować obecności kandydata który już uzyskał uprawnienia prowadzącego")]
        public void Cannot_record_training_attendance_when_candidate_already_has_lecturer_rights()
        {
            // Arrange
            var training = new Training(
                "Papieska 21/37", "Wadowice",
                 new NodaTime.LocalDateTime(2019, 09, 01, 10, 00).InMainTimezone().ToOffsetDateTime(),
                 new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                 Guid.NewGuid()
            );
            training.GetType().GetProperty(nameof(Training.ID)).SetValue(training, 1);

            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var event3 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAttendedTraining>(
                new CandidateAttendedTraining(Guid.NewGuid(), 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 3
            );
            var event4 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateObtainedLecturerRights>(
                new CandidateObtainedLecturerRights(Guid.NewGuid(), "brak notatek"),
                new Metadata(), DateTimeOffset.Now, id, 4
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2, event3, event4 });
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { training },
                training,
                new NodaTime.LocalDateTime(2019, 09, 01, 11, 00).InMainTimezone().ToInstant()
            );

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordTrainingResults_Messages.CannotRecordTrainingAttendanceBeforeTrainingEnd, error.Message);
        }


        [Fact]
        public void Command_with_mismatched_EnrollmentID_throws_InvalidOperationException()
        {
            // Arrange
            var enrollment = new EnrollmentAggregate(EnrollmentAggregate.EnrollmentId.New);
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act & Assert
            var ex = Assert.Throws<AggregateMismatchException>(() => enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = default,
                    TrainingId = 1,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { BuildDefaultTraining() },
                BuildDefaultTraining(),
                NodaTime.SystemClock.Instance.GetCurrentInstant()
            ));
        }

        [Fact]
        public void Command_without_TrainingID_fails()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var training = BuildDefaultTraining();
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, default);
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordTrainingResults(new RecordTrainingResults.Command() {
                    EnrollmentId = id.GetGuid(),
                    TrainingId = default,
                    TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                    AdditionalNotes = "notatka testowa"
                },
                recordingCoordinator,
                new[] { training },
                training,
                NodaTime.SystemClock.Instance.GetCurrentInstant()
            );

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            Assert.Equal(nameof(RecordTrainingResults.Command.TrainingId), failure.PropertyName);
        }
    }
}

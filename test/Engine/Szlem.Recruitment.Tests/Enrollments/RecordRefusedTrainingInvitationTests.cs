using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class RecordRefusedTrainingInvitationTests
    {
        private static Training CreateTrainingWithIdAndDaysOffset(int id, int daysOffset)
        {
            var training = new Training(
                            "Papieska 12/37", "Wadowice",
                            SystemClock.Instance.GetOffsetDateTime().Plus(Duration.FromDays(daysOffset)),
                            SystemClock.Instance.GetOffsetDateTime().Plus(Duration.FromDays(daysOffset + 1)),
                            Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, id);
            return training;
        }


        [Fact]
        // PhoneNumber.Parse() takes ~1 second, WHY? because initial call to PhoneNumbers.PhoneNumberUtil.Parse() loads entire XML dictionary
        public void Registered_candidate_can_decline_training_invitation()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);

            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndDaysOffset(1, 1);

            // Act
            var command = new RecordRefusedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                RefusalReason = "brak powodu",
                AdditionalNotes = "brak notatek"
            };
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(command, recordingCoordinator, new[] { training }, SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var e = Assert.Single(enrollment.UncommittedEvents);
            var @event = Assert.IsType<CandidateRefusedTrainingInvitation>(e.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingPersonalContact, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.RefusalReason);
            Assert.Equal("brak notatek", @event.AdditionalNotes);
        }

        [Fact(DisplayName = "Po odrzuceniu zaproszenia na szkolenie agregat zawiera notatkę związaną z zaproszeniem")]
        public void After_registering_training_invitation_refusal_enrollment_aggregate_contains_additional_notes()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);

            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndDaysOffset(1, 1);

            // Act
            var command = new RecordRefusedTrainingInvitation.Command() {
                EnrollmentId = enrollment.Id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                RefusalReason = "brak powodu",
                AdditionalNotes = "brak notatek"
            };
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(command, recordingCoordinator, new[] { training }, SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("brak notatek", enrollment.AdditionalNotes);
        }

        [Fact(DisplayName = "Po odrzuceniu zaproszenia, kandydat nie jest zapisany na szkolenie")]
        public void After_refusing_invitation__candidate_is_not_signed_up_for_training()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
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
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndDaysOffset(1, 1);

            // Act
            var command = new RecordRefusedTrainingInvitation.Command() {
                EnrollmentId = enrollment.Id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                RefusalReason = "brak powodu",
                AdditionalNotes = "brak notatek"
            };
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(command, recordingCoordinator, new[] { training }, SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            enrollment.HasSignedUpForTraining(training).Should().BeFalse();
        }

        [Fact(DisplayName = "Jeśli żadne szkolenie nie zostało wybrane, nie można odrzucić zaproszenia na szkolenie po rozpoczęciu ostatniego z preferowanych szkoleń")]
        public void If_no_training_was_selected_cannot_refuse_training_invitation_after_all_preferred_trainings_have_started()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1, 2 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1 });
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training1 = CreateTrainingWithIdAndDaysOffset(1, -1);
            var training2 = CreateTrainingWithIdAndDaysOffset(2, -2);

            // Act
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(
                new RecordRefusedTrainingInvitation.Command() {
                    EnrollmentId = enrollment.Id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                    AdditionalNotes = "brak notatek"
                },
                recordingCoordinator,
                new[] { training1, training2 },
                SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordRefusedTrainingInvitation_ErrorMessages.AllPreferredTrainingsHaveAlreadyPassed, error.Message);
        }

        [Fact(DisplayName = "Jeśli szkolenie zostało wybrane, nie można odrzucić zaproszenia na szkolenie po rozpoczęciu tego szkolenia")]
        public void If_training_was_selected_cannot_refuse_training_invitation_after_that_training_has_started()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1, 2 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training1 = CreateTrainingWithIdAndDaysOffset(1, -1);
            var training2 = CreateTrainingWithIdAndDaysOffset(2, -2);

            // Act
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(
                new RecordRefusedTrainingInvitation.Command() {
                    EnrollmentId = enrollment.Id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                    AdditionalNotes = "brak notatek"
                },
                recordingCoordinator,
                new[] { training1, training2 },
                SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordRefusedTrainingInvitation_ErrorMessages.SelectedTrainingHasAlreadyPassed, error.Message);
        }

        [Fact]
        public void Candidate_must_be_registered_to_decline_training_invitation()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordRefusedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                RefusalReason = "brak powodu",
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(
                command,
                recordingCoordinator,
                Array.Empty<Training>(),
                SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsFailure);
            var error = Assert.IsType<Szlem.Domain.Error.ResourceNotFound>(result.Error);
            Assert.Equal(CommonErrorMessages.CandidateNotFound, error.Message);
        }

        [Theory]
        [InlineData(CommunicationChannel.Unknown)]
        public void Command_with_invalid_CommunicationChannel_fails(CommunicationChannel communicationChannel)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndDaysOffset(1, 1);
            var command = new RecordRefusedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = communicationChannel
            };

            // Act
            var result = enrollment.RecordCandidateRefusedTrainingInvitation(
                command,
                recordingCoordinator,
                new[] { training },
                SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            Assert.Equal(nameof(command.CommunicationChannel), failure.PropertyName);
            Assert.Single(failure.Errors);
        }

        [Fact]
        public void Command_without_EnrollmentID_fails()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndDaysOffset(1, 1);
            var command = new RecordRefusedTrainingInvitation.Command() {
                CommunicationChannel = CommunicationChannel.OutgoingEmail
            };

            // Act & Assert
            var ex = Assert.Throws<AggregateMismatchException>(() =>
                enrollment.RecordCandidateRefusedTrainingInvitation(
                    command, recordingCoordinator, new[] { training }, SystemClock.Instance.GetCurrentInstant()));
        }

        [Fact]
        public void Command_with_mismatched_EnrollmentID_fails()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndDaysOffset(1, 1);
            var command = new RecordRefusedTrainingInvitation.Command() {
                EnrollmentId = EnrollmentAggregate.EnrollmentId.New.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail
            };

            // Act & Assert
            var ex = Assert.Throws<AggregateMismatchException>(() =>
                enrollment.RecordCandidateRefusedTrainingInvitation(
                    command, recordingCoordinator, new[] { training }, SystemClock.Instance.GetCurrentInstant()));
        }
    }
}

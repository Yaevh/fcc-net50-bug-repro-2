using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.Aggregates.ExecutionResults;
using FluentAssertions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class RecordAcceptedTrainingInvitationTests
    {
        private static Training CreateTrainingInFutureWithId(int id)
        {
            return CreateTrainingWithIdAndDaysOffset(id, 7);
        }

        private static Training CreateTrainingWithIdAndDaysOffset(int id, int daysOffset)
        {
            var training = new Training(
                            "Papieska 12/37", "Wadowice",
                            SystemClock.Instance.GetOffsetDateTime() + Duration.FromDays(daysOffset),
                            SystemClock.Instance.GetOffsetDateTime() + Duration.FromDays(daysOffset + 1),
                            Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, id);
            return training;
        }

        private static Training CreateTrainingWithIdAndOffset(int id, Duration duration)
        {
            var training = new Training(
                            "Papieska 12/37", "Wadowice",
                            SystemClock.Instance.GetOffsetDateTime() + duration,
                            SystemClock.Instance.GetOffsetDateTime() + duration + Duration.FromHours(4),
                            Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, id);
            return training;
        }

        [Fact]
        public void Registered_candidate_can_accept_training_invitation()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var training = CreateTrainingInFutureWithId(1);
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents);
            var @event = Assert.IsType<CandidateAcceptedTrainingInvitation>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal(1, @event.SelectedTrainingID);
            Assert.Equal("brak notatek", @event.AdditionalNotes);
        }

        [Fact(DisplayName = "Po przyjęciu zaproszenia na szkolenie agregat zawiera notatkę związaną z zaproszeniem")]
        public void After_registering_training_invitation_refusal_enrollment_aggregate_contains_additional_notes()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            // Assert
            Assert.True(result.IsSuccess);
            Assert.Contains("brak notatek", enrollment.AdditionalNotes);
        }

        [Fact(DisplayName = "Tylko zarejestrowany kandydat może potwierdzić zaproszenie na szkolenie")]
        public void Candidate_must_be_registered_to_accept_training_invitation()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsFailure);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(CommonErrorMessages.CandidateNotFound, error.Message);
        }

        [Fact(DisplayName = "Potwierdzenie zaproszenia na szkolenie musi wskazywać na jedno z preferowanych szkoleń")]
        public void Training_invitation_acceptance_must_specify_one_of_preferred_trainings()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            Assert.Equal(nameof(command.SelectedTrainingID), failure.PropertyName);
            Assert.Single(failure.Errors);
        }

        [Fact(DisplayName = "Po potwierdzeniu zaproszenia, agregat zwraca HasSignedUpForTraining() == true dla tego szkolenia")]
        public void After_accepting_invitation__HasSignedUpForTraining_is_true()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                new CandidateRefusedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, "kandydat nie ma czasu", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var training = CreateTrainingInFutureWithId(1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(
                new RecordAcceptedTrainingInvitation.Command() {
                    EnrollmentId = enrollment.Id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                    AdditionalNotes = "brak notatek",
                    SelectedTrainingID = training.ID
                },
                recordingCoordinator,
                new[] { training },
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(enrollment.HasSignedUpForTraining(training));
        }

        [Fact(DisplayName = "Po potwierdzeniu zaproszenia, obecność kandydata na szkoleniu może zostać zarejestrowana")]
        public void After_accepting_invitation_can_record_training_attendance()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                new CandidateRefusedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, "kandydat nie ma czasu", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 2
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(
                new RecordAcceptedTrainingInvitation.Command() {
                    EnrollmentId = enrollment.Id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                    AdditionalNotes = "brak notatek",
                    SelectedTrainingID = 1
                },
                recordingCoordinator,
                new[] { CreateTrainingInFutureWithId(1) },
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.False(enrollment.CanRecordTrainingResults(new[] { CreateTrainingInFutureWithId(1) }, NodaTime.SystemClock.Instance.GetCurrentInstant()).IsSuccess);
        }

        [Fact(DisplayName = "Po potwierdzeniu zaproszenia, na Hangfire planowana jest komenda SendTrainingReminder z czasem 24h przed rozpoczęciem szkolenia")]
        public async Task After_accepting_invitation__SendTrainingReminder_command_is_scheduled()
        {
            // Arrange
            var enrollmentId = Guid.NewGuid();

            var aggregateUpdateResult = Mock.Of<IAggregateUpdateResult<AggregateStoreExtensions.ExecutionResultWrapper<Result<Nothing, Error>>>>(
                mock => mock.DomainEvents == Array.Empty<IDomainEvent>() && mock.Result == new AggregateStoreExtensions.ExecutionResultWrapper<Result<Nothing, Error>>(Result.Success<Nothing, Error>(Nothing.Value)));
            var aggregateStore = Mock.Of<IAggregateStore>(
                mock => mock.UpdateAsync(
                    It.IsAny<EnrollmentAggregate.EnrollmentId>(),
                    It.IsAny<EventFlow.Core.ISourceId>(),
                    It.IsAny<Func<EnrollmentAggregate, CancellationToken, Task<AggregateStoreExtensions.ExecutionResultWrapper<Result<Nothing, Error>>>>>(),
                    It.IsAny<CancellationToken>())
                    == Task.FromResult(aggregateUpdateResult),
                MockBehavior.Strict
            );

            var training = CreateTrainingWithIdAndOffset(1, Duration.FromDays(3));
            var trainingRepo = Mock.Of<Szlem.Recruitment.Impl.Repositories.ITrainingRepository>(
                mock => mock.GetByIds(It.IsAny<IReadOnlyCollection<int>>()) == Task.FromResult<IReadOnlyCollection<Training>>(new[] { training }), MockBehavior.Strict);

            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                mock => mock.Query() == new[] { new EnrollmentReadModel() {
                    Id = EnrollmentAggregate.EnrollmentId.With(enrollmentId),
                    PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 1 } }
                } }.AsQueryable(),
                MockBehavior.Strict);

            var userAccessor = Mock.Of<Szlem.Engine.Interfaces.IUserAccessor>(
                mock => mock.GetUser() == Task.FromResult(new Models.Users.ApplicationUser()), MockBehavior.Strict);
            
            var backgroundJobClientMock = new Mock<Hangfire.IBackgroundJobClient>(MockBehavior.Loose);

            var handler = new RecordAcceptedTrainingInvitationHandler(
                aggregateStore, trainingRepo, enrollmentRepo, NodaTime.SystemClock.Instance, userAccessor,
                backgroundJobClientMock.Object, Mock.Of<ISzlemEngine>());
            var command = new RecordAcceptedTrainingInvitation.Command()
                { EnrollmentId = enrollmentId, SelectedTrainingID = 1, CommunicationChannel = CommunicationChannel.OutgoingPhone };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            backgroundJobClientMock.Invocations.Should().ContainSingle();

            var invocation = backgroundJobClientMock.Invocations.Single();
            invocation.Method.Name.Should().Be(nameof(Hangfire.IBackgroundJobClient.Create));
            invocation.Arguments.Should().HaveCount(2);
            var scheduledJob = invocation.Arguments[0].Should().BeOfType<Hangfire.Common.Job>().Subject;
            var jobState = invocation.Arguments[1].Should().BeOfType<Hangfire.States.ScheduledState>().Subject;

            var subCommand = scheduledJob.Args[0] as SendTrainingReminder.Command;
            subCommand.EnrollmentId.Should().Be(enrollmentId);
            subCommand.TrainingId.Should().Be(1);

            jobState.EnqueueAt.Should().Be(training.StartDateTime.Minus(Duration.FromHours(24)).ToDateTimeOffset().UtcDateTime);
        }

        [Fact(DisplayName = "Nie można potwierdzić zaproszenia na szkolenie po rozpoczęciu szkolenia")]
        public void Cannot_accept_training_invitation_after_training_start_datetime()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, id, 1
            );
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1 });
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(-1));

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(
                new RecordAcceptedTrainingInvitation.Command() {
                    EnrollmentId = enrollment.Id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingPersonalContact,
                    AdditionalNotes = "brak notatek",
                    SelectedTrainingID = 1
                },
                recordingCoordinator,
                new[] { training },
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordAcceptedTrainingInvitation_ErrorMessages.TrainingTimeAlreadyPassed, error.Message);
        }

        [Theory]
        [InlineData(CommunicationChannel.Unknown)]
        public void Command_with_invalid_CommunicationChannel_fails(CommunicationChannel communicationChannel)
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = communicationChannel,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

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
            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };

            // Act & Assert
            var ex = Assert.Throws<AggregateMismatchException>(() => enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant()));
        }

        [Fact]
        public void Command_with_mismatched_EnrollmentID_fails()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };

            // Act & Assert
            var ex = Assert.Throws<AggregateMismatchException>(() => enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant()));
        }

        [Fact]
        public void Cannot_accept_invitation_to_training_with_ID_not_present_in_available_trainings()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var training = CreateTrainingInFutureWithId(1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 2,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(RecordAcceptedTrainingInvitation_ErrorMessages.TrainingNotFound, error.Message);
        }

        [Fact(DisplayName = "Nie można potwierdzić udziału w szkoleniu wcześniejszym niż data zgłoszenia")]
        public void Cannot_accept_invitation_to_training_earlier_than_submission_date()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var training = new Training(
                "Papieska 12/37", "Wadowice",
                SystemClock.Instance.GetOffsetDateTime().Minus(Duration.FromDays(7)),
                SystemClock.Instance.GetOffsetDateTime().Minus(Duration.FromDays(6)),
                Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 1,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordAcceptedTrainingInvitation_ErrorMessages.TrainingTimeAlreadyPassed, error.Message);
        }

        [Fact(DisplayName = "Nie można potwierdzić udziału w szkoleniu spoza lisy preferowanych szkoleń")]
        public void Cannot_accept_invitation_to_training_not_selected_as_preferred_training()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    NodaTime.SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "małopolskie", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(),
                DateTimeOffset.Now,
                id,
                1);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new[] { event1 });

            var training1 = CreateTrainingInFutureWithId(1);
            var training2 = CreateTrainingInFutureWithId(2);
            var recordingCoordinator = new Models.Users.ApplicationUser() { Id = Guid.NewGuid() };

            var command = new RecordAcceptedTrainingInvitation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                SelectedTrainingID = 2,
                AdditionalNotes = "brak notatek"
            };

            // Act
            var result = enrollment.RecordCandidateAcceptedTrainingInvitation(command, recordingCoordinator, new[] { training1, training2 }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            Assert.Equal(nameof(command.SelectedTrainingID), failure.PropertyName);
            var errorMessage = Assert.Single(failure.Errors);
            Assert.Equal(RecordAcceptedTrainingInvitation_ErrorMessages.TrainingWasNotSpecifiedAsPreferred, errorMessage);
        }
    }
}

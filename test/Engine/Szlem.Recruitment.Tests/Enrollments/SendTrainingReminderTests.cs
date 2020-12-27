using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using FluentAssertions;
using FluentNHibernate.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;
using Szlem.Engine.Interfaces;
using Szlem.Models.Users;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class SendTrainingReminderTests
    {
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


        [Fact(DisplayName = "Gdy warunki są spełnione, metoda .CanSendTrainingReminder() zwraca sukces")]
        public void With_valid_command__CanSendTrainingReminder_returns_success()
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

            var command = new SendTrainingReminder.Command() { EnrollmentId = id.GetGuid(), TrainingId = 1 };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(12));

            // Act
            var result = enrollment.CanSendTrainingReminder(command, training, SystemClock.Instance.GetCurrentInstant());

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Fact(DisplayName = "Komendy nie można wykonać wcześniej niż na 24h przed początkiem szkolenia")]
        public void Command_cannot_be_issued_earlier_than_24h_before_training()
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

            var command = new SendTrainingReminder.Command() { EnrollmentId = id.GetGuid(), TrainingId = 1 };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromDays(3));

            // Act
            var result = enrollment.CanSendTrainingReminder(command, training, SystemClock.Instance.GetCurrentInstant());

            // Assert
            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.DomainError>().Subject;
            error.Message.Should().Be(SendTrainingReminder_Messages.Reminder_cannot_be_sent_earlier_than_24h_before_training);
        }

        [Fact(DisplayName = "Komendy nie można wykonać po rozpoczęciu szkolenia")]
        public void Command_cannot_be_issued_after_straining_start()
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

            var command = new SendTrainingReminder.Command() { EnrollmentId = id.GetGuid(), TrainingId = 1 };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromDays(-1));

            // Act
            var result = enrollment.CanSendTrainingReminder(command, training, SystemClock.Instance.GetCurrentInstant());

            // Assert
            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.DomainError>().Subject;
            error.Message.Should().Be(SendTrainingReminder_Messages.Reminder_cannot_be_sent_after_training_start);
        }

        [Fact(DisplayName = "Komendy nie można wykonać, jeśli kandydat nie został zaproszony na szkolenie")]
        public void Command_is_invalid_if_candidate_was_not_invited()
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

            var command = new SendTrainingReminder.Command() { EnrollmentId = id.GetGuid(), TrainingId = 1 };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(12));

            // Act
            var result = enrollment.CanSendTrainingReminder(command, training, SystemClock.Instance.GetCurrentInstant());

            // Assert
            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.DomainError>().Subject;
            error.Message.Should().Be(SendTrainingReminder_Messages.Reminder_cannot_be_sent_if_the_candidate_is_not_invited_to_training);
        }

        [Fact(DisplayName = "Komendy nie można wykonać, jeśli kandydat zrezygnował ze szkolenia")]
        public void Command_is_invalid_if_candidate_resigned_from_training()
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
                new CandidateRefusedTrainingInvitation(
                    Guid.NewGuid(), CommunicationChannel.IncomingPhone, "zrezygnował", string.Empty),
                new Metadata(), DateTimeOffset.Now, id, 3);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2, event3 });

            var command = new SendTrainingReminder.Command() { EnrollmentId = id.GetGuid(), TrainingId = 1 };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(12));

            // Act
            var result = enrollment.CanSendTrainingReminder(command, training, SystemClock.Instance.GetCurrentInstant());

            // Assert
            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.DomainError>().Subject;
            error.Message.Should().Be(SendTrainingReminder_Messages.Reminder_cannot_be_sent_if_the_candidate_is_not_invited_to_training);
        }

        [Fact(DisplayName = "Komendy nie można wykonać, jeśli kandydat zrezygnował z udziału w projekcie")]
        public void Command_is_invalid_if_candidate_resigned_from_project()
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
            var event3 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                new CandidateResignedTemporarily(
                    Guid.NewGuid(), CommunicationChannel.IncomingPhone, "zrezygnował", string.Empty,
                    SystemClock.Instance.GetTodayDate().Plus(Period.FromDays(1))),
                new Metadata(), DateTimeOffset.Now, id, 3);
            var enrollment = new EnrollmentAggregate(id);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2, event3 });

            var command = new SendTrainingReminder.Command() { EnrollmentId = id.GetGuid(), TrainingId = 1 };
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(12));

            // Act
            var result = enrollment.CanSendTrainingReminder(command, training, SystemClock.Instance.GetCurrentInstant());

            // Assert
            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.DomainError>().Subject;
            error.Message.Should().Be(SendTrainingReminder_Messages.Reminder_cannot_be_sent_if_the_candidate_resigned);
        }

        [Fact(DisplayName = "Po wydaniu komendy, email jest wysyłany, a agregat zawiera event EmailSent")]
        public async Task After_command_is_executed__email_is_sent_and_aggregate_contains_EmailSent_event()
        {
            // Arrange
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, enrollmentId, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, enrollmentId, 2
            );
            var enrollment = new EnrollmentAggregate(enrollmentId);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });

            var aggregateStore = new MockAggregateStore<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>(enrollment);
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(12));
            var trainingRepo = Mock.Of<ITrainingRepository>(repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(training)));
            var emailService = new SucceedingEmailService();

            var options = Mock.Of<IOptions<Config>>(mock =>
                mock.Value == new Config() { TrainingReminderEmail = new Config.EmailMessageConfig() {
                    Subject = "przypomnienie o szkoleniu",
                    Body = "{{ Candidate.FullName }} przypominamy o szkoleniu w {{ Training.City }} w dniu {{ Training.StartDate }}" +
                        " o godzinie {{ Training.StartTime }} trwającym {{ Training.Duration }} godzin!"
                } });

            // Act
            var handler = new SendTrainingReminderHandler(SystemClock.Instance, aggregateStore, trainingRepo, emailService, options, new FluidTemplateRenderer());

            var command = new SendTrainingReminder.Command() { EnrollmentId = enrollmentId.GetGuid(), TrainingId = 1 };
            var result = await handler.Handle(command, CancellationToken.None);


            // Assert
            result.IsSuccess.Should().BeTrue();
            enrollment.UncommittedEvents.Should().ContainSingle();
            enrollment.UncommittedEvents.Single().AggregateEvent.Should().BeOfType<EmailSent>();
            emailService.SentMessages.Should().ContainSingle();
            emailService.SentMessages.Single().Should().BeEquivalentTo(new {
                Subject = "przypomnienie o szkoleniu",
                Body = $"Andrzej Strzelba przypominamy o szkoleniu w Wadowice w dniu {training.StartDateTime.Date}" +
                    $" o godzinie {training.StartDateTime.TimeOfDay} trwającym {training.Duration.ToString("HH':'mm", null)} godzin!"
            });
        }

        [Fact(DisplayName = "Po wydaniu niepoprawnej komendy email nie został wysłany, a agregat nie zawiera nowych eventów")]
        public async Task After_invalid_command_is_executed__email_is_not_sent_and_aggregate_does_not_contain_EmailSent_event()
        {
            // Arrange
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, enrollmentId, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, enrollmentId, 2
            );
            var enrollment = new EnrollmentAggregate(enrollmentId);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });

            var aggregateStore = new MockAggregateStore<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>(enrollment);
            var emailService = new Mock<IEmailService>();
            var options = Mock.Of<IOptions<Config>>(mock =>
                mock.Value == new Config() { TrainingReminderEmail = new Config.EmailMessageConfig()
                    { Subject = "przypomnienie o szkoleniu", Body = "przypominamy o szkoleniu" } });


            // Act
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromDays(3));
            var trainingRepo = Mock.Of<ITrainingRepository>(repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(training)));

            var handler = new SendTrainingReminderHandler(SystemClock.Instance, aggregateStore, trainingRepo, emailService.Object, options, new FluidTemplateRenderer());

            var command = new SendTrainingReminder.Command() { EnrollmentId = enrollmentId.GetGuid(), TrainingId = 1 };
            var result = await handler.Handle(command, CancellationToken.None);


            // Assert
            result.IsSuccess.Should().BeFalse();
            enrollment.UncommittedEvents.Should().BeEmpty();
            emailService.Invocations.Should().BeEmpty();
        }

        [Fact(DisplayName = "Po wydaniu komendy, jeśli serwis email trafił na błąd, agregat zawiera event EmailSendingFailed")]
        public async Task After_command_is_executed__if_email_service_encounters_error__aggregate_contains_EmailSendingFailed_event()
        {
            // Arrange
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var event1 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                new RecruitmentFormSubmitted(
                    SystemClock.Instance.GetCurrentInstant(),
                    "Andrzej", "Strzelba",
                    EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber,
                    "ala ma kota", 1, "Wolne Miasto Gdańsk", new[] { "Wadowice" }, new[] { 1 }, true),
                new Metadata(), DateTimeOffset.Now, enrollmentId, 1
            );
            var event2 = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                new CandidateAcceptedTrainingInvitation(Guid.NewGuid(), CommunicationChannel.OutgoingEmail, 1, string.Empty),
                new Metadata(), DateTimeOffset.Now, enrollmentId, 2
            );
            var enrollment = new EnrollmentAggregate(enrollmentId);
            enrollment.ApplyEvents(new IDomainEvent[] { event1, event2 });

            var aggregateStore = new MockAggregateStore<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>(enrollment);
            var emailService = new FailingEmailService();

            var options = Mock.Of<IOptions<Config>>(mock =>
                mock.Value == new Config() { TrainingReminderEmail = new Config.EmailMessageConfig()
                    { Subject = "przypomnienie o szkoleniu", Body = "przypominamy o szkoleniu" } });


            // Act
            var training = CreateTrainingWithIdAndOffset(1, Duration.FromHours(12));
            var trainingRepo = Mock.Of<ITrainingRepository>(repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(training)));

            var handler = new SendTrainingReminderHandler(SystemClock.Instance, aggregateStore, trainingRepo, emailService, options, new FluidTemplateRenderer());

            var command = new SendTrainingReminder.Command() { EnrollmentId = enrollmentId.GetGuid(), TrainingId = 1 };
            var result = await handler.Handle(command, CancellationToken.None);


            // Assert
            result.IsSuccess.Should().BeTrue();
            enrollment.UncommittedEvents.Should().ContainSingle().Which.AggregateEvent.Should().BeOfType<EmailSendingFailed>();
            var newEvent = enrollment.UncommittedEvents.Single().AggregateEvent as EmailSendingFailed;
            newEvent.Subject.Should().Be("przypomnienie o szkoleniu");
            newEvent.Body.Should().Be("przypominamy o szkoleniu");
            emailService.FailedMessages.Should().ContainSingle();
            var emailMessage = emailService.FailedMessages.Single();
            emailMessage.Subject.Should().Be("przypomnienie o szkoleniu");
            emailMessage.Body.Should().Be("przypominamy o szkoleniu");
            emailMessage.To.Should().ContainSingle().Which.Should().Be(EmailAddress.Parse("andrzej@strzelba.com"));
        }
    }
}

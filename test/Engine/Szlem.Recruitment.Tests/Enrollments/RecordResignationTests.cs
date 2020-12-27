using EventFlow.Aggregates;
using FluentAssertions;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.Models.Users;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class RecordResignationTests
    {
        [Fact(DisplayName = "Tylko zarejestrowany kandydat może zrezygnować")]
        public void Candidate_must_be_registered_to_resign()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };
            var command = new RecordResignation.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                ResignationReason = "brak powodu",
                AdditionalNotes = "brak notatek",
                ResignationType = RecordResignation.ResignationType.Permanent
            };

            // Act
            var result = enrollment.RecordResignation(command, recordingCoordinator, NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsFailure);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(CommonErrorMessages.CandidateNotFound, error.Message);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował trwale, to agregat ma ustawioną flagę HasResignedPermanently")]
        public void If_candidate_has_resigned_permanently_then_aggregate_HasResigned_flag_is_set()
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
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Permanent,
                },
                recordingCoordinator,
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(enrollment.HasResignedPermanently);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował trwale, to agregat zawiera event CandidateResignedPermanently z danymi rezygnacji")]
        public void If_candidate_has_resigned_permanently_then_aggregate_contains_CandidateResignedPermanently_event_with_resignation_data()
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
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Permanent
                },
                recordingCoordinator,
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateResignedPermanently);
            var @event = Assert.IsType<CandidateResignedPermanently>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.ResignationReason);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo bez daty wznowienia, to agregat zawiera event CandidateResignedTemporarily bez daty wznowienia")]
        public void If_candidate_has_resigned_temporarily_without_resume_date__then_aggregate_contains_CandidateResignedTemporarily_event_without_resume_date()
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
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Temporary,
                    ResumeDate = null
                },
                recordingCoordinator,
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateResignedTemporarily);
            var @event = Assert.IsType<CandidateResignedTemporarily>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.ResignationReason);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
            Assert.Null(@event.ResumeDate);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo bez daty wznowienia, to agregat zawiera flagę HasResignedTemporarily, a HasResignedEffectively() zwraca true")]
        public void If_candidate_has_resigned_temporarily_without_resume_date__then_aggregate_has_HasResignedTemporarily_flag_and_HasResignedEffectively_returns_true()
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
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Temporary,
                    ResumeDate = null
                },
                recordingCoordinator,
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateResignedTemporarily);
            var @event = Assert.IsType<CandidateResignedTemporarily>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.ResignationReason);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
            Assert.Null(@event.ResumeDate);

            enrollment.HasResignedEffectively(NodaTime.SystemClock.Instance.GetCurrentInstant()).Should().BeTrue();
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo z datą wznowienia, to agregat zawiera event CandidateResignedTemporarily z datą wznowienia")]
        public void If_candidate_has_resigned_temporarily_with_resume_date__then_aggregate_contains_CandidateResignedTemporarily_event_with_resume_date()
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
            var currentDate = new NodaTime.LocalDate(2020, 06, 30);

            // Act
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Temporary,
                    ResumeDate = currentDate.PlusWeeks(1)
                },
                recordingCoordinator,
                currentDate.AtStartOfDayInZone(Consts.MainTimezone).ToInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateResignedTemporarily);
            var @event = Assert.IsType<CandidateResignedTemporarily>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.ResignationReason);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
            Assert.Equal(currentDate.PlusWeeks(1), @event.ResumeDate);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo z datą wznowienia, to agregat ma flagę HasResignedTemporarily, a HasResignedEffectively() z datą przed datą wznowienia zwraca True")]
        public void If_candidate_has_resigned_temporarily_with_resume_date__then_HasResignedEffectively_with_current_date_returns_true()
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
            var currentDate = new NodaTime.LocalDate(2020, 06, 30);

            // Act
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Temporary,
                    ResumeDate = currentDate.PlusWeeks(1)
                },
                recordingCoordinator,
                currentDate.AtStartOfDayInZone(Consts.MainTimezone).ToInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateResignedTemporarily);
            var @event = Assert.IsType<CandidateResignedTemporarily>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.ResignationReason);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
            Assert.Equal(currentDate.PlusWeeks(1), @event.ResumeDate);

            enrollment.HasResignedEffectively(currentDate.AtStartOfDayInZone(Consts.MainTimezone).ToInstant()).Should().BeTrue();
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo z datą wznowienia, to agregat ma flagę HasResignedTemporarily, a HasResignedEffectively() z datą po dacie wznowienia zwraca False")]
        public void If_candidate_has_resigned_temporarily_with_resume_date__then_aggregate_has_HasResignedTemporarily_flag_and_HasResignedEffectively_with_date_after_resuma_date_returns_true()
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
            var currentDate = new NodaTime.LocalDate(2020, 06, 30);

            // Act
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Temporary,
                    ResumeDate = currentDate.PlusWeeks(1)
                },
                recordingCoordinator,
                currentDate.AtStartOfDayInZone(Consts.MainTimezone).ToInstant());

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is CandidateResignedTemporarily);
            var @event = Assert.IsType<CandidateResignedTemporarily>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingCoordinator.Id, @event.RecordingCoordinatorID);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("brak powodu", @event.ResignationReason);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
            Assert.Equal(currentDate.PlusWeeks(1), @event.ResumeDate);

            enrollment.HasResignedEffectively(currentDate.PlusWeeks(2).AtStartOfDayInZone(Consts.MainTimezone).ToInstant()).Should().BeFalse();
        }

        [Fact(DisplayName = "Komenda musi zawierać kanał komunikacji")]
        public void Command_with_unknown_communication_channel_fails()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingCoordinator = new ApplicationUser() { Id = Guid.NewGuid() };
            
            // Act
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.Unknown,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Permanent
                },
                recordingCoordinator,
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            var failure = Assert.Single(error.Failures);
            Assert.Equal(nameof(RecordResignation.Command.CommunicationChannel), failure.PropertyName);
            Assert.Single(failure.Errors);
        }

        [Fact(DisplayName = "Data wznowienia po rezygnacji tymczasowej nie może być wcześniejsza niż data bieżąca")]
        public void ResumeDate_cannot_be_earlier_than_today()
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
            var currentDate = new NodaTime.LocalDate(2020, 06, 30);

            // Act
            var result = enrollment.RecordResignation(new RecordResignation.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.IncomingEmail,
                    ResignationReason = "brak powodu",
                    AdditionalNotes = "notatka testowa",
                    ResignationType = RecordResignation.ResignationType.Temporary,
                    ResumeDate = currentDate.Minus(Period.FromDays(1))
                },
                recordingCoordinator,
                currentDate.AtStartOfDayInZone(Consts.MainTimezone).ToInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            error.Message.Should().BeEquivalentTo(RecordResignation_Messages.ResumeDateCannotBeEarlierThanToday);
        }
    }
}

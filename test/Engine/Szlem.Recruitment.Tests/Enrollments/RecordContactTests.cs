using EventFlow.Aggregates;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Test.Helpers;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class RecordContactTests
    {
        [Fact(DisplayName = "Komenda musi zawierać pola: CommunicationChannel, Content")]
        public void CommandMustContain_EnrollmentId_CommunicationChannel_Content_fields()
        {
            var enrollment = new EnrollmentAggregate(EnrollmentAggregate.EnrollmentId.New);
            var command = new RecordContact.Command() { EnrollmentId = enrollment.Id.GetGuid() };

            var result = enrollment.RecordContact(command, new Models.Users.ApplicationUser());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Equal(2, error.Failures.Count);
        }

        [Fact(DisplayName = "Komenda musi zostać wywołana na agregacie o zgodnym EnrollmentId")]
        public void CommandMustBeExecutedOnAggregateWithMatchingEnrollmentId()
        {
            var enrollment = new EnrollmentAggregate(EnrollmentAggregate.EnrollmentId.New);
            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };
            
            var command = new RecordContact.Command() {
                EnrollmentId = EnrollmentAggregate.EnrollmentId.New.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                Content = "ala ma kota",
                AdditionalNotes = "notatka testowa"
            };

            Assert.Throws<AggregateMismatchException>(() => enrollment.RecordContact(
                new RecordContact.Command() {
                    EnrollmentId = EnrollmentAggregate.EnrollmentId.New.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    Content = "ala ma kota",
                    AdditionalNotes = "notatka testowa"
                },
                recordingUser));
        }

        [Fact(DisplayName = "Komenda musi wskazywać na istniejący EnrollmentId")]
        public void CommandMustSpecifyExistingEnrollmentId()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var enrollment = new EnrollmentAggregate(id);
            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };

            var command = new RecordContact.Command() {
                EnrollmentId = id.GetGuid(),
                CommunicationChannel = CommunicationChannel.OutgoingEmail,
                Content = "ala ma kota",
                AdditionalNotes = "notatka testowa"
            };

            var result = enrollment.RecordContact(command, recordingUser);

            Assert.False(result.IsSuccess);
            Assert.IsType<Error.ResourceNotFound>(result.Error);
        }

        [Fact(DisplayName = "Po zarejestrowaniu kontaktu, agregat zawiera event ContactOccured")]
        public void AfterRegisteringContact_EnrollmentEmitsContactOccuredEvent()
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

            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };

            // Act
            var result = enrollment.RecordContact(new RecordContact.Command() {
                    EnrollmentId = id.GetGuid(),
                    CommunicationChannel = CommunicationChannel.OutgoingEmail,
                    Content = "ala ma kota",
                    AdditionalNotes = "notatka testowa"
                },
                recordingUser);

            // Assert
            Assert.True(result.IsSuccess);
            var uncommittedEvent = Assert.Single(enrollment.UncommittedEvents, e => e.AggregateEvent is ContactOccured);
            var @event = Assert.IsType<ContactOccured>(uncommittedEvent.AggregateEvent);
            Assert.Equal(recordingUser.Id, @event.RecordingUserId);
            Assert.Equal(CommunicationChannel.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal("ala ma kota", @event.Content);
            Assert.Equal("notatka testowa", @event.AdditionalNotes);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;
using Szlem.SchoolManagement.Impl;
using Szlem.SchoolManagement.Impl.Events;
using Xunit;

namespace Szlem.SchoolManagement.Tests
{
    public class RecordContactTests
    {
        [Fact(DisplayName = "Rejestracja kontaktu musi zawierać Id szkoły, timestamp, kanał komunikacji, imię i nazwisko osoby kontaktowej, treść")]
        public void Request_must_contain__SchoolId_Timestamp_CommunicationChannel_and_Content()
        {
            var school = new SchoolAggregate(SchoolId.With(Guid.Empty));
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command() {
                ContactPersonName = "   ",
                Content = "   "
            };
            var result = school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);

            Assert.Contains(RecordContact_Messages.SchoolId_cannot_be_empty, error.Failures[nameof(command.SchoolId)]);
            Assert.Contains(RecordContact_Messages.Timestamp_cannot_be_empty, error.Failures[nameof(command.ContactTimestamp)]);
            Assert.Contains(RecordContact_Messages.CommunicationChannel_cannot_be_empty, error.Failures[nameof(command.CommunicationChannel)]);
            Assert.Contains(RecordContact_Messages.ContactPersonName_cannot_be_empty, error.Failures[nameof(command.ContactPersonName)]);
            Assert.Contains(RecordContact_Messages.Content_cannot_be_empty, error.Failures[nameof(command.Content)]);
        }

        [Fact(DisplayName = "Timestamp kontaktu nie może być późniejszy niż bieżąca data i czas")]
        public void Request_Timestamp_cannot_be_later_than_current_timestamp()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var registeringUser = new ApplicationUser() { Id = Guid.NewGuid() };
            school.RegisterSchool(
                NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                },
                registeringUser);

            var command = new RecordContact.Command() {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() + NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść"
            };
            var result = school.RecordContact(command, new ApplicationUser() { Id = Guid.NewGuid() }, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(RecordContact_Messages.Contact_timestamp_cannot_be_later_than_current_timestamp, error.Message);
        }

        [Fact(DisplayName = "Komenda musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_aggregate_with_matching_SchoolId()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command() {
                SchoolId = SchoolId.New.GetGuid()
            };
            Assert.Throws<AggregateMismatchException>(() =>
                school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant()));
        }

        [Fact(DisplayName = "Rejestracja kontaktu musi wskazywać na Id istniejącej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command() {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść"
            };
            var result = school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(Messages.School_not_found, error.Message);
        }

        [Fact(DisplayName = "Jeśli kanał komunikacji to telefon wychodzący, to PhoneNumber musi być wypełniony")]
        public void If_CommunicationChannel_is_OutgoingPhone__PhoneNumber_must_be_provided()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command()
            {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingPhone,
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść"
            };
            var result = school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Contains(RecordContact_Messages.PhoneNumber_cannot_be_empty_when_CommunicationChannelType_is_IncomingPhone_or_OutgoingPhone,
                error.Failures[nameof(command.PhoneNumber)]);
        }

        [Fact(DisplayName = "Jeśli kanał komunikacji to telefon przychodzący, to PhoneNumber musi być wypełniony")]
        public void If_CommunicationChannel_is_IncomingPhone__PhoneNumber_must_be_provided()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command()
            {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.IncomingPhone,
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść"
            };
            var result = school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Contains(RecordContact_Messages.PhoneNumber_cannot_be_empty_when_CommunicationChannelType_is_IncomingPhone_or_OutgoingPhone,
                error.Failures[nameof(command.PhoneNumber)]);
        }

        [Fact(DisplayName = "Jeśli kanał komunikacji to email wychodzący, to EmailAddress musi być wypełniony")]
        public void If_CommunicationChannel_is_OutgoingEmail__EmailAddress_must_be_provided()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command()
            {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść"
            };
            var result = school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Contains(RecordContact_Messages.EmailAddress_cannot_be_empty_when_CommunicationChannel_is_IncomingEmail_or_OutgoingEmail,
                error.Failures[nameof(command.EmailAddress)]);
        }

        [Fact(DisplayName = "Jeśli kanał komunikacji to email przychodzący, to EmailAddress musi być wypełniony")]
        public void If_CommunicationChannel_is_IncomingEmail__EmailAddress_must_be_provided()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordContact.Command()
            {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.IncomingEmail,
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść"
            };
            var result = school.RecordContact(command, recordingUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Contains(RecordContact_Messages.EmailAddress_cannot_be_empty_when_CommunicationChannel_is_IncomingEmail_or_OutgoingEmail,
                error.Failures[nameof(command.EmailAddress)]);
        }

        [Fact(DisplayName = "Po rejestracji kontaktu, agregat zawiera event ContactOccured")]
        public void After_registering_contact__aggregate_contains_ContactOccured_event()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var registeringUser = new ApplicationUser() { Id = Guid.NewGuid() };
            school.RegisterSchool(
                NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(2),
                new RegisterSchool.Command()
                {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                    }
                },
                registeringUser);

            var command = new RecordContact.Command()
            {
                SchoolId = school.Id.GetGuid(),
                ContactTimestamp = NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                PhoneNumber = null,
                ContactPersonName = "Andrzej Strzelba",
                Content = "treść",
                AdditionalNotes = "notatka"
            };
            var result = school.RecordContact(command, registeringUser, NodaTime.SystemClock.Instance.GetCurrentInstant());

            Assert.True(result.IsSuccess);

            var uncommittedEvent = Assert.Single(school.UncommittedEvents, @event => @event.AggregateEvent is ContactOccured);
            var @event = Assert.IsType<ContactOccured>(uncommittedEvent.AggregateEvent);

            Assert.Equal(registeringUser.Id, @event.RecordingUserId);
            Assert.Equal(command.ContactTimestamp, @event.ContactTimestamp);
            Assert.Equal(CommunicationChannelType.OutgoingEmail, @event.CommunicationChannel);
            Assert.Equal(command.EmailAddress, @event.EmailAddress);
            Assert.Equal(command.PhoneNumber, @event.PhoneNumber);
            Assert.Equal("Andrzej Strzelba", @event.ContactPersonName);
            Assert.Equal("treść", @event.Content);
            Assert.Equal("notatka", @event.AdditionalNotes);
        }
    }
}

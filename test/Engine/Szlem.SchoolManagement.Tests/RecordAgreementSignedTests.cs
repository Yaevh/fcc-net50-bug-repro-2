using FluentAssertions;
using NodaTime;
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
    public class RecordAgreementSignedTests
    {
        protected Instant Now { get; }
        protected ZonedDateTime ZonedNow => Now.InMainTimezone();
        protected LocalDate Today => ZonedNow.Date;

        public RecordAgreementSignedTests() => Now = SystemClock.Instance.GetCurrentInstant();


        [Fact(DisplayName = "1. Komenda musi zawierać Id szkoły, skan dokumentu, rozszerzenie pliku i czas trwania")]
        public void Command_must_contain__schoolId_scannedDocument_extension_duration()
        {
            var school = new SchoolAggregate(SchoolId.With(Guid.Empty));
            var recordingUser = new ApplicationUser();

            var command = new RecordAgreementSigned.Command() {
                SchoolId = default,
                ScannedDocument = default,
                ScannedDocumentExtension = default,
                Duration = default,
                AgreementEndDate = default
            };

            var result = school.RecordAgreementSigned(command, recordingUser, Today);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);

            Assert.Contains(RecordAgreementSigned_Messages.SchoolId_cannot_be_empty, error.Failures[nameof(command.SchoolId)]);
            Assert.Contains(RecordAgreementSigned_Messages.ScannedDocument_cannot_be_empty, error.Failures[nameof(command.ScannedDocument)]);
            Assert.Contains(RecordAgreementSigned_Messages.ScannedDocumentExtension_cannot_be_empty, error.Failures[nameof(command.ScannedDocumentExtension)]);
            Assert.Contains(RecordAgreementSigned_Messages.Duration_cannot_be_empty, error.Failures[nameof(command.Duration)]);
        }

        [Fact(DisplayName = "2. Jeśli umowa jest zawarta na czas określony, komenda musi zawierać datę zakończenia umowy")]
        public void Command_must_contain__schoolId_scannedDocument_agreementTimespan()
        {
            var school = new SchoolAggregate(SchoolId.With(Guid.Empty));
            var recordingUser = new ApplicationUser();

            var command = new RecordAgreementSigned.Command() {
                SchoolId = school.Id.GetGuid(),
                ScannedDocument = new byte[] { 0x00 },
                ScannedDocumentExtension = "jpg",
                Duration = RecordAgreementSigned.AgreementDuration.FixedTerm,
                AgreementEndDate = default
            };

            var result = school.RecordAgreementSigned(command, recordingUser, Today);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);

            Assert.Contains(RecordAgreementSigned_Messages.AgreementEndDate_cannot_be_empty, error.Failures[nameof(command.AgreementEndDate)]);
        }

        [Fact(DisplayName = "3. Komenda musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_aggregate_with_matching_SchoolId()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordAgreementSigned.Command() {
                SchoolId = Guid.NewGuid()
            };
            Assert.Throws<AggregateMismatchException>(() =>
                school.RecordAgreementSigned(command, recordingUser, Today));
        }

        [Fact(DisplayName = "4. Rejestracja umowy o współpracy musi wskazywać na Id zarejestrowanej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordAgreementSigned.Command() {
                SchoolId = school.Id.GetGuid(),
                ScannedDocument = new byte[] { 0x00 },
                ScannedDocumentExtension = ".jpg",
                ScannedDocumentContentType = "image/jpeg",
                Duration = RecordAgreementSigned.AgreementDuration.Permanent
            };
            var result = school.RecordAgreementSigned(command, recordingUser, Today);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(Messages.School_not_found, error.Message);
        }

        [Fact(DisplayName = "5. Dla umowy na czas określony, AgreementEndDate musi być późniejszy niż dzisiejsza data")]
        public void When_registering_agreement_for_fixed_period__AgreementEndDate_must_be_in_the_future()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };
            school.RegisterSchool(
                NodaTime.SystemClock.Instance.GetCurrentInstant() - NodaTime.Duration.FromDays(2),
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
                new ApplicationUser() { Id = Guid.NewGuid() });

            var scannedDocument = new byte[] { 0x00 };
            var command = new RecordAgreementSigned.Command() {
                SchoolId = school.Id.GetGuid(),
                ScannedDocument = scannedDocument,
                ScannedDocumentExtension = ".jpg",
                ScannedDocumentContentType = "image/jpeg",
                Duration = RecordAgreementSigned.AgreementDuration.FixedTerm,
                AgreementEndDate = Today - Period.FromDays(1)
            };
            var result = school.RecordAgreementSigned(command, recordingUser, Today);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.DomainError>()
                .Which.Message.Should().BeEquivalentTo(RecordAgreementSigned_Messages.AgreementEndDate_must_be_in_the_future);
        }

        [Fact(DisplayName = "6. Po rejestracji umowy na czas nieokreślony, agregat zawiera event PermanentAgreementSigned")]
        public void After_registering_agreement_for_indefinite_period__aggregate_contains_PermanentAgreementSigned_event()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };
            school.RegisterSchool(
                SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(2),
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
                new ApplicationUser() { Id = Guid.NewGuid() });

            var scannedDocument = new byte[] { 0x00 };
            var command = new RecordAgreementSigned.Command() {
                SchoolId = school.Id.GetGuid(),
                ScannedDocument = scannedDocument,
                ScannedDocumentExtension = ".jpg",
                ScannedDocumentContentType = "image/jpeg",
                Duration = RecordAgreementSigned.AgreementDuration.Permanent
            };
            var result = school.RecordAgreementSigned(command, recordingUser, Today);

            Assert.True(result.IsSuccess);

            var uncommittedEvent = Assert.Single(school.UncommittedEvents, @event => @event.AggregateEvent is PermanentAgreementSigned);
            var @event = Assert.IsType<PermanentAgreementSigned>(uncommittedEvent.AggregateEvent);

            Assert.Equal(recordingUser.Id, @event.RecordingUserId);
            @event.ScannedDocument.Should().Equal(scannedDocument);
            @event.ScannedDocumentExtension.Should().Be(".jpg");
            @event.ScannedDocumentContentType.Should().Be("image/jpeg");
            result.Value.Should().Be(@event.Id, "command should return the same GUID as agreement ID");
        }

        [Fact(DisplayName = "7. Po rejestracji umowy na czas określony, agregat zawiera event FixedTermAgreementSigned")]
        public void After_registering_agreement_for_school_year__aggregate_contains_FixedTermAgreementSigned_event()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };
            school.RegisterSchool(
                SystemClock.Instance.GetCurrentInstant() - Duration.FromDays(2),
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
                new ApplicationUser() { Id = Guid.NewGuid() });

            var scannedDocument = new byte[] { 0x00 };
            var agreementEndDate = SystemClock.Instance.GetTodayDate().PlusYears(1);
            var command = new RecordAgreementSigned.Command() {
                SchoolId = school.Id.GetGuid(),
                ScannedDocument = scannedDocument,
                ScannedDocumentExtension = ".jpg",
                ScannedDocumentContentType = "image/jpeg",
                Duration = RecordAgreementSigned.AgreementDuration.FixedTerm,
                AgreementEndDate = agreementEndDate
            };
            var result = school.RecordAgreementSigned(command, recordingUser, Today);
            result.IsSuccess.Should().BeTrue();

            var uncommittedEvent = Assert.Single(school.UncommittedEvents, @event => @event.AggregateEvent is FixedTermAgreementSigned);
            var @event = Assert.IsType<FixedTermAgreementSigned>(uncommittedEvent.AggregateEvent);

            Assert.Equal(recordingUser.Id, @event.RecordingUserId);
            Assert.Equal(agreementEndDate, @event.AgreementEndDate);
            @event.ScannedDocument.Should().Equal(scannedDocument);
            @event.ScannedDocumentExtension.Should().Be(".jpg");
            @event.ScannedDocumentContentType.Should().Be("image/jpeg");
            result.Value.Should().Be(@event.Id, "command should return the same GUID as agreement ID");
        }
    }
}

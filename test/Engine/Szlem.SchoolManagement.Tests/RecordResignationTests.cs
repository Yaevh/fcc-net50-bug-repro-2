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
    public class RecordResignationTests
    {
        protected Instant Now { get; }
        protected ZonedDateTime ZonedNow => Now.InMainTimezone();
        protected LocalDate LocalDate => ZonedNow.Date;
        
        public RecordResignationTests() => Now = NodaTime.SystemClock.Instance.GetCurrentInstant();


        [Fact(DisplayName = "1. Komenda musi zawierać Id szkoły")]
        public void Command_must_contain__schoolId_and_resignationDate()
        {
            var school = new SchoolAggregate(SchoolId.With(Guid.Empty));
            var recordingUser = new ApplicationUser();

            var command = new RecordResignation.Command() {
                SchoolId = default
            };

            var result = school.RecordResignation(command, recordingUser, LocalDate);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);

            Assert.Contains(RecordResignation_Messages.SchoolId_cannot_be_empty, error.Failures[nameof(command.SchoolId)]);
        }

        [Fact(DisplayName = "2. Komenda musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_aggregate_with_matching_SchoolId()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordResignation.Command() {
                SchoolId = Guid.NewGuid()
            };
            Assert.Throws<AggregateMismatchException>(() => school.RecordResignation(command, recordingUser, LocalDate));
        }

        [Fact(DisplayName = "3. Rejestracja rezygnacji ze współpracy musi wskazywać na Id istniejącej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordResignation.Command() {
                SchoolId = school.Id.GetGuid()
            };
            var result = school.RecordResignation(command, recordingUser, LocalDate);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(Messages.School_not_found, error.Message);
        }

        [Fact(DisplayName = "4. Data następnego kontaktu musi być późniejsza niż bieżąca data")]
        public void ResignationDate_cannot_be_later_than_today()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser() { Id = Guid.NewGuid() };
            school.RegisterSchool(
                Now - Duration.FromDays(2),
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

            var command = new RecordResignation.Command() {
                SchoolId = school.Id.GetGuid(),
                PotentialNextContactDate = LocalDate - Period.FromDays(1)
            };
            var result = school.RecordResignation(command, recordingUser, LocalDate);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.DomainError>()
                .Which.Message.Should().BeEquivalentTo(RecordResignation_Messages.PotentialNextContactDate_must_be_later_than_today);
        }

        [Fact(DisplayName = "5. Po rejestracji rezygnacji ze współpracy bez daty ponownego kontaktu, agregat zawiera event SchoolResignedFromCooperation")]
        public void After_registering_resignation__aggregate_contains_SchoolResignedFromCooperation_event()
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

            var resignationDate = SystemClock.Instance.GetTodayDate().Minus(Period.FromDays(1));
            var command = new RecordResignation.Command() {
                SchoolId = school.Id.GetGuid()
            };
            var result = school.RecordResignation(command, recordingUser, LocalDate);

            Assert.True(result.IsSuccess);

            var uncommittedEvent = Assert.Single(school.UncommittedEvents, @event => @event.AggregateEvent is SchoolResignedFromCooperation);
            var @event = Assert.IsType<SchoolResignedFromCooperation>(uncommittedEvent.AggregateEvent);

            Assert.Equal(recordingUser.Id, @event.RecordingUserId);
        }

        [Fact(DisplayName = "6. Po rejestracji rezygnacji ze współpracy z datą ponownego kontaktu, agregat zawiera event SchoolResignedFromCooperation z datą ponownego kontaktu")]
        public void After_registering_resignation_with_potentialNextContactDate__aggregate_contains_SchoolResignedFromCooperation_event()
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

            var resignationDate = SystemClock.Instance.GetTodayDate().Minus(Period.FromDays(1));
            var potentialNextContactDate = SystemClock.Instance.GetTodayDate().PlusYears(1);
            var command = new RecordResignation.Command() {
                SchoolId = school.Id.GetGuid(),
                PotentialNextContactDate = potentialNextContactDate,
                AdditionalNotes = "odezwać się za rok"
            };
            var result = school.RecordResignation(command, recordingUser, LocalDate);

            Assert.True(result.IsSuccess);

            var uncommittedEvent = Assert.Single(school.UncommittedEvents, @event => @event.AggregateEvent is SchoolResignedFromCooperation);
            var @event = Assert.IsType<SchoolResignedFromCooperation>(uncommittedEvent.AggregateEvent);

            Assert.Equal(recordingUser.Id, @event.RecordingUserId);
            Assert.Equal(potentialNextContactDate, @event.PotentialNextContactDate);
            Assert.Equal("odezwać się za rok", @event.AdditionalNotes);
        }
    }
}

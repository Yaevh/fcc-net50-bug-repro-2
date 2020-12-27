using FluentAssertions;
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
    public class RecordInitialAgreementTests
    {
        [Fact(DisplayName = "Komenda musi zawierać Id szkoły, kanał komunikacji, imię i nazwisko osoby kontaktowej")]
        public void Command_must_contain__schoolId_communicationChannel_contactPersonName()
        {
            var school = new SchoolAggregate(SchoolId.With(Guid.Empty));
            var recordingUser = new ApplicationUser();

            var command = new RecordInitialAgreement.Command() {
                AgreeingPersonName = "   ",
                SchoolId = default
            };

            var result = school.RecordInitialAgreement(NodaTime.SystemClock.Instance.GetCurrentInstant(), command, recordingUser);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);

            Assert.Contains(RecordInitialAgreement_Messages.SchoolId_cannot_be_empty, error.Failures[nameof(command.SchoolId)]);
            Assert.Contains(RecordInitialAgreement_Messages.ContactPersonName_cannot_be_empty, error.Failures[nameof(command.AgreeingPersonName)]);
        }


        [Fact(DisplayName = "Komenda musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_aggregate_with_matching_SchoolId()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordInitialAgreement.Command() {
                SchoolId = Guid.NewGuid()
            };
            Assert.Throws<AggregateMismatchException>(() =>
                school.RecordInitialAgreement(NodaTime.SystemClock.Instance.GetCurrentInstant(), command, recordingUser));
        }

        [Fact(DisplayName = "Rejestracja kontaktu musi wskazywać na Id istniejącej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var recordingUser = new ApplicationUser();

            var command = new RecordInitialAgreement.Command() {
                SchoolId = school.Id.GetGuid(),
                AgreeingPersonName = "Andrzej Strzelba"
            };
            var result = school.RecordInitialAgreement(NodaTime.SystemClock.Instance.GetCurrentInstant(), command, recordingUser);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(Messages.School_not_found, error.Message);
        }

        [Fact(DisplayName = "Po rejestracji wstępnej zgody, agregat zawiera event InitialAgreementAchieved")]
        public void After_registering_initial_agreement__aggregate_contains_InitialAgreementAchieved_event()
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

            var command = new RecordInitialAgreement.Command() {
                SchoolId = school.Id.GetGuid(),
                AgreeingPersonName = "Andrzej Strzelba",
                AdditionalNotes = "test"
            };
            var result = school.RecordInitialAgreement(NodaTime.SystemClock.Instance.GetCurrentInstant(), command, recordingUser);

            result.IsSuccess.Should().BeTrue();

            school.HasAgreedInitially.Should().BeTrue();

            var uncommittedEvent = Assert.Single(school.UncommittedEvents, @event => @event.AggregateEvent is InitialAgreementAchieved);
            var @event = Assert.IsType<InitialAgreementAchieved>(uncommittedEvent.AggregateEvent);
            @event.Should().BeEquivalentTo(new InitialAgreementAchieved(recordingUser.Id, "Andrzej Strzelba", "test"));
        }
    }
}

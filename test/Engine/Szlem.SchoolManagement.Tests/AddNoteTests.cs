using FluentAssertions;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;
using Szlem.SchoolManagement.Impl;
using Szlem.SchoolManagement.Impl.Events;
using Xunit;

namespace Szlem.SchoolManagement.Tests
{
    public class AddNoteTests
    {
        protected NodaTime.Instant Now { get; }
        public AddNoteTests()
        {
            Now = NodaTime.SystemClock.Instance.GetCurrentInstant();
        }


        [Fact(DisplayName = "A1. Dodanie notatki musi zawierać ID szkoły i treść")]
        public void Command_must_contain_SchoolId_and_Content()
        {
            var school = new SchoolAggregate(SchoolId.With(default(Guid)));
            var author = new ApplicationUser();

            var command = new AddNote.Command() { SchoolId = default, Content = "    " };

            var result = school.AddNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.ValidationFailed>().Subject;
            error.Failures.Should().BeEquivalentTo(new[] {
                new Domain.Exceptions.ValidationFailure(nameof(command.SchoolId), AddNote_Messages.SchoolId_cannot_be_empty),
                new Domain.Exceptions.ValidationFailure(nameof(command.Content), AddNote_Messages.Content_cannot_be_empty)
            });
        }

        [Fact(DisplayName = "A2. Dodanie notatki musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_matching_school_aggregate()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser();

            var command = new AddNote.Command() {
                SchoolId = SchoolId.New.GetGuid(),
                Content = "test"
            };
            Assert.Throws<AggregateMismatchException>(() => school.AddNote(command, author, Now));
        }

        [Fact(DisplayName = "A3. Dodana notatka musi wskazywać na Id istniejącej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser();

            var command = new AddNote.Command() {
                SchoolId = school.Id.GetGuid(),
                Content = "treść"
            };
            var result = school.AddNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.ResourceNotFound>()
                .Which.Message.Should().BeEquivalentTo(Messages.School_not_found);
        }

        [Fact(DisplayName = "A4. Po dodaniu notatki, szkoła zawiera event NoteAdded")]
        public async Task After_adding_note__school_contains_NoteAdded_event()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser() { Id = Guid.NewGuid() };

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
                author).IsSuccess.Should().BeTrue();
            await school.CommitAsync(Mock.Of<EventFlow.EventStores.IEventStore>(), Mock.Of<EventFlow.Snapshots.ISnapshotStore>(), EventFlow.Core.SourceId.New, CancellationToken.None);
            
            var command = new AddNote.Command() {
                SchoolId = school.Id.GetGuid(),
                Content = "treść"
            };
            var result = school.AddNote(command, author, Now);

            result.IsSuccess.Should().BeTrue();
            var @event = school.UncommittedEvents.Should().ContainSingle()
                .Which.AggregateEvent.Should().BeOfType<NoteAdded>().Subject;

            @event.AuthorId.Should().Be(author.Id);
            @event.Content.Should().Be("treść");
            @event.Timestamp.Should().Be(Now);
            @event.NoteId.Should().NotBeEmpty();
        }

        [Fact(DisplayName = "A5. Po dodaniu notatki, agregat zawiera notatkę w kolekcji Notes")]
        public void After_adding_note__school_contains_single_note_in_Notes_collection()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser() { Id = Guid.NewGuid() };

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
                author).IsSuccess.Should().BeTrue();

            var command = new AddNote.Command() {
                SchoolId = school.Id.GetGuid(),
                Content = "treść"
            };
            var result = school.AddNote(command, author, Now);

            result.IsSuccess.Should().BeTrue();

            var noteId = result.Value;

            school.Notes.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new Note(noteId, author.Id, Now, command.Content));
        }

    }
}

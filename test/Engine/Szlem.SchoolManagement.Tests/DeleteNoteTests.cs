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
    public class DeleteNoteTests
    {
        protected NodaTime.Instant Now { get; }
        public DeleteNoteTests()
        {
            Now = NodaTime.SystemClock.Instance.GetCurrentInstant();
        }

        private async Task<SchoolAggregate> RegisterSchool()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser() { Id = Guid.NewGuid() };

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
                author).IsSuccess.Should().BeTrue();
            await school.CommitAsync(
                Mock.Of<EventFlow.EventStores.IEventStore>(),
                Mock.Of<EventFlow.Snapshots.ISnapshotStore>(),
                EventFlow.Core.SourceId.New, CancellationToken.None);
            return school;
        }

        private async Task<Guid> AddNote(SchoolAggregate school, string content, ApplicationUser author)
        {
            var result = school.AddNote(
                new AddNote.Command() { Content = content, SchoolId = school.Id.GetGuid() },
                author, Now
                );
            result.IsSuccess.Should().BeTrue();
            await school.CommitAsync(
                Mock.Of<EventFlow.EventStores.IEventStore>(),
                Mock.Of<EventFlow.Snapshots.ISnapshotStore>(),
                EventFlow.Core.SourceId.New, CancellationToken.None);
            return result.Value;
        }



        [Fact(DisplayName = "C1. Usunięcie notatki musi zawierać ID szkoły, ID notatki")]
        public void Command_must_contain__SchoolId_and_NoteId()
        {
            var school = new SchoolAggregate(SchoolId.With(default(Guid)));
            var author = new ApplicationUser();

            var command = new DeleteNote.Command() { SchoolId = default, NoteId = default };

            var result = school.DeleteNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.ValidationFailed>().Subject;
            error.Failures.Should().BeEquivalentTo(new[] {
                new ValidationFailure(nameof(command.SchoolId), DeleteNote_Messages.SchoolId_cannot_be_empty),
                new ValidationFailure(nameof(command.NoteId), DeleteNote_Messages.NoteId_cannot_be_empty)
            });
        }

        [Fact(DisplayName = "C2. Usunięcie notatki musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_matching_school_aggregate()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser();

            var command = new DeleteNote.Command() {
                SchoolId = SchoolId.New.GetGuid(),
                NoteId = Guid.NewGuid()
            };
            Assert.Throws<AggregateMismatchException>(() => school.DeleteNote(command, author, Now));
        }

        [Fact(DisplayName = "C3. Usuwana notatka musi wskazywać na Id istniejącej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser();

            var command = new DeleteNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = Guid.NewGuid()
            };
            var result = school.DeleteNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.ResourceNotFound>()
                .Which.Message.Should().BeEquivalentTo(Messages.School_not_found);
        }

        [Fact(DisplayName = "C4. Usuwana notatka musi wskazywać na Id istniejącej notatki")]
        public async Task NoteId_must_point_to_existing_note()
        {
            var school = await RegisterSchool();
            var author = new ApplicationUser() { Id = Guid.NewGuid() };

            var command = new DeleteNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = Guid.NewGuid()
            };
            var result = school.DeleteNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.DomainError>()
                .Which.Message.Should().Be(DeleteNote_Messages.Note_does_not_exist);
        }

        [Fact(DisplayName = "C5. Po usunięciu notatki, szkoła zawiera event NoteDeleted")]
        public async Task After_deleting_note__school_contains_NoteDeleted_event()
        {
            var school = await RegisterSchool();
            var author = new ApplicationUser() { Id = Guid.NewGuid() };
            var noteId = await AddNote(school, "test", author);

            var command = new DeleteNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = noteId
            };
            var result = school.DeleteNote(command, author, Now);

            result.IsSuccess.Should().BeTrue();
            var @event = school.UncommittedEvents.Should().ContainSingle()
                .Which.AggregateEvent.Should().BeOfType<NoteDeleted>().Subject;
            @event.Timestamp.Should().Be(Now);
            @event.NoteId.Should().Be(noteId);
            @event.DeletingUserId.Should().Be(author.Id);
        }

        [Fact(DisplayName = "C6. Po usunięciu notatki, agregat nie zawiera usuniętej notatki w kolekcji Notes")]
        public async Task After_deleting_note__school_does_not_contain_the_note_in_Notes_collection()
        {
            var school = await RegisterSchool();
            var author = new ApplicationUser() { Id = Guid.NewGuid() };
            var noteId1 = await AddNote(school, "test1", author);
            var noteId2 = await AddNote(school, "test2", author);
            var noteId3 = await AddNote(school, "test3", author);
            school.Notes.Should().HaveCount(3);

            var command = new DeleteNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = noteId3
            };
            var result = school.DeleteNote(command, author, Now);

            result.IsSuccess.Should().BeTrue();
            school.Notes.Should().HaveCount(2);
            school.Notes.Should().NotContain(x => x.NoteId == noteId3);
        }
    }
}

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
    public class EditNoteTests
    {
        protected NodaTime.Instant Now { get; }
        public EditNoteTests()
        {
            Now = NodaTime.SystemClock.Instance.GetCurrentInstant();
        }


        #region supporting code
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
        #endregion


        [Fact(DisplayName = "B1. Edycja notatki musi zawierać ID notatki i treść")]
        public void Command_must_contain__SchoolId_NoteId_and_Content()
        {
            var school = new SchoolAggregate(SchoolId.With(default(Guid)));
            var author = new ApplicationUser();

            var command = new EditNote.Command() { SchoolId = default, NoteId = default, Content = "  " };

            var result = school.EditNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            var error = result.Error.Should().BeOfType<Error.ValidationFailed>().Subject;
            error.Failures.Should().BeEquivalentTo(new[] {
                new ValidationFailure(nameof(command.SchoolId), EditNote_Messages.SchoolId_cannot_be_empty),
                new ValidationFailure(nameof(command.NoteId), EditNote_Messages.NoteId_cannot_be_empty),
                new ValidationFailure(nameof(command.Content), EditNote_Messages.Content_cannot_be_empty)
            });
        }

        [Fact(DisplayName = "B2. Edycja notatki musi zostać wywołana na agregacie o zgodnym SchoolId")]
        public void Command_must_be_executed_on_matching_school_aggregate()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser();

            var command = new EditNote.Command() {
                SchoolId = SchoolId.New.GetGuid(),
                NoteId = Guid.NewGuid(),
                Content = "test"
            };
            Assert.Throws<AggregateMismatchException>(() => school.EditNote(command, author, Now));
        }

        [Fact(DisplayName = "B3. Edytowana notatka musi wskazywać na Id istniejącej szkoły")]
        public void SchoolId_must_point_to_existing_school()
        {
            var school = new SchoolAggregate(SchoolId.New);
            var author = new ApplicationUser();

            var command = new EditNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = Guid.NewGuid(),
                Content = "test"
            };
            var result = school.EditNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.ResourceNotFound>()
                .Which.Message.Should().BeEquivalentTo(Messages.School_not_found);
        }

        [Fact(DisplayName = "B4. Edytowana notatka musi wskazywać na Id istniejącej notatki")]
        public async Task NoteId_must_point_to_existing_note()
        {
            var school = await RegisterSchool();
            var author = new ApplicationUser() { Id = Guid.NewGuid() };

            var command = new EditNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = Guid.NewGuid(),
                Content = "test"
            };
            var result = school.EditNote(command, author, Now);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().BeOfType<Error.DomainError>()
                .Which.Message.Should().Be(EditNote_Messages.Note_does_not_exist);
        }

        

        [Fact(DisplayName = "B5. Po edycji notatki, szkoła zawiera event NoteEdited")]
        public async Task After_editing_note__school_contains_NoteEdited_event()
        {
            var school = await RegisterSchool();
            var author = new ApplicationUser() { Id = Guid.NewGuid() };
            var noteId = await AddNote(school, "test", author);

            var command = new EditNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = noteId,
                Content = "nowa treść"
            };
            var result = school.EditNote(command, author, Now);

            result.IsSuccess.Should().BeTrue();
            var @event = school.UncommittedEvents.Should().ContainSingle()
                .Which.AggregateEvent.Should().BeOfType<NoteEdited>().Subject;
            @event.Timestamp.Should().Be(Now);
            @event.NoteId.Should().Be(noteId);
            @event.EditingUserId.Should().Be(author.Id);
            @event.Content.Should().Be("nowa treść");
        }

        [Fact(DisplayName = "B6. Po edycji notatki, agregat zawiera zmienioną notatkę w kolekcji Notes")]
        public async Task After_editing_note__school_contains_changed_note_in_Notes_collection()
        {
            var school = await RegisterSchool();
            var author = new ApplicationUser() { Id = Guid.NewGuid() };
            var noteId1 = await AddNote(school, "test1", author);
            var noteId2 = await AddNote(school, "test2", author);
            var noteId3 = await AddNote(school, "test3", author);
            school.Notes.Should().HaveCount(3);

            var command = new EditNote.Command() {
                SchoolId = school.Id.GetGuid(),
                NoteId = noteId1,
                Content = "nowa treść"
            };
            var result = school.EditNote(command, author, Now);

            result.IsSuccess.Should().BeTrue();
            school.Notes.Should().HaveCount(3);
            school.Notes.Should().NotContain(x => x.Content == "test1");
            school.Notes.Should().Contain(x => x.Content == "nowa treść");
        }
    }
}

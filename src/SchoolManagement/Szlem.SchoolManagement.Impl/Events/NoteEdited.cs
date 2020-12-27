using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace Szlem.SchoolManagement.Impl.Events
{
    [EventVersion("Szlem.SchoolManagement.School.NoteEdited", 1)]
    internal class NoteEdited : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Instant Timestamp { get; }
        public Guid NoteId { get; }
        public Guid EditingUserId { get; }
        public string Content { get; } = string.Empty;

        public NoteEdited(Instant timestamp, Guid noteId, Guid editingUserId, string content)
        {
            Guard.Against.Default(timestamp, nameof(timestamp));
            Guard.Against.Default(noteId, nameof(noteId));
            Guard.Against.Default(editingUserId, nameof(editingUserId));
            Guard.Against.NullOrWhiteSpace(content, nameof(content));

            Timestamp = timestamp;
            NoteId = noteId;
            EditingUserId = editingUserId;
            Content = content;
        }
    }
}

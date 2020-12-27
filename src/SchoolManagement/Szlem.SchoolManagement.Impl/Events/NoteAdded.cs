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
    [EventVersion("Szlem.SchoolManagement.School.NoteAdded", 1)]
    internal class NoteAdded : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Instant Timestamp { get; }
        public Guid NoteId { get; }
        public Guid AuthorId { get; }
        public string Content { get; } = string.Empty;

        public NoteAdded(Instant timestamp, Guid noteId, Guid authorId, string content)
        {
            Guard.Against.Default(timestamp, nameof(timestamp));
            Guard.Against.Default(noteId, nameof(noteId));
            Guard.Against.Default(authorId, nameof(authorId));
            Guard.Against.NullOrWhiteSpace(content, nameof(content));

            Timestamp = timestamp;
            NoteId = noteId;
            AuthorId = authorId;
            Content = content;
        }
    }
}

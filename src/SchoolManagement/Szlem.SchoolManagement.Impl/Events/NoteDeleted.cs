using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.SchoolManagement.Impl.Events
{
    [EventVersion("Szlem.SchoolManagement.School.NoteDeleted", 1)]
    internal class NoteDeleted : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Instant Timestamp { get; }
        public Guid NoteId { get; }
        public Guid DeletingUserId { get; }

        public NoteDeleted(Instant timestamp, Guid noteId, Guid deletingUserId)
        {
            Guard.Against.Default(timestamp, nameof(timestamp));
            Guard.Against.Default(noteId, nameof(noteId));
            Guard.Against.Default(deletingUserId, nameof(deletingUserId));
            Timestamp = timestamp;
            NoteId = noteId;
            DeletingUserId = deletingUserId;
        }
    }
}

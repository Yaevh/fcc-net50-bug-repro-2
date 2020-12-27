using Ardalis.GuardClauses;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Engine.Infrastructure;

#nullable enable
namespace Szlem.SchoolManagement.Impl
{
    internal class Note : IHaveOwner
    {
        public Guid NoteId { get; }
        public Guid AuthorId { get; }
        public Instant CreationTimestamp { get; }
        private Instant? _lastEditTimestamp = null;
        public Instant? LastEditTimestamp { get => _lastEditTimestamp; set { _lastEditTimestamp = value; WasEdited = true; } }
        public bool WasEdited { get; private set; }
        public string Content { get; set; } = string.Empty;

        public Note(Guid noteId, Guid authorId, Instant timestamp, string content)
        {
            Guard.Against.Default(noteId, nameof(noteId));
            Guard.Against.Default(authorId, nameof(authorId));
            Guard.Against.Default(timestamp, nameof(timestamp));
            Guard.Against.NullOrWhiteSpace(content, nameof(content));
            NoteId = noteId;
            AuthorId = authorId;
            CreationTimestamp = timestamp;
            Content = content;
        }

        public bool IsOwner(Guid userId) => AuthorId == userId;
    }
}

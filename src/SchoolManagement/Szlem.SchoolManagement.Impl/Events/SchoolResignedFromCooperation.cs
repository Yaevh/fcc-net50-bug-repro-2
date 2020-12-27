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
    [EventVersion("Szlem.SchoolManagement.School.SchoolResignedFromCooperation", 1)]
    internal class SchoolResignedFromCooperation : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Guid RecordingUserId { get; }
        public LocalDate? PotentialNextContactDate { get; }
        public string AdditionalNotes { get; } = string.Empty;

        public SchoolResignedFromCooperation(Guid recordingUserId, LocalDate? potentialNextContactDate, string additionalNotes)
        {
            Guard.Against.Default(recordingUserId, nameof(recordingUserId));
            Guard.Against.Null(additionalNotes, nameof(additionalNotes));

            RecordingUserId = recordingUserId;
            PotentialNextContactDate = potentialNextContactDate;
            AdditionalNotes = additionalNotes;
        }
    }
}

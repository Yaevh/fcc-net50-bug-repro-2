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
    [EventVersion("Szlem.SchoolManagement.School.InitialAgreementAchieved", 1)]
    internal class InitialAgreementAchieved : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Guid RecordingUserId { get; }

        public string AgreeingPersonName { get; } = string.Empty;

        public string? AdditionalNotes { get; }

        public InitialAgreementAchieved(
            Guid recordingUserId,
            string agreeingPersonName,
            string? additionalNotes = null)
        {
            Guard.Against.Default(recordingUserId, nameof(recordingUserId));
            Guard.Against.NullOrWhiteSpace(agreeingPersonName, nameof(agreeingPersonName));

            RecordingUserId = recordingUserId;
            AgreeingPersonName = agreeingPersonName;
            AdditionalNotes = additionalNotes;
        }
    }
}

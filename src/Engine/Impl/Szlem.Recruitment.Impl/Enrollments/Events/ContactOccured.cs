using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Recruitment.Enrollments;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.ContactOccured", 1)]
    internal class ContactOccured : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Guid RecordingUserId { get; }
        public CommunicationChannel CommunicationChannel { get; }
        public string Content { get; }
        public string AdditionalNotes { get; }

        public ContactOccured(Guid recordingUserId, CommunicationChannel communicationChannel, string content, string additionalNotes)
        {
            Guard.Against.Default(recordingUserId, nameof(recordingUserId));
            Guard.Against.Default(communicationChannel, nameof(communicationChannel));
            Guard.Against.NullOrWhiteSpace(content, nameof(content));
            Guard.Against.Null(additionalNotes, nameof(additionalNotes));

            RecordingUserId = recordingUserId;
            CommunicationChannel = communicationChannel;
            Content = content;
            AdditionalNotes = additionalNotes;
        }
    }
}

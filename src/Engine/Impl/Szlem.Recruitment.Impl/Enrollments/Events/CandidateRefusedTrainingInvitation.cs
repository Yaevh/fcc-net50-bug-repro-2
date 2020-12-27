using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.CandidateRefusedTrainingInvitation", 1)]
    internal class CandidateRefusedTrainingInvitation : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Guid RecordingCoordinatorID { get; }
        public Recruitment.Enrollments.CommunicationChannel CommunicationChannel { get; }
        public string RefusalReason { get; }
        public string AdditionalNotes { get; }

        public CandidateRefusedTrainingInvitation(
            Guid recordingCoordinatorID,
            Recruitment.Enrollments.CommunicationChannel communicationChannel,
            string refusalReason,
            string additionalNotes)
        {
            Guard.Against.Default(recordingCoordinatorID, nameof(recordingCoordinatorID));
            Guard.Against.Default(communicationChannel, nameof(communicationChannel));
            Guard.Against.Null(refusalReason, nameof(refusalReason));
            Guard.Against.Null(additionalNotes, nameof(additionalNotes));

            RecordingCoordinatorID = recordingCoordinatorID;
            CommunicationChannel = communicationChannel;
            RefusalReason = refusalReason;
            AdditionalNotes = additionalNotes;
        }
    }
}

using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.CandidateAcceptedTrainingInvitation", 1)]
    internal class CandidateAcceptedTrainingInvitation : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Guid RecordingCoordinatorID { get; }
        public Recruitment.Enrollments.CommunicationChannel CommunicationChannel { get; }
        public int SelectedTrainingID { get; }
        public string AdditionalNotes { get; }

        public CandidateAcceptedTrainingInvitation(
            Guid recordingCoordinatorID,
            Recruitment.Enrollments.CommunicationChannel communicationChannel,
            int selectedTrainingID,
            string additionalNotes)
        {
            Guard.Against.Default(recordingCoordinatorID, nameof(recordingCoordinatorID));
            Guard.Against.Default(communicationChannel, nameof(communicationChannel));
            Guard.Against.Default(selectedTrainingID, nameof(selectedTrainingID));
            Guard.Against.Null(additionalNotes, nameof(additionalNotes));

            RecordingCoordinatorID = recordingCoordinatorID;
            CommunicationChannel = communicationChannel;
            SelectedTrainingID = selectedTrainingID;
            AdditionalNotes = additionalNotes;
        }
    }
}

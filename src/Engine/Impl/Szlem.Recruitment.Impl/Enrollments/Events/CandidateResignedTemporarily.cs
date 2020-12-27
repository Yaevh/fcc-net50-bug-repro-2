using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.CandidateResignedTemporarily", 1)]
    internal class CandidateResignedTemporarily : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Guid RecordingCoordinatorID { get; }
        public Recruitment.Enrollments.CommunicationChannel CommunicationChannel { get; }
        public string ResignationReason { get; }
        public string AdditionalNotes { get; }
        public NodaTime.LocalDate? ResumeDate { get; }

        public CandidateResignedTemporarily(
            Guid recordingCoordinatorID,
            Recruitment.Enrollments.CommunicationChannel communicationChannel,
            string resignationReason,
            string additionalNotes,
            NodaTime.LocalDate? resumeDate = null)
        {
            Guard.Against.Default(recordingCoordinatorID, nameof(recordingCoordinatorID));
            Guard.Against.Default(communicationChannel, nameof(communicationChannel));
            Guard.Against.Null(resignationReason, nameof(resignationReason));
            Guard.Against.Null(additionalNotes, nameof(additionalNotes));

            RecordingCoordinatorID = recordingCoordinatorID;
            CommunicationChannel = communicationChannel;
            ResignationReason = resignationReason;
            AdditionalNotes = additionalNotes;
            ResumeDate = resumeDate;
        }
    }
}

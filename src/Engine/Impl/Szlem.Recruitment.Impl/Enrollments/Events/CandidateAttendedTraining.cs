using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.CandidateAttendedTraining", 1)]
    internal class CandidateAttendedTraining : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Guid RecordingCoordinatorID { get; }
        public int TrainingID { get; }
        public string AdditionalNotes { get; }

        public CandidateAttendedTraining(Guid recordingCoordinatorID, int trainingID, string additionalNotes)
        {
            Guard.Against.Default(recordingCoordinatorID, nameof(recordingCoordinatorID));
            Guard.Against.Default(trainingID, nameof(trainingID));
            Guard.Against.Null(additionalNotes, nameof(additionalNotes)); // may be empty
            RecordingCoordinatorID = recordingCoordinatorID;
            TrainingID = trainingID;
            AdditionalNotes = additionalNotes;
        }
    }
}

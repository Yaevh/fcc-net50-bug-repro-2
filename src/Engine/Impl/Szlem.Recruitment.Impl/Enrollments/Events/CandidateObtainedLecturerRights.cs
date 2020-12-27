using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Recruitment.Impl.Enrollments.Events
{
    [EventVersion("Szlem.Recruitment.CandidateObtainedLecturerRights", 1)]
    internal class CandidateObtainedLecturerRights : AggregateEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId>
    {
        public Guid GrantingCoordinatorID { get; }
        public string AdditionalNotes { get; }

        public CandidateObtainedLecturerRights(Guid grantingCoordinatorID, string additionalNotes)
        {
            Guard.Against.Default(grantingCoordinatorID, nameof(grantingCoordinatorID));
            GrantingCoordinatorID = grantingCoordinatorID;
            AdditionalNotes = additionalNotes ?? throw new ArgumentNullException(nameof(additionalNotes));
        }
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;


namespace Szlem.Recruitment.Trainings
{
    public static class Details
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Maybe<TrainingDetails>>
        {
            public int TrainingId { get; set; }
        }

#nullable enable
        public abstract class TrainingDetails
        {
            public int Id { get; set; }
            public string City { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public OffsetDateTime Start { get; set; }
            public OffsetDateTime End { get; set; }
            public Duration Duration { get; set; }
            public Guid CoordinatorId { get; set; }
            public string CoordinatorName { get; set; } = string.Empty;
            public TrainingTiming Timing { get; set; }
            public abstract IReadOnlyCollection<CandidateSummary> AllCandidates { get; }
            public IReadOnlyCollection<CandidateSummary> InvitedCandidates { get; set; } = Array.Empty<CandidateSummary>();
            public IReadOnlyCollection<TrainingNote> Notes { get; set; } = Array.Empty<TrainingNote>();
        }

        public class FutureTrainingDetails : TrainingDetails
        {
            public IReadOnlyCollection<FutureTrainingParticipant> PreferringCandidates { get; set; } = Array.Empty<FutureTrainingParticipant>();
            public IReadOnlyCollection<FutureTrainingParticipant> AvailableCandidates { get; set; } = Array.Empty<FutureTrainingParticipant>();
            public override IReadOnlyCollection<CandidateSummary> AllCandidates =>
                InvitedCandidates.Concat(AvailableCandidates).Concat(PreferringCandidates).Distinct().ToArray();
        }

        public class CurrentTrainingDetails : TrainingDetails
        {
            public IReadOnlyCollection<CurrentTrainingParticipant> PreferringCandidates { get; set; } = Array.Empty<CurrentTrainingParticipant>();
            public override IReadOnlyCollection<CandidateSummary> AllCandidates => InvitedCandidates.Concat(PreferringCandidates).Distinct().ToArray();
        }

        public class PastTrainingDetails : TrainingDetails
        {
            public IReadOnlyCollection<PastTrainingParticipant> PresentCandidates { get; set; } = Array.Empty<PastTrainingParticipant>();
            public IReadOnlyCollection<PastTrainingParticipant> AbsentCandidates { get; set; } = Array.Empty<PastTrainingParticipant>();
            public IReadOnlyCollection<PastTrainingParticipant> UnreportedCandidates { get; set; } = Array.Empty<PastTrainingParticipant>();
            public IReadOnlyCollection<PastTrainingParticipant> PreferringCandidates { get; set; } = Array.Empty<PastTrainingParticipant>();
            public override IReadOnlyCollection<CandidateSummary> AllCandidates =>
                PresentCandidates.Concat(UnreportedCandidates).Concat(AbsentCandidates).Concat(InvitedCandidates).Concat(PreferringCandidates).Distinct().ToArray();
        }

        public abstract class CandidateSummary : IEquatable<CandidateSummary>
        {
            public Guid Id { get; set; }
            public string FullName { get; set; } = string.Empty;
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public EmailAddress Email { get; set; }
            public PhoneNumber Phone { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
            public bool HasLecturerRights { get; set; }
            public bool HasResignedPermanently { get; set; }
            public bool HasResignedTemporarily { get; set; }
            public LocalDate? ResignationEndDate { get; set; }
            public bool HasRefusedTraining { get; set; }
            public string? RefusalReason { get; set; } = string.Empty;

            public override bool Equals(object obj) => Equals(obj as CandidateSummary);
            public override int GetHashCode() => HashCode.Combine(Id);
            public bool Equals(CandidateSummary? other)
            {
                return other != null && (ReferenceEquals(this, other) || Id == other.Id);
            }
        }

        public class CurrentTrainingParticipant : CandidateSummary
        {
            public bool ChoseAnotherTraining { get; set; }
        }

        public class PastTrainingParticipant : CandidateSummary
        {
            public bool WasAbsent { get; set; }
            public bool ChoseAnotherTraining { get; set; }
            public bool WasPresentButDidNotAcceptedAsLecturer { get; set; }
            public bool WasPresentAndAcceptedAsLecturer { get; set; }
            /// <summary>
            /// Brak danych o obecności
            /// </summary>
            public bool IsUnreported { get; set; }
            public bool IsInvited { get; set; }
            public bool CanRecordTrainingResults { get; set; }
            public bool CanRecordTrainingResultsConditionally { get; set; }
        }

        public class FutureTrainingParticipant : CandidateSummary
        {
            public bool HasAccepted { get; set; }
            public bool CanBeInvited { get; set; }
            public bool ChoseAnotherTraining { get; set; }
        }

        public class TrainingNote
        {
            public string AuthorName { get; set; } = string.Empty;
            public ZonedDateTime Timestamp { get; set; }
            public string Content { get; set; } = string.Empty;
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.TrainingId).NotEmpty();
            }
        }
    }
}

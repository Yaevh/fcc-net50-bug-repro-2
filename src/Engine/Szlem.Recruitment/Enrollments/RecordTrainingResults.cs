using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Enrollments
{
    public static class RecordTrainingResults
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Domain.Error>>
        {
            public Guid EnrollmentId { get; set; }
            public int TrainingId { get; set; }
            public TrainingResult TrainingResult { get; set; }
            public string AdditionalNotes { get; set; }
        }

        public enum TrainingResult
        {
            Absent,
            PresentButNotAcceptedAsLecturer,
            PresentAndAcceptedAsLecturer
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.EnrollmentId).NotEmpty();
                RuleFor(x => x.TrainingId).NotEmpty();
                When(x => x.TrainingResult == TrainingResult.PresentButNotAcceptedAsLecturer,
                    () => RuleFor(x => x.AdditionalNotes).NotEmpty()
                        .WithMessage(RecordTrainingResults_Messages.IfCandidateWasNotAccepted_CommandMustContainExplanation));
            }
        }
    }
}

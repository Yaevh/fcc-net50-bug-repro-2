using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Enrollments
{
    public static class RecordResignation
    {
        public enum ResignationType
        {
            Temporary = 1,
            Permanent = 2
        }

        [Authorize(AuthorizationPolicies.OwningCandidateOrCoordinator)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid EnrollmentId { get; set; }
            public CommunicationChannel CommunicationChannel { get; set; }
            public string ResignationReason { get; set; }
            public string AdditionalNotes { get; set; }
            public ResignationType ResignationType { get; set; }
            public NodaTime.LocalDate? ResumeDate { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.EnrollmentId).NotEmpty();
                RuleFor(x => x.CommunicationChannel).NotEqual(CommunicationChannel.Unknown);
                RuleFor(x => x.ResignationType).NotEmpty();
                RuleFor(x => x.ResumeDate).Null().When(x => x.ResignationType == ResignationType.Permanent);
            }
        }
    }
}
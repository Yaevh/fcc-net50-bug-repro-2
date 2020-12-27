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
    public static class RecordAcceptedTrainingInvitation
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Domain.Error>>
        {
            public Guid EnrollmentId { get; set; }
            public CommunicationChannel CommunicationChannel { get; set; }
            public int SelectedTrainingID { get; set; }
            public string AdditionalNotes { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.EnrollmentId).NotEmpty();
                RuleFor(x => x.CommunicationChannel).NotEqual(CommunicationChannel.Unknown);
                RuleFor(x => x.SelectedTrainingID).NotEmpty();
            }
        }
    }
}

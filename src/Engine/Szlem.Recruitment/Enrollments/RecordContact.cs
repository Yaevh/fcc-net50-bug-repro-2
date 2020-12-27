using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Enrollments
{
    public static class RecordContact
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid EnrollmentId { get; set; }
            public CommunicationChannel CommunicationChannel { get; set; }

            [Display(Name = "Treść/opis kontaktu")]
            public string Content { get; set; }
            public string AdditionalNotes { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.EnrollmentId).NotEmpty();
                RuleFor(x => x.CommunicationChannel).NotEmpty();
                RuleFor(x => x.Content).NotEmpty();
            }
        }
    }
}

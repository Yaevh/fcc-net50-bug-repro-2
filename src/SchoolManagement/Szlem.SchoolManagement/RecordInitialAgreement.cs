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

#nullable enable
namespace Szlem.SchoolManagement
{
    public static class RecordInitialAgreement
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid SchoolId { get; set; }

            [Display(Name = "Imię i nazwisko lub stanowisko osoby z którą doszło do kontaktu")]
            public string AgreeingPersonName { get; set; } = string.Empty;

            [Display(Name = "Dodatkowe informacje i notatki")]
            public string? AdditionalNotes { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(RecordInitialAgreement_Messages.SchoolId_cannot_be_empty);
                RuleFor(x => x.AgreeingPersonName).NotEmpty().WithMessage(RecordInitialAgreement_Messages.ContactPersonName_cannot_be_empty);
            }
        }
    }
}

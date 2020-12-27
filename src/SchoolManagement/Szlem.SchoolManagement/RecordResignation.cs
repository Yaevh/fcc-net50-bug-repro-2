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
    public static class RecordResignation
    {
        /// <summary>
        /// Komenda wydawana, gdy szkoła zrezygnuje ze współpracy z projektem (w trakcie negocjacji lub w trakcie trwania projektu)
        /// </summary>
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid SchoolId { get; set; }

            [Display(Name = "Kiedy możemy ponownie nawiązać kontakt? (np. na początku następnego roku szkolnego)")]
            public NodaTime.LocalDate? PotentialNextContactDate { get; set; }

            [Display(Name = "Dodatkowe notatki i uwagi")] public string? AdditionalNotes { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(RecordResignation_Messages.SchoolId_cannot_be_empty);
            }
        }
    }
}

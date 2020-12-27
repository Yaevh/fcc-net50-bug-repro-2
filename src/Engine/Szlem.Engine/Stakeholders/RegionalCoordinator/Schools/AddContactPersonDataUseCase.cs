using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Domain;
using Szlem.Models;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class AddContactPersonDataUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest
        {
            public int SchoolID { get; set; }

            [Display(Name = "Imię i nazwisko")]
            public string Name { get; set; }

            [Display(Name = "Stanowisko")]
            public string Position { get; set; }

            [Display(Name = "Adres e-mail")]
            public string Email { get; set; }

            [Display(Name = "Numer telefonu")]
            public PhoneNumber PhoneNumber { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolID).NotEmpty();
                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.Position).NotEmpty();
                RuleFor(x => x.Email).EmailAddress();
                RuleFor(x => x.PhoneNumber)
                    .NotEmpty();
            }
        }
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

#nullable enable
namespace Szlem.SchoolManagement
{
    public static class RegisterSchool
    {
        /// <summary>
        /// Zarejestruj ręcznie nową szkołę (przez uczestnika projektu)
        /// </summary>
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Guid, Error>>
        {
            [Display(Name = "Nazwa szkoły", Prompt = "np. I LO im. Mikołaja Kopernika")] public string Name { get; set; } = string.Empty;
            [Display(Name = "Miasto", Prompt = "np. Gdańsk")] public string City { get; set; } = string.Empty;
            [Display(Name = "Adres", Prompt = "np. Wały Piastowskie 6")] public string Address { get; set; } = string.Empty;
            [Display(Name = "Dane kontaktowe")] public IReadOnlyList<ContactData>? ContactData { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty().WithMessage(RegisterSchool_Messages.SchoolName_CannotBeEmpty);
                RuleFor(x => x.City).NotEmpty().WithMessage(RegisterSchool_Messages.City_CannotBeEmpty);
                RuleFor(x => x.Address).NotEmpty().WithMessage(RegisterSchool_Messages.Address_Cannot_be_empty);
                RuleFor(x => x.ContactData).NotEmpty().WithMessage(RegisterSchool_Messages.ContactData_cannot_be_empty);
                RuleForEach(x => x.ContactData).SetValidator(new ContactDataValidator());
                RuleFor(x => x.ContactData)
                    .Must(coll => coll.Where(x => x.EmailAddress != null).GroupBy(x => x.EmailAddress).All(x => x.Count() == 1))
                    .When(x => x.ContactData != null)
                    .WithMessage(RegisterSchool_Messages.ContactData_emails_and_phone_numbers_cannot_repeat_themselves);
                RuleFor(x => x.ContactData)
                    .Must(coll => coll.Where(x => x.PhoneNumber != null).GroupBy(x => x.PhoneNumber).All(x => x.Count() == 1))
                    .When(x => x.ContactData != null)
                    .WithMessage(RegisterSchool_Messages.ContactData_emails_and_phone_numbers_cannot_repeat_themselves);
            }

            public class ContactDataValidator : AbstractValidator<ContactData>
            {
                public ContactDataValidator()
                {
                    RuleFor(x => x.PhoneNumber).NotEmpty().Unless(x => x.EmailAddress != null)
                        .WithMessage(RegisterSchool_Messages.Either_ContactData_PhoneNumber_or_ContactData_EmailAddress_must_be_provided);
                    RuleFor(x => x.EmailAddress).NotEmpty().Unless(x => x.PhoneNumber != null)
                        .WithMessage(RegisterSchool_Messages.Either_ContactData_PhoneNumber_or_ContactData_EmailAddress_must_be_provided);
                }
            }
        }
    }
}
#nullable restore

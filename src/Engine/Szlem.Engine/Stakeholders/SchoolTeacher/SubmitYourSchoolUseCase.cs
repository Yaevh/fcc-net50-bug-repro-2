using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.Models;

namespace Szlem.Engine.Stakeholders.SchoolTeacher
{
    /// <summary>
    /// zgłoszenie szkoły do projektu
    /// </summary>
    public static class SubmitYourSchoolUseCase
    {
        [Authorize]
        public class Command : IRequest<Result>
        {
            public string Name { get; set; }

            public string Address { get; set; }

            public string City { get; set; }

            public Uri Website { get; set; }

            public EmailAddress Email { get; set; }

            public PhoneNumber PhoneNumber { get; set; }

            /// <summary>
            /// imię i nazwisko osoby zgłaszającej szkołę
            /// </summary>
            public string SubmitterName { get; set; }

            /// <summary>
            /// email osoby zgłaszającej szkołę
            /// </summary>
            public EmailAddress SubmitterEmail { get; set; }

            /// <summary>
            /// numer telefonu osoby zgłaszającej szkołę
            /// </summary>
            public PhoneNumber SubmitterPhoneNumber { get; set; }

            /// <summary>
            /// stanowisko osoby zgłaszającej szkołę
            /// </summary>
            public string SubmitterPosition { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.Name).NotEmpty();
                RuleFor(x => x.Address).NotEmpty();
                RuleFor(x => x.City).NotEmpty();
                RuleFor(x => x.Website).NotEmpty();
                RuleFor(x => x.Email)
                    .NotEmpty();
                RuleFor(x => x.PhoneNumber)
                    .NotEmpty();
                RuleFor(x => x.SubmitterName).NotEmpty();
                RuleFor(x => x.SubmitterEmail)
                    .NotEmpty();
                RuleFor(x => x.SubmitterPhoneNumber)
                    .NotEmpty();
                RuleFor(x => x.SubmitterPosition).NotEmpty();
            }
        }

        public class Result
        {
            public Guid SchoolID { get; set; }
        }
    }
}

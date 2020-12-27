using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.Models;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class AddBasicSchoolDataUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest
        {
            public string SchoolName { get; set; }

            public string City { get; set; }

            public string Address { get; set; }

            public string Website { get; set; }

            public string Email { get; set; }

            public PhoneNumber PhoneNumber { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolName).NotEmpty();
                RuleFor(x => x.City).NotEmpty();
                RuleFor(x => x.Address).NotEmpty();

                RuleFor(x => x.Website).Uri();
                RuleFor(x => x.Email).EmailAddress();
                RuleFor(x => x.PhoneNumber)
                    .NotEmpty();
            }
        }

        public class SchoolAlreadyExistsException : Exceptions.InvalidRequestException
        {
            public int SchoolID { get; }

            public SchoolAlreadyExistsException(string message, int id) : base(message) => SchoolID = id;
        }
    }
}

using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using Szlem.Domain;
using Szlem.Models;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Lecturers
{
    public static class Details
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<LecturerDetails>
        {
            public int ID { get; set; }
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.ID).NotEmpty();
            }
        }
        
        public class LecturerDetails
        {
            public int ID { get; set; }

            public string Name { get; set; }

            public MailAddress Email { get; set; }

            public PhoneNumber PhoneNumber { get; set; }

            public IReadOnlyCollection<string> Cities { get; set; }
            
            public bool CanDelete { get; set; }
        }
    }
}

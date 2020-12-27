using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Szlem.Domain;
using Szlem.Models;

namespace Szlem.Engine.Stakeholders.Admin.Enrollments.Import
{
    public class Request : IRequest
    {
        public int RecruitmentCampaignID { get; set; }

        /// <summary>
        /// Zgłoszenia do zaimportowania
        /// </summary>
        public IReadOnlyCollection<Submission> Submissions { get; set; }


        public class Submission
        {
            public DateTime? SubmissionDate { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public EmailAddress Email { get; set; }

            public PhoneNumber PhoneNumber { get; set; }

            public IReadOnlyCollection<string> Cities { get; set; }

            public string Province { get; set; }

            public string AboutMe { get; set; }

            public string WhatDoYouExpectFromTheProject { get; set; }


            public class Validator : AbstractValidator<Submission>
            {
                public Validator()
                {
                    RuleFor(x => x.FirstName).NotEmpty();
                    RuleFor(x => x.LastName).NotEmpty();
                    RuleFor(x => x.Email).NotEmpty();
                    RuleFor(x => x.PhoneNumber).NotEmpty();
                    RuleFor(x => x.Cities).Must(x => x.Any()).WithMessage("zgłoszenie powinno zawierać co najmniej jedno miasto");
                    RuleForEach(x => x.Cities).NotEmpty();
                    RuleFor(x => x.Province).NotEmpty();
                    RuleFor(x => x.AboutMe).NotEmpty();
                }
            }
        }

        public class Validator : AbstractValidator<Request>
        {
            public Validator()
            {
                RuleFor(x => x.RecruitmentCampaignID).NotEmpty();
                RuleForEach(x => x.Submissions).SetValidator(new Submission.Validator());
            }
        }
    }
}

using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Campaigns
{
    public static class Create
    {
        public static class ErrorMessages
        {
            public const string StartDateCannotBeGreaterThanEndDate = "Data rozpoczęcia nie może być późniejsza niż data zakończenia";
            public const string StartDateMustBeInTheFuture = "Data rozpoczęcia musi być datą z przyszłości";
            public const string CampaignCannotStartBeforeEditionStart = "Kampania nie może rozpocząć się przez rozpoczęciem edycji";
            public const string CampaignMustEndBeforeEditionEnd = "Kampania musi zakończyć się przed końcem roku szkolnego";
            public const string CampaignsCannotOverlap = "Kampanie nie mogą się pokrywać czasowo";
        }

        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Command : IRequest<Result<Response, Error>>
        {
            public string Name { get; set; }
            public NodaTime.OffsetDateTime StartDateTime { get; set; }
            public NodaTime.OffsetDateTime EndDateTime { get; set; }
            public int EditionID { get; set; }

            public class Validator : AbstractValidator<Command>
            {
                public Validator(NodaTime.IClock clock)
                {
                    RuleFor(x => x.StartDateTime.ToInstant())
                        .LessThanOrEqualTo(x => x.EndDateTime.ToInstant())
                        .WithMessage(ErrorMessages.StartDateCannotBeGreaterThanEndDate)
                        .WithName(nameof(StartDateTime));
                    RuleFor(x => x.StartDateTime.ToInstant())
                        .GreaterThan(x => clock.GetCurrentInstant())
                        .WithMessage(ErrorMessages.StartDateMustBeInTheFuture)
                        .WithName(nameof(StartDateTime));
                }
            }
        }

        public class Response
        {
            public int ID { get; set; }
        }
    }
}

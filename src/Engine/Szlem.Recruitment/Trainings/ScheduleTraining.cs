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

namespace Szlem.Recruitment.Trainings
{
#nullable enable
    public static class ScheduleTraining
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Response, Szlem.Domain.Error>>
        {
            [Display(Name = "Kampania rekrutacyjna")]
            public int CampaignID { get; set; }

            [Display(Name = "Data i czas rozpoczęcia szkolenia")]
            public NodaTime.LocalDateTime StartDateTime { get; set; }
            [Display(Name = "Data i czas zakończenia szkolenia")]
            public NodaTime.LocalDateTime EndDateTime { get; set; }

            [Display(Name = "Miasto")]
            public string City { get; set; } = string.Empty;

            [Display(Name = "Miejsce szkolenia (adres)")]
            public string Address { get; set; } = string.Empty;

            [Display(Name = "Notatki")]
            public string? Notes { get; set; }
        }

        public class Response
        {
            public int ID { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.CampaignID).NotEmpty();
                RuleFor(x => x.City).NotEmpty();
                RuleFor(x => x.Address).NotEmpty();
                RuleFor(x => x.StartDateTime).NotEmpty();
                RuleFor(x => x.EndDateTime).NotEmpty();
                RuleFor(x => x.StartDateTime).LessThan(x => x.EndDateTime)
                    .WithMessage(ErrorMessages.StartDateTimeCannotBeGreaterThanEndDateTime);
                RuleFor(x => x.StartDateTime.Date).Equal(x => x.EndDateTime.Date)
                    .OverridePropertyName(nameof(Command.StartDateTime))
                    .WithMessage(ErrorMessages.Training_must_begin_and_end_on_the_same_day);
                RuleFor(x => x.EndDateTime.Date).Equal(x => x.StartDateTime.Date)
                    .OverridePropertyName(nameof(Command.EndDateTime))
                    .WithMessage(ErrorMessages.Training_must_begin_and_end_on_the_same_day);
                RuleFor(x => x.Notes).Must(note => string.IsNullOrEmpty(note) == false)
                    .When(x => x.Notes != null)
                    .WithMessage(ErrorMessages.NoteCannotBeEmpty);
            }
        }
    }
}

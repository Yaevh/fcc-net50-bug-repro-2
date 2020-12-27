using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Models.Editions;
using Szlem.SharedKernel;

namespace Szlem.Engine.Editions.Editions
{
    public class CreateUseCase
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Command : IRequest<Result<Response, Domain.Error>>
        {
            [DataType(DataType.Date)]
            public DateTime StartDate { get; set; }

            [DataType(DataType.Date)]
            public DateTime EndDate { get; set; }

            public string Name { get; set; }
            
            public EditionStatistics CumulativeStatistics { get; private set; } = new EditionStatistics();
        }

        public class Response
        {
            public int Id { get; set; }
        }
        
        public class CommandValidator : AbstractValidator<Command>
        {
            public const string StartDateCannotBeEmpty = "Start date cannot be empty";
            public const string EndDateCannotBeEmpty = "End date cannot be empty";
            public const string StartDateMustBeEarlierThanEndDate = "Start date must be earlier than end date";
            public const string EndDateMustBeLaterThanStartDate = "End date must be later than start date";
            
            public CommandValidator()
            {
                RuleFor(x => x.StartDate).NotEmpty().WithMessage(StartDateCannotBeEmpty);
                RuleFor(x => x.EndDate).NotEmpty().WithMessage(EndDateCannotBeEmpty);
                RuleFor(x => x.StartDate).LessThan(x => x.EndDate)
                    .When(x => x.StartDate != default && x.EndDate != default)
                    .WithMessage(StartDateMustBeEarlierThanEndDate);
                RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
                    .When(x => x.StartDate != default && x.EndDate != default)
                    .WithMessage(EndDateMustBeLaterThanStartDate);
                
                RuleFor(x => x.CumulativeStatistics).SetValidator(new Models.Editions.Validators.EditionStatisticsValidator());
            }
        }
    }
}

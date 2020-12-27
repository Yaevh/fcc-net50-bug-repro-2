using FluentValidation;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Szlem.Models.Editions.Validators
{
    public class EditionValidator : AbstractValidator<Edition>
    {
        private const string StartDateCannotBeEmpty = "Start date cannot be empty";
        private const string EndDateCannotBeEmpty = "End date cannot be empty";
        private const string StartDateMustBeEarlierThanEndDate = "Start date must be earlier than end date";
        private const string EndDateMustBeLaterThanStartDate = "End date must be later than start date";

        public EditionValidator()
        {
            RuleFor(x => x.StartDate).NotEmpty().WithMessage(StartDateCannotBeEmpty);
            RuleFor(x => x.EndDate).NotEmpty().WithMessage(EndDateCannotBeEmpty);
            RuleFor(x => x.StartDate).LessThan(x => x.EndDate).WithMessage(StartDateMustBeEarlierThanEndDate);
            RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate).WithMessage(EndDateMustBeLaterThanStartDate);

            RuleFor(x => x.ThisEditionStatistics).SetValidator(new EditionStatisticsValidator());
            RuleFor(x => x.CumulativeStatistics).SetValidator(new EditionStatisticsValidator());
        }
    }
}

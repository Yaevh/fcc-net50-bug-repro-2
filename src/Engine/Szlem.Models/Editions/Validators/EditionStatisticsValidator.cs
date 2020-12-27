using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Models.Editions.Validators
{
    public class EditionStatisticsValidator : AbstractValidator<EditionStatistics>
    {
        private const string CityCountMustBeNonNegative = "City count must be non-negative";
        private const string SchoolCountMustBeNonNegative = "School count must be non-negative";
        private const string StudentCountMustBeNonNegative = "Student count must be non-negative";
        private const string LessonCountMustBeNonNegative = "Lesson count must be non-negative";
        private const string LecturerCountMustBeNonNegative = "Lecturer count must be non-negative";

        public EditionStatisticsValidator()
        {
            RuleFor(x => x.CityCount).GreaterThanOrEqualTo(0).WithMessage(CityCountMustBeNonNegative);
            RuleFor(x => x.SchoolCount).GreaterThanOrEqualTo(0).WithMessage(SchoolCountMustBeNonNegative);
            RuleFor(x => x.StudentCount).GreaterThanOrEqualTo(0).WithMessage(StudentCountMustBeNonNegative);
            RuleFor(x => x.LessonCount).GreaterThanOrEqualTo(0).WithMessage(LessonCountMustBeNonNegative);
            RuleFor(x => x.LecturerCount).GreaterThanOrEqualTo(0).WithMessage(LecturerCountMustBeNonNegative);
        }
    }
}

using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Courses
{
    public static class AddConductedCourseUseCase
    {
        [Authorize(AuthorizationRoles.RegionalCoordinator)]
        public class Command
        {
            public int SchoolID { get; set; }

            public int EditionID { get; set; }

            [Display(Name = "Prowadzący", Description = "Osoby prowadzące cykl lekcji")]
            public IReadOnlyCollection<int> LecturerIDs { get; set; }

            [Display(Name = "Klasy", Description = "Klasa w której prowadzono lekcje; jeśli lekcje były prowadzone w więcej niż jednej klasie, oddziel je przecinkami lub średnikami")]
            public IReadOnlyCollection<string> Classes { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Data rozpoczęcia cyklu (może być orientacyjna)")]
            public DateTime StartDate { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Data zakończenia cyklu (może być orientacyjna)")]
            public DateTime EndDate { get; set; }

            [Display(Name = "Liczba przeprowadzonych lekcji")]
            public int ConductedLessonCount { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolID).NotEmpty();
                RuleFor(x => x.EditionID).NotEmpty();
                RuleFor(x => x.Classes).NotEmpty();
                RuleFor(x => x.StartDate).NotEmpty();
                RuleFor(x => x.EndDate).NotEmpty();
                RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate);
                RuleFor(x => x.ConductedLessonCount).NotEmpty();
            }
        }
    }
}

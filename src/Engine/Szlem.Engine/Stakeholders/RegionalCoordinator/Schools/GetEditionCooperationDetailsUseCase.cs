using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.SharedKernel;
using static Szlem.Engine.Stakeholders.RegionalCoordinator.Schools.GetTimetableUseCase;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    /// <summary>
    /// Pobiera szczegóły wspópracy ze szkołą w ramach danej edycji
    /// </summary>
    public static class GetEditionCooperationDetailsUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Response>
        {
            public int SchoolID { get; set; }
            public int EditionID { get; set; }
        }

        public class Validator : AbstractValidator<Query>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolID).NotEmpty();
                RuleFor(x => x.EditionID).NotEmpty();
            }
        }

        public class Response
        {
            [Display(Name = "Czy to bieżąca edycja?")]
            public bool IsCurrentEdition { get; set; }

            public int SchoolID { get; set; }
            [Display(Name = "Nazwa szkoły")]
            public string SchoolName { get; set; }
            [Display(Name = "Adres szkoły")]
            public string SchoolAddress { get; set; }
            [Display(Name = "Miasto")]
            public string SchoolCity { get; set; }

            [Display(Name = "Plan lekcji")]
            public Timetable Timetable { get; set; }

            public int EditionID { get; set; }

            [Display(Name = "Nazwa edycji")]
            public string EditionName { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Początek edycji")]
            public DateTime EditionStartDate { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Koniec edycji")]
            public DateTime EditionEndDate { get; set; }

            [Display(Name = "Ilość przeprowadzonych lekcji")]
            public int LessonCount { get; set; }

            [Display(Name = "Cykle lekcji")]
            public IReadOnlyCollection<CourseSummary> Courses { get; set; }

            [Display(Name = "Czy można rozpocząć nowy cykl?")]
            public bool CanStartNewCourse { get; set; }
        }

        /// <summary>
        /// Dane o cyklu lekcji
        /// </summary>
        public class CourseSummary
        {
            [Display(Name = "Czy można wyświetlić szczegóły")]
            public bool CanShowDetails { get; set; }

            [Display(Name = "Czy cykl trwa?")]
            public bool IsOngoing { get; set; }

            [Display(Name = "Klasa")]
            public string Class { get; set; }

            [Display(Name = "Prowadzący")]
            public IReadOnlyCollection<string> Lecturers { get; set; }
        }
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Szlem.Domain;
using Szlem.Models;
using Szlem.SharedKernel;

namespace Szlem.Engine.Stakeholders.RegionalCoordinator.Schools
{
    public static class DetailsUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<SchoolDetails>
        {
            public int ID { get; set; }
        }

        public class SchoolDetails
        {
            public int ID { get; set; }

            [Display(Name = "Nazwa szkoły")]
            public string Name { get; set; }

            [Display(Name = "Adres")]
            public string Address { get; set; }

            [Display(Name = "Miasto")]
            public string City { get; set; }

            [Display(Name = "Strona internetowa")]
            public Uri Website { get; set; }

            [Display(Name = "E-mail")]
            public string Email { get; set; }

            [Display(Name = "Nr telefonu")]
            public PhoneNumber PhoneNumber { get; set; }

            [Display(Name = "Osoby kontaktowe")]
            public IReadOnlyCollection<ContactPerson> Contacts { get; set; }

            [Display(Name = "Edycje")]
            public IReadOnlyCollection<EditionSummary> Editions { get; set; }

            [Display(Name = "Czy można dodać plan lekcji?")]
            public bool CanAddTimetable { get; set; }

            [Display(Name = "Czy można edytować plan lekcji?")]
            public bool CanEditTimetable { get; set; }

            [Display(Name = "Bieżący plan lekcji")]
            public Timetable CurrentTimetable { get; set; }

            [Display(Name = "Wszystkie plany lekcji")]
            public IReadOnlyCollection<Timetable> Timetables { get; set; }


            public class ContactPerson
            {
                public int ID { get; set; }

                [Display(Name = "Imię i nazwisko")]
                public string Name { get; set; }

                [Display(Name = "Nr telefonu")]
                public PhoneNumber PhoneNumber { get; set; }

                [Display(Name = "E-mail")]
                public string Email { get; set; }

                [Display(Name = "Stanowisko")]
                public string Position { get; set; }
            }

            public class EditionSummary
            {
                public int EditionID { get; set; }

                [Display(Name = "Edycja")]
                public string EditionName { get; set; }

                [Display(Name = "Ilość klas")]
                public int ClassCount { get; set; }

                [Display(Name = "Ilość lekcji")]
                public int LessonCount { get; set; }

                [Display(Name = "Prowadzący")]
                public IReadOnlyCollection<string> Lecturers { get; set; }

                public bool CanShowDetails { get; set; }
            }
            
        }
    }
}

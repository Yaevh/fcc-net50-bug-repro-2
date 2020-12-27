using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.Recruitment.Enrollments
{
    public static class GetSubmissions
    {
        public enum EnrollmentAge
        {
            LatestCampaign,
            OldCampaign
        }

        public enum SortBy
        {
            [Display(Name = "Imię")] FirstName,
            [Display(Name = "Nazwisko")] LastName,
            [Display(Name = "Data zgłoszenia")] Timestamp,
            [Display(Name = "Adres e-mail")] Email,
            [Display(Name = "Region")] Region,
            [Display(Name = "Data wznowienia (jeśli kandydat zrezygnował tymczasowo)")] ResumeDate
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<IPagedList<SubmissionSummary>>
        {
            [Display(Name = "Wyników na stronę")] public int PageSize { get; set; } = 50;
            /// <summary>
            /// One-based!
            /// </summary>
            [Display(Name = "Numer strony (licząc od 1)")] public int PageNo { get; set; } = 1;
            [Display(Name = "Szukana fraza")] public string SearchPattern { get; set; }
            [Display(Name = "Kampanie rekrutacyjne")] public IReadOnlyCollection<int> CampaignIds { get; set; }
            [Display(Name = "Szkolenia")] public IReadOnlyCollection<int> PreferredTrainingIds { get; set; }
            public EnrollmentAge? EnrollmentAge { get; set; }
            public bool? HasLecturerRights { get; set; }
            public bool? HasResigned { get; set; }
            public SortBy? SortBy { get; set; }
        }

        public class SubmissionSummary
        {
            public Guid Id { get; set; }
            [Display(Name = "Data zgłoszenia")] public NodaTime.OffsetDateTime Timestamp { get; set; }
            [Display(Name = "Imię")] public string FirstName { get; set; }
            [Display(Name = "Nazwisko")] public string LastName { get; set; }
            [Display(Name = "Imię i nazwisko")] public string FullName { get; set; }
            [Display(Name = "E-mail")] public EmailAddress Email { get; set; }
            [Display(Name = "Tel.")] public PhoneNumber PhoneNumber { get; set; }
            [Display(Name = "O mnie")] public string AboutMe { get; set; }
            [Display(Name = "Kampania rekrutacyjna")] public CampaignSummary Campaign { get; set; }
            [Display(Name = "Region")] public string Region { get; set; }
            
            [Display(Name = "Preferowane miasta")] public IReadOnlyCollection<string> PreferredLecturingCities { get; set; }
            [Display(Name = "Preferowane szkolenia")] public IReadOnlyCollection<PreferredTrainingSummary> PreferredTrainings { get; set; }

            /// <summary>
            /// Czy kandydat zgłosił się w bieżącej edycji?
            /// </summary>
            public bool IsCurrentSubmission { get; set; }

            /// <summary>
            /// Czy zgłoszenie pochodzi ze starej edycji?
            /// </summary>
            public bool IsOldSubmission { get; set; }

            /// <summary>
            /// Czy kandydat zdobył już uprawnienia prowadzącego?
            /// </summary>
            public bool HasLecturerRights { get; set; }

            /// <summary>
            /// Czy kandydat trwale zrezygnował z udziału w projekcie?
            /// </summary>
            public bool HasResignedPermanently { get; set; }

            /// <summary>
            /// Czy kandydat tymczasowo zrezygnował z udziału w projekcie?
            /// </summary>
            public bool HasResignedTemporarily { get; set; }

            /// <summary>
            /// Przewidywana data wznowienia działalności po tymczasowej rezygnacji
            /// </summary>
            public NodaTime.LocalDate? ResumeDate { get; set; }
        }

        public class CampaignSummary
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public NodaTime.LocalDate StartDate { get; set; }
            public NodaTime.LocalDate EndDate { get; set; }
        }

        public class PreferredTrainingSummary
        {
            public int ID { get; set; }
            public NodaTime.OffsetDateTime StartDateTime { get; set; }
            public NodaTime.OffsetDateTime EndDateTime { get; set; }
            public Guid CoordinatorID { get; set; }
            public string City { get; set; }
            public string Address { get; set; }
        }
    }
}

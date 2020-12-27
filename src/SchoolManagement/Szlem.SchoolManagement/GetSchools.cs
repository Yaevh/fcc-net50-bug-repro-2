using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.SchoolManagement
{
    public static class GetSchools
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<IPagedList<Summary>>
        {
            [Display(Name = "Ilość wyników na stronie")] public int PageSize { get; set; } = 25;
            [Display(Name = "Numer strony")] public int PageNo { get; set; } = 1;
            [Display(Name = "Fraza do wyszukania (nazwa szkoły, miasto albo ulica)")] public string SearchPattern { get; set; }
        }

        public class Summary
        {
            public Guid Id { get; set; }
            [Display(Name = "Nazwa szkoły")] public string Name { get; set; }
            [Display(Name = "Adres szkoły")] public string Address { get; set; }
            [Display(Name = "Miasto")] public string City { get; set; }
            public SchoolStatus Status { get; set; } = SchoolStatus.Unknown;
        }

        public enum SchoolStatus { Unknown, HasSignedAgreement, HasAgreedInitially, HasResigned }
    }
}

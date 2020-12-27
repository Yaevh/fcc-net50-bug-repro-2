using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.Editions.Editions
{
    public static class IndexUseCase
    {
        [Authorize(AuthorizationPolicies.ApprovedUsers)]
        public class Query : IRequest<EditionSummary[]>
        {
        }

        public class EditionSummary
        {
            public int ID { get; set; }

            public string Name { get; set; }

            [DataType(DataType.Date)]
            public NodaTime.LocalDate StartDate { get; set; }

            [DataType(DataType.Date)]
            public NodaTime.LocalDate EndDate { get; set; }

            public bool CanShowDetails { get; set; }
        }
    }
}

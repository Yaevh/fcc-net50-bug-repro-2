using CSharpFunctionalExtensions;
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
    public static class DetailsUseCase
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Result<EditionDetails, Szlem.Domain.Error>>
        {
            public int EditionID { get; set; }
        }


        public class EditionDetails
        {
            public int ID { get; set; }

            public NodaTime.LocalDate StartDate { get; set; }

            public NodaTime.LocalDate EndDate { get; set; }

            public string Name { get; set; }

            public EditionStatistics ThisEditionStatistics { get; set; }

            public EditionStatistics CumulativeStatistics { get; set; }

            public RecruitmentCampaignData[] RecruitmentCampaigns { get; set; }

            public bool CanAddRecruitmentCampaign { get; set; }

            public bool IsCurrent { get; set; }


            public class RecruitmentCampaignData
            {
                public int ID { get; set; }
                public string Name { get; set; }
                public NodaTime.OffsetDateTime StartDateTime { get; set; }
                public NodaTime.OffsetDateTime EndDateTime { get; set; }
            }
        }
    }
}

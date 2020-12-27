using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Campaigns
{
    public static class Index
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<Result<CampaignSummary[], Error>>
        {
            public int? EditionID { get; set; }
        }

        public class CampaignSummary
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public NodaTime.OffsetDateTime StartDateTime { get; set; }
            public NodaTime.OffsetDateTime EndDateTime { get; set; }
        }
    }
}

using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Recruitment.Campaigns;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.Recruitment.Trainings
{
#nullable enable
    public static class Index
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Query : IRequest<IReadOnlyCollection<TrainingSummary>>
        {
            public IReadOnlyCollection<int> CampaignIds { get; set; } = Array.Empty<int>();
            public NodaTime.Instant? From { get; set; }
            public NodaTime.Instant? To { get; set; }
            public string? City { get; set; }
            public Guid? CoordinatorId { get; set; }
        }
    }
#nullable restore
}

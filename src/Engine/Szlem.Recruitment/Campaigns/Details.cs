using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Recruitment.Trainings;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Campaigns
{
    public static class Details
    {
        [AllowAnonymous]
        public class Query : IRequest<Result<Campaign, Szlem.Domain.Error>>
        {
            public int CampaignID { get; set; }
        }

        public class Campaign
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public NodaTime.OffsetDateTime StartDateTime { get; set; }
            public NodaTime.OffsetDateTime EndDateTime { get; set; }
            public bool IsRecruitmentFormOpen { get; set; }
            public IReadOnlyCollection<TrainingSummary> Trainings { get; set; }
            public bool CanScheduleTraining { get; set; }

            public override string ToString() => $"{StartDateTime.Date} - {EndDateTime.Date}";
        }
    }
}

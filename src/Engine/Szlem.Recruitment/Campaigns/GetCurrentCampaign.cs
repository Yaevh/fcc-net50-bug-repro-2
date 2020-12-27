using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Campaigns
{
    public static class GetCurrentCampaign
    {
        [Authorize]
        public class Query : IRequest<Maybe<Details.Campaign>> { }
    }
}

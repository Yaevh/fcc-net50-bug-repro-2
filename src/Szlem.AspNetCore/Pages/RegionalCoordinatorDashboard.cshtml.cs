using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Pages
{
    [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
    public class RegionalCoordinatorDashboardModel : PageModel
    {
        public const string PageName = "RegionalCoordinatorDashboard";
        public static readonly string Route = $"/{PageName}";


        private readonly ISzlemEngine _engine;

        public RegionalCoordinatorDashboardModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public Maybe<Szlem.Recruitment.Campaigns.Details.Campaign> CurrentCampaign { get; private set; }


        public async Task OnGet()
        {
            CurrentCampaign = await _engine.Query(new Szlem.Recruitment.Campaigns.GetCurrentCampaign.Query());
        }
    }
}
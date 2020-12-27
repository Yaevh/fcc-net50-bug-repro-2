using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ISzlemEngine _engine;

        public IndexModel(ILogger<IndexModel> logger, ISzlemEngine engine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public Maybe<Szlem.Recruitment.Campaigns.Details.Campaign> CurrentCampaign { get; private set; }


        public async Task OnGet()
        {
            if (User.Identity.IsAuthenticated)
                CurrentCampaign = await _engine.Query(new Szlem.Recruitment.Campaigns.GetCurrentCampaign.Query());
            else
                CurrentCampaign = Maybe<Szlem.Recruitment.Campaigns.Details.Campaign>.None;
        }
    }
}

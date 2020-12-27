using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NodaTime;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Trainings;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages.Trainings
{
    [Authorize(Policy = AuthorizationPolicies.CoordinatorsOnly)]
    public class IndexModel : PageModel
    {
        public const string PageName = "Index";
        public static readonly string Route = Consts.TrainingPageRoute(PageName);

        private readonly ISzlemEngine _engine;
        private readonly IUserAccessor _userAccessor;
        private readonly IClock _clock;

        public IndexModel(ISzlemEngine engine, IUserAccessor userAccessor, IClock clock)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }


        public IReadOnlyCollection<TrainingSummary> Trainigns { get; private set; }
        public IReadOnlyCollection<Szlem.Recruitment.Campaigns.Index.CampaignSummary> Campaigns { get; private set; }

        [BindProperty]
        public ScheduleTraining.Command ScheduleTrainingCommand { get; set; }

        public async Task OnGet()
        {
            var user = await _userAccessor.GetUser();
            Trainigns = await _engine.Query(new Szlem.Recruitment.Trainings.Index.Query() { CoordinatorId = user.Id });

            var campaigns = await _engine.Query(new Szlem.Recruitment.Campaigns.Index.Query())
                .Map(res => res.Where(x => x.StartDateTime.ToInstant() > _clock.GetCurrentInstant()));

            Campaigns = campaigns.Value.ToArray();
        }

        public async Task<IActionResult> OnPostScheduleTraining()
        {
            var result = await _engine.Execute(ScheduleTrainingCommand);
            if (result.IsFailure)
                ModelState.AddModelErrors(result.Error, nameof(ScheduleTrainingCommand));

            await OnGet();
            return Page();
        }
    }
}

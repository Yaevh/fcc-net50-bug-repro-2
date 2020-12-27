using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.Engine;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.Enrollments;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages
{
    public class DetailsModel : PageModel
    {
        public const string Route = "/Details";

        private readonly ISzlemEngine _engine;

        public DetailsModel(ISzlemEngine engine, IUserAccessor userAccessor)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public Details.Campaign Campaign { get; set; }

        public IPagedList<GetSubmissions.SubmissionSummary> Submissions { get; private set; }


        [BindProperty]
        public Szlem.Recruitment.Trainings.ScheduleTraining.Command ScheduleTrainingCommand { get; set; }


        public async Task<IActionResult> OnGet(int id, int submissionsPageNo = 1)
        {
            var result = await _engine.Query(new Details.Query() { CampaignID = id });
            if (result.IsFailure)
            {
                switch (result.Error)
                {
                    case Domain.Error.ResourceNotFound _:
                        throw new Engine.Exceptions.ResourceNotFoundException();
                    case Domain.Error.BadRequest _:
                        throw new Engine.Exceptions.InvalidRequestException();
                }
            }

            Campaign = result.Value;
            Submissions = await _engine.Query(new GetSubmissions.Query() { CampaignIds = new[] { id }, PageNo = submissionsPageNo });

            return Page();
        }

        public async Task<IActionResult> OnPostScheduleTraining()
        {
            var result = await _engine.Execute(ScheduleTrainingCommand);
            if (result.IsFailure)
                CommonPageModelExtensions.AddModelErrors(ModelState, result.Error);

            return await OnGet(ScheduleTrainingCommand.CampaignID);
        }
    }
}
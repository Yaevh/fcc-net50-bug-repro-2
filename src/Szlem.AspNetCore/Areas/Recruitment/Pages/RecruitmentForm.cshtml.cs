using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.Enrollments;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages
{
    [AllowAnonymous]
    public class RecruitmentFormModel : PageModel
    {
        public const string PageName = "RecruitmentForm";
        public static readonly string Route = $"/{PageName}";
        public static string RouteFor(int campaignId) => $"{Route}/{campaignId.ToString()}";


        private readonly ISzlemEngine _engine;

        public RecruitmentFormModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        
        public IEnumerable<SelectListItem> AvailableTrainings { get; set; }

        [BindProperty]
        public int CampaignID { get; set; }

        [BindProperty]
        public SubmitRecruitmentForm.Command Command { get; set; }


        public async Task<IActionResult> OnGet([FromRoute] int campaignId)
        {
            CampaignID = campaignId;
            var result = await _engine.Query(new Details.Query() { CampaignID = campaignId });
            if (result.IsFailure)
            {
                ModelState.AddModelError(string.Empty, result.Error.Message);
                return Page();
            }

            var campaign = result.Value;
            if (campaign.IsRecruitmentFormOpen == false)
            {
                ModelState.AddModelError(string.Empty, "Rekrutacja w tej kampanii jest zamknięta");
                return Page();
            }

            AvailableTrainings = campaign.Trainings.Select(x => BuildSelectListItem(x)).ToArray();

            return Page();
        }

        private SelectListItem BuildSelectListItem(Szlem.Recruitment.Trainings.TrainingSummary training)
        {
            return new SelectListItem(
                training.ToString(),
                training.ID.ToString()
            );
        }

        public async Task<IActionResult> OnPost()
        {
            var result = await _engine.Execute(Command);
            if (result.IsFailure)
            {
                CommonPageModelExtensions.AddModelErrors(ModelState, result.Error);
                return await OnGet(CampaignID);
            }
            else
            {
                return RedirectToPage(RecruitmentFormSubmittedModel.PageName, new { email = Command.Email.ToString() });
            }
        }

    }
}

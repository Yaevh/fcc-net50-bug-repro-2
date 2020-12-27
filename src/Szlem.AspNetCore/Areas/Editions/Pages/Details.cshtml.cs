using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.Engine;
using Szlem.SharedKernel;
using Szlem.Recruitment.Campaigns;
using static Szlem.Engine.Editions.Editions.DetailsUseCase;

namespace Szlem.AspNetCore.Areas.Editions.Pages
{
    public class DetailsModel : PageModel
    {
        public static readonly string Route = $"/{PageName}";
        public const string PageName = "Details";

        private readonly ISzlemEngine _engine;

        public DetailsModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public EditionDetails Edition { get; set; }

        [BindProperty]
        public Create.Command CreateRecruitmentCampaignCommand { get; set; }

        public async Task<IActionResult> OnGet(int id)
        {
            Edition = (await _engine.Query(new Query() { EditionID = id })).Value;
            return Page();
        }

        public async Task<IActionResult> OnPostCreateRecruitmentCampaign()
        {
            var result = await _engine.Execute(CreateRecruitmentCampaignCommand);
            if (result.IsFailure)
                CommonPageModelExtensions.AddModelErrors(ModelState, result.Error);
            return await OnGet(CreateRecruitmentCampaignCommand.EditionID);
        }
    }
}
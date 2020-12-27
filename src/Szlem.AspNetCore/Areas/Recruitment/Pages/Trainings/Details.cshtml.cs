using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Trainings;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages.Trainings
{
    [Authorize(Policy = AuthorizationPolicies.CoordinatorsOnly)]
    public class DetailsModel : PageModel
    {
        public const string PageName = "Details";
        public static readonly string Route = Consts.TrainingPageRoute(PageName);
        public static string RouteFor(int trainingId) => $"{Route}/{trainingId}";

        private readonly ISzlemEngine _engine;

        public DetailsModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public Details.TrainingDetails Training { get; private set; }

        [BindProperty]
        public AddNote.Command AddNoteCommand { get; set; }

        public async Task<IActionResult> OnGet(int trainingId)
        {
            var result = await _engine.Query(new Details.Query() { TrainingId = trainingId });
            if (result.HasNoValue)
                return NotFound();

            Training = result.Value;

            return Page();
        }

        public async Task<IActionResult> OnPostAddNote()
        {
            await _engine.Execute(AddNoteCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(AddNoteCommand)));

            return await OnGet(AddNoteCommand.TrainingId);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.SchoolManagement;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.SchoolManagement.Pages
{
    public class RegisterSchoolModel : PageModel
    {
        public const string PageName = "RegisterSchool";
        public static readonly string Route = $"/{PageName}";

        private readonly ISzlemEngine _engine;

        public RegisterSchoolModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        [BindProperty]
        public RegisterSchool.Command Command { get; set; }


        public void OnGet() { }

        public async Task<IActionResult> OnPost()
        {
            var result = await _engine.Execute(Command);
            return result.Match<Guid, IActionResult, Domain.Error>(
                guid => RedirectToPage(DetailsModel.Route, new { schoolId = guid }),
                error => Page()
            );
        }
    }
}

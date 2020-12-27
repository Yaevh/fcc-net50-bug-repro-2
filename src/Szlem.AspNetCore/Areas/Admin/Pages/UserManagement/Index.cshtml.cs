using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.Engine;
using Szlem.Engine.UserManagement;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Admin.UserManagement.Pages
{
    public class IndexModel : PageModel
    {
        public const string PageName = "Index";
        public static readonly string Route = $"/{Consts.SubAreaName}/{PageName}";

        private readonly ISzlemEngine _engine;

        public IndexModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public IReadOnlyList<IndexUseCase.UserSummary> Users { get; set; }


        public async Task<IActionResult> OnGet()
        {
            Users = await _engine.Query(new IndexUseCase.Query());
            return Page();
        }
    }
}
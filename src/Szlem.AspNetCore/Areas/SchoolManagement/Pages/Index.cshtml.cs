using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.SchoolManagement;
using Szlem.SharedKernel;
using X.PagedList;

namespace Szlem.AspNetCore.Areas.SchoolManagement.Pages
{
    public class IndexModel : PageModel
    {
        public const string PageName = "Index";
        public static readonly string Route = $"/{PageName}";

        private readonly ISzlemEngine _engine;

        public IndexModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        [FromQuery]
        public GetSchools.Query Query { get; set; }

        public IPagedList<GetSchools.Summary> Schools { get; set; }

        public async Task<IActionResult> OnGet()
        {
            Schools = await _engine.Query(Query);
            return Page();
        }
    }
}

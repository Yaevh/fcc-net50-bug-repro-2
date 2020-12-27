using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.Engine;
using Szlem.SharedKernel;
using static Szlem.Engine.Editions.Editions.IndexUseCase;

namespace Szlem.AspNetCore.Areas.Editions.Pages
{
    public class IndexModel : PageModel
    {
        public static readonly string Route = $"/{PageName}";
        public const string PageName = "Index";
        
        private readonly ISzlemEngine _engine;

        public IndexModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        public EditionSummary[] Editions { get; set; }


        public async Task<IActionResult> OnGet()
        {
            Editions = await _engine.Query(new Query());
            return Page();
        }
    }
}
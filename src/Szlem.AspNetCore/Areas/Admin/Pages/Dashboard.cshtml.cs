using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NodaTime;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;

namespace Szlem.AspNetCore.Areas.Admin.Pages
{
    public class DashboardModel : PageModel
    {
        public const string PageName = "Dashboard";
        public static readonly string Route = $"/{PageName}";

        private readonly MockableClock _clock;

        public DashboardModel(MockableClock clock)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public ZonedDateTime CurrentDateTime => _clock.GetZonedDateTime();
        public bool IsTimeMocked => _clock.IsMocked;

        [BindProperty]
        public LocalDate MockDate { get; set; }


        public IActionResult OnPostSetClock()
        {
            _clock.MockNow = MockDate.At(LocalTime.Noon).InMainTimezone().ToInstant();
            return RedirectToPage(Route);
        }

        public IActionResult OnPostClearClock()
        {
            _clock.RestoreRealTime();
            return RedirectToPage(Route);
        }
    }
}
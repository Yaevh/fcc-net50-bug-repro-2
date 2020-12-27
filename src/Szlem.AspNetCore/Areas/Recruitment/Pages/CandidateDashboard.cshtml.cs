using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Enrollments;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages
{
    [Authorize(AuthorizationPolicies.CandidateOnly)]
    public class CandidateDashboardModel : PageModel
    {
        public const string PageName = "CandidateDashboard";
        public static readonly string Route = $"/{PageName}";

        private readonly ISzlemEngine _engine;
        private readonly IUserAccessor _userAccessor;

        public CandidateDashboardModel(ISzlemEngine engine, IUserAccessor userAccessor)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        }


        public GetEnrollmentDetails.Details EnrollmentDetails { get; private set; }

        [BindProperty]
        public RecordResignation.Command ResignCommand { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userAccessor.GetUser();
            var result = await _engine.Query(new GetEnrollmentDetails.QueryByEmail() { Email = EmailAddress.Parse(user.Email) })
                .Tap(details => EnrollmentDetails = details)
                .OnFailure(error => ModelState.AddModelError(string.Empty, error.Message));
            return result.Match<GetEnrollmentDetails.Details, IActionResult, Error>(
                success => { EnrollmentDetails = success; return Page(); },
                failure => { ModelState.AddModelError(string.Empty, failure.Message); return RedirectToPage("/Index"); }
            );
        }

        public async Task<IActionResult> OnPostResignFromProject()
        {
            ResignCommand.CommunicationChannel = CommunicationChannel.IncomingApiRequest;
            var result = await _engine.Execute(ResignCommand);
            return result.Match<Nothing, IActionResult, Error>(
                success => { return RedirectToPage("/Index"); },
                failure => { ModelState.AddModelError(string.Empty, failure.Message); return RedirectToPage("/Index"); }
            );
        }
    }
}
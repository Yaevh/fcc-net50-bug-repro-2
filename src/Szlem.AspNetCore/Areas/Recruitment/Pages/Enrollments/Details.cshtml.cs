using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Szlem.AspNetCore.Infrastructure;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Trainings;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.Recruitment.Pages.Enrollments
{
    public class DetailsModel : PageModel
    {
        public const string PageName = "Details";
        public static readonly string Route = Consts.EnrollmentPageRoute(PageName);
        public static string RouteFor(Guid enrollmentId) => $"{Route}/{enrollmentId}";

        private readonly ISzlemEngine _engine;

        public DetailsModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        [FromQuery]
        public Guid EnrollmentId { get; set; }
        public GetEnrollmentDetails.Details EnrollmentDetails { get; set; }
        public IEnumerable<SelectListItem> PreferredTrainings => EnrollmentDetails.PreferredTrainings.Select(BuildPreferredTrainingOption);

        [BindProperty]
        public RecordAcceptedTrainingInvitation.Command RecordAcceptedTrainingInvitationCommand { get; set; }

        [BindProperty]
        public RecordRefusedTrainingInvitation.Command RecordRefusedTrainingInvitationCommand { get; set; }

        [BindProperty]
        public RecordResignation.Command RecordResignationCommand { get; set; }

        [BindProperty]
        public RecordTrainingResults.Command RecordTrainingResultsCommand { get; set; }

        [BindProperty]
        public RecordContact.Command RecordContactCommand { get; set; }



        public async Task<IActionResult> OnGet([FromQuery] Guid enrollmentId)
        {
            return await BuildPage(enrollmentId);
        }


        public async Task<IActionResult> OnPostCandidateAcceptedTrainingInvitation()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordAcceptedTrainingInvitationCommand));
            return await _engine.Execute(RecordAcceptedTrainingInvitationCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordAcceptedTrainingInvitationCommand)))
                .Finally(_ => RedirectToPage(Route, new { enrollmentId = RecordAcceptedTrainingInvitationCommand.EnrollmentId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostCandidateRefusedTrainingInvitation()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordRefusedTrainingInvitationCommand));
            return await _engine.Execute(RecordRefusedTrainingInvitationCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordRefusedTrainingInvitationCommand)))
                .Finally(_ => RedirectToPage(Route, new { enrollmentId = RecordRefusedTrainingInvitationCommand.EnrollmentId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostCandidateResigned()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordResignationCommand));
            return await _engine.Execute(RecordResignationCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordResignationCommand)))
                .Finally(_ => RedirectToPage(Route, new { enrollmentId = RecordResignationCommand.EnrollmentId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostRecordTrainingResults()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordTrainingResultsCommand));
            return await _engine.Execute(RecordTrainingResultsCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordTrainingResultsCommand)))
                .Finally(_ => RedirectToPage(Route, new { enrollmentId = RecordTrainingResultsCommand.EnrollmentId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostRecordContact()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordContactCommand));
            return await _engine.Execute(RecordContactCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordContactCommand)))
                .Finally(_ => RedirectToPage(Route, new { enrollmentId = RecordContactCommand.EnrollmentId }).WithModelStateOf(this));
        }

        private async Task<IActionResult> BuildPage(Guid enrollmentId)
        {
            EnrollmentId = enrollmentId;
            var request = new GetEnrollmentDetails.QueryByEnrollmentId() { EnrollmentID = enrollmentId };
            var result = await _engine.Query(request);
            if (result.IsFailure)
                return new PageResult() { StatusCode = (int)System.Net.HttpStatusCode.NotFound }; 
            
            EnrollmentDetails = result.Value;
            return Page();
        }

        private void PurgeAllErrorsExceptConcerningPostCommand(string commandName)
        {
            var errorsToPurge = ModelState.Where(x => x.Value.ValidationState == ModelValidationState.Invalid && x.Key.StartsWith(commandName) == false);
            foreach (var errorToPurge in errorsToPurge)
                ModelState.Remove(errorToPurge.Key);
        }

        private SelectListItem BuildPreferredTrainingOption(TrainingSummary training)
        {
            var text = training.ToString();
            var isSelected = training.ID == EnrollmentDetails.SelectedTraining?.ID;
            if (isSelected)
                text = $"[wybrane szkolenie] {text}";
            return new SelectListItem(
                text,
                training.ID.ToString(), isSelected
            );
        }
    }
}

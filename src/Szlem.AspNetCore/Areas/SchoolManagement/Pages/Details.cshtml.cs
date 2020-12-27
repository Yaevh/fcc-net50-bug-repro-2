using CSharpFunctionalExtensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Szlem.SchoolManagement;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Areas.SchoolManagement.Pages
{
    public class DetailsModel : PageModel
    {
        public const string PageName = "Details";
        public static readonly string Route = $"/{PageName}";
        public static string RouteFor(Guid schoolId) => $"{Route}/{schoolId}";

        public const string GetScannedAgreementHandlerName = "ScannedAgreement";

        private readonly ISzlemEngine _engine;

        public DetailsModel(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        [FromQuery]
        public Guid SchoolId { get; set; }
        public GetDetails.SchoolDetails SchoolDetails { get; set; }


        [BindProperty] public RecordContact.Command RecordContactCommand { get; set; }
        [BindProperty] public RecordInitialAgreement.Command RecordInitialAgreementCommand { get; set; }
        [BindProperty] public RecordAgreementSignedInputModel RecordAgreementSignedModel { get; set; }
        [BindProperty] public RecordResignation.Command RecordResignationCommand { get; set; }
        [BindProperty] public AddNote.Command AddNoteCommand { get; set; }
        [BindProperty] public EditNote.Command EditNoteCommand { get; set; }
        [BindProperty] public DeleteNote.Command DeleteNoteCommand { get; set; }



        public async Task<IActionResult> OnGet([FromQuery] Guid schoolId)
        {
            return await BuildPage(schoolId);
        }

        public async Task<IActionResult> OnPostRecordContact()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordContactCommand));
            return await _engine.Execute(RecordContactCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordContactCommand)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = RecordContactCommand.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostRecordInitialAgreement()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordInitialAgreementCommand));
            return await _engine.Execute(RecordInitialAgreementCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordInitialAgreementCommand)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = RecordInitialAgreementCommand.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostRecordAgreementSigned()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordAgreementSignedModel));
            if (RecordAgreementSignedModel.AgreementFile == null)
                return RedirectToPage(Route, new { schoolId = RecordAgreementSignedModel.Command.SchoolId }).WithModelStateOf(this);

            RecordAgreementSignedModel.Command.ScannedDocumentExtension = Path.GetExtension(RecordAgreementSignedModel.AgreementFile.FileName);
            RecordAgreementSignedModel.Command.ScannedDocumentContentType = RecordAgreementSignedModel.AgreementFile.ContentType;
            using (var fileStream = RecordAgreementSignedModel.AgreementFile.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);
                    RecordAgreementSignedModel.Command.ScannedDocument = memoryStream.ToArray();
                }
            }

            var errorsToPurge = ModelState.Where(x => x.Value.ValidationState == ModelValidationState.Invalid);
            foreach (var errorToPurge in errorsToPurge)
                ModelState.Remove(errorToPurge.Key);
            ModelState.Validate(new RecordAgreementSignedInputModel.Validator(), RecordAgreementSignedModel);
            if (ModelState.IsValid == false)
                return RedirectToPage(Route, new { schoolId = RecordAgreementSignedModel.Command.SchoolId }).WithModelStateOf(this);

            return await _engine.Execute(RecordAgreementSignedModel.Command)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordAgreementSignedModel)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = RecordAgreementSignedModel.Command.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostRecordResignation()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(RecordResignationCommand));
            return await _engine.Execute(RecordResignationCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(RecordResignationCommand)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = RecordResignationCommand.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostAddNote()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(AddNoteCommand));
            return await _engine.Execute(AddNoteCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(AddNoteCommand)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = AddNoteCommand.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostEditNote()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(EditNoteCommand));
            return await _engine.Execute(EditNoteCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(EditNoteCommand)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = EditNoteCommand.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnPostDeleteNote()
        {
            PurgeAllErrorsExceptConcerningPostCommand(nameof(DeleteNoteCommand));
            return await _engine.Execute(DeleteNoteCommand)
                .OnFailure(error => ModelState.AddModelErrors(error, nameof(DeleteNoteCommand)))
                .Finally(_ => RedirectToPage(Route, new { schoolId = DeleteNoteCommand.SchoolId }).WithModelStateOf(this));
        }

        public async Task<IActionResult> OnGetScannedAgreement(Guid schoolId, Guid agreementId)
        {
            var result = await _engine.Query(new GetScannedAgreement.Query() { SchoolId = schoolId, AgreementId = agreementId });
            if (result.HasNoValue)
                return NotFound();
            return File(result.Value.Content, result.Value.ContentType);
        }


        private void PurgeAllErrorsExceptConcerningPostCommand(string commandName)
        {
            var errorsToPurge = ModelState.Where(x => x.Value.ValidationState == ModelValidationState.Invalid && x.Key.StartsWith(commandName) == false);
            foreach (var errorToPurge in errorsToPurge)
                ModelState.Remove(errorToPurge.Key);
        }

        private async Task<IActionResult> BuildPage(Guid schoolId)
        {
            SchoolId = schoolId;
            var request = new GetDetails.Query() { SchoolId = schoolId };
            var result = await _engine.Query(request);
            if (result.IsFailure)
                return new PageResult() { StatusCode = (int)HttpStatusCode.NotFound };
            
            SchoolDetails = result.Value;
            return Page();
        }


        public class RecordAgreementSignedInputModel
        {
            public RecordAgreementSigned.Command Command { get; set; }
            [Display(Name = "Skan umowy")] public IFormFile AgreementFile { get; set; }

            public class Validator : AbstractValidator<RecordAgreementSignedInputModel>
            {
                public Validator()
                {
                    RuleFor(x => x.Command).NotEmpty();
                    RuleFor(x => x.Command).SetValidator(x => new RecordAgreementSigned.Validator());
                    RuleFor(x => x.AgreementFile).NotEmpty();
                }
            }
        }
    }
}

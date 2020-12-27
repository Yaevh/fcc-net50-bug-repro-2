using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.Recruitment.Enrollments;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Controllers.v1
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class EnrollmentController : ControllerBase
    {
        private readonly ISzlemEngine _engine;

        public EnrollmentController(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        [AllowAnonymous]
        [HttpPost(Routes.v1.Enrollments.SubmitRecruitmentForm)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Create(SubmitRecruitmentForm.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [HttpGet(Routes.v1.Enrollments.GetSubmissions)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<GetSubmissions.SubmissionSummary>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSubmissions([FromQuery] GetSubmissions.Query query)
        {
            return Ok(await _engine.Query(query));
        }

        [HttpGet(Routes.v1.Enrollments.GetEnrollment)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetEnrollmentDetails.Details))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetEnrollment([FromRoute] Guid enrollmentID)
        {
            var result = await _engine.Query(new GetEnrollmentDetails.QueryByEnrollmentId() { EnrollmentID = enrollmentID });
            return result.MatchToActionResult(v => Ok(v));
        }

        [HttpPost(Routes.v1.Enrollments.RecordAcceptedTrainingInvitation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordAcceptedTrainingInvitation(RecordAcceptedTrainingInvitation.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [HttpPost(Routes.v1.Enrollments.RecordRefusedTrainingInvitation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordRefusedTrainingInvitation(RecordRefusedTrainingInvitation.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [HttpPost(Routes.v1.Enrollments.RecordTrainingResults)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordTrainingResults(RecordTrainingResults.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [HttpPost(Routes.v1.Enrollments.RecordResignation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordResignation(RecordResignation.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [HttpPost(Routes.v1.Enrollments.RecordContact)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordContact(RecordContact.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }
    }
}

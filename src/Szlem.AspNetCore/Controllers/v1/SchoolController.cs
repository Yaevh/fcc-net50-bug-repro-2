using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.SchoolManagement;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Controllers.v1
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class SchoolController : ControllerBase
    {
        private readonly ISzlemEngine _engine;
        public SchoolController(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }


        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet(Routes.v1.Schools.Index)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Index([FromQuery] GetSchools.Query query)
        {
            return Ok(await _engine.Query(query));
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet(Routes.v1.Schools.Details)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetDetails.SchoolDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> GetDetails([FromRoute] Guid schoolId)
        {
            var result = await _engine.Query(new GetDetails.Query() { SchoolId = schoolId });
            return result.MatchToActionResult(v => Ok(v));
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.Register)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Guid))]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Register(RegisterSchool.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok(success));
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.RecordInitialAgreement)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordInitialAgreement(RecordInitialAgreement.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.RecordAgreementSigned)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordAgreementSigned(RecordAgreementSigned.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok(success));
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.RecordResignation)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordResignation(RecordResignation.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.RecordContact)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> RecordContact(RecordContact.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.AddNote)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> AddNote(AddNote.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok(success));
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.EditNote)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> EditNote(EditNote.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }

        [Authorize(AuthorizationPolicies.CoordinatorsOnly, AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost(Routes.v1.Schools.DeleteNote)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> DeleteNote(DeleteNote.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(success => Ok());
        }
    }
}

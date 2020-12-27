using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Szlem.AspNetCore.Contracts.Identity;
using Szlem.AspNetCore.Infrastructure;

namespace Szlem.AspNetCore.Controllers.v1
{
    [ApiController]
    [AllowAnonymous]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class IdentityController : ControllerBase
    {
        private readonly IIdentityService _identityService;

        public IdentityController(IIdentityService identityService)
        {
            _identityService = identityService ?? throw new ArgumentNullException(nameof(identityService));
        }

        [HttpPost(Routes.v1.Identity.Register)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Register(Register.Request request)
        {
            var result = await _identityService.Register(request);
            return result.MatchToActionResult(v => Ok(v));
        }

        [HttpPost(Routes.v1.Identity.Login)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Login(Login.Request request)
        {
            var result = await _identityService.Login(request);
            return result.MatchToActionResult(v => Ok(v));
        }
    }
}
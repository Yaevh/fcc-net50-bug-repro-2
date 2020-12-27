using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Szlem.Engine;
using Szlem.Engine.Editions.Editions;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Controllers.v1
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class EditionController : ControllerBase
    {
        private readonly ISzlemEngine _engine;

        public EditionController(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        [HttpGet(Routes.v1.Editions.Index)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IndexUseCase.EditionSummary[]))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Index()
        {
            return Ok(await _engine.Query(new IndexUseCase.Query()));
        }

        [HttpGet(Routes.v1.Editions.Details)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DetailsUseCase.EditionDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _engine.Query(new DetailsUseCase.Query() { EditionID = id });
            return result.MatchToActionResult(v => Ok(v));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [HttpPost(Routes.v1.Editions.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create(CreateUseCase.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(s => Created(Routes.v1.Editions.DetailsFor(s.Id), null));
        }
    }
}
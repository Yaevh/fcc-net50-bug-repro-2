using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using CSharpFunctionalExtensions;
using Szlem.Engine;
using Szlem.SharedKernel;
using Szlem.Recruitment.Campaigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Szlem.Recruitment.Trainings;

namespace Szlem.AspNetCore.Controllers.v1
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class RecruitmentCampaignController : ControllerBase
    {
        public static readonly string ControllerName = nameof(RecruitmentCampaignController).TrimEnd(nameof(Controller));

        private readonly ISzlemEngine _engine;

        public RecruitmentCampaignController(ISzlemEngine engine)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        [HttpPost(Routes.v1.RecruitmentCampaigns.Create)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create(Create.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(v => Created(Routes.v1.RecruitmentCampaigns.DetailsFor(v.ID), null));
        }

        [HttpGet(Routes.v1.RecruitmentCampaigns.Details)]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Recruitment.Campaigns.Details.Campaign))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _engine.Query(new Recruitment.Campaigns.Details.Query() { CampaignID = id });
            return result.MatchToActionResult(v => Ok(v));
        }

        [HttpPost(Routes.v1.RecruitmentCampaigns.ScheduleTraining)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ScheduleTraining(ScheduleTraining.Command command)
        {
            var result = await _engine.Execute(command);
            return result.MatchToActionResult(v => Created(Routes.v1.RecruitmentCampaigns.DetailsFor(v.ID), null));
        }
    }
}
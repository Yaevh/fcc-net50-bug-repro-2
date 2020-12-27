using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Exceptions;
using Szlem.Engine.Infrastructure;
using Szlem.Engine.Interfaces;
using Szlem.Models.Editions;

using static Szlem.Engine.Editions.Editions.DetailsUseCase;


namespace Szlem.Persistence.EF.Editions.Editions
{
    internal class DetailsUseCaseHandler : IRequestHandler<Query, Result<EditionDetails, Error>>
    {
        private readonly AppDbContext _dbContext;
        private readonly IRequestAuthorizationAnalyzer _authorizer;
        private readonly IMediator _mediator;

        public DetailsUseCaseHandler(AppDbContext dbContext, IRequestAuthorizationAnalyzer authorizer, IMediator mediator)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }
        
        public async Task<Result<EditionDetails, Error>> Handle(Query request, CancellationToken cancellationToken)
        {
            if (request.EditionID == default)
                return Result.Failure<EditionDetails, Error>(new Error.ResourceNotFound());

            var edition = await _dbContext.Editions
                .AsNoTracking()
                .Where(x => x.ID == request.EditionID)
                .SingleOrDefaultAsync();
            if (edition == null)
                return Result.Failure<EditionDetails, Error>(new Error.ResourceNotFound());

            var campaigns = await _mediator.Send(new Szlem.Recruitment.Campaigns.Index.Query() { EditionID = edition.ID });

            var isCurrent = (edition.StartDate <= DateTime.Today && edition.EndDate >= DateTime.Today);
            var canAddRecruitmentCampaign = edition.EndDate >= DateTime.Today && (await _authorizer.Authorize(new Szlem.Recruitment.Campaigns.Create.Command() { EditionID = edition.ID })).Succeeded;

            return Result.Success<EditionDetails, Error>(new EditionDetails()
            {
                ID = edition.ID,
                Name = edition.Name,
                StartDate = NodaTime.LocalDate.FromDateTime(edition.StartDate),
                EndDate = NodaTime.LocalDate.FromDateTime(edition.EndDate),
                CumulativeStatistics = edition.CumulativeStatistics,
                ThisEditionStatistics = edition.ThisEditionStatistics,
                RecruitmentCampaigns = campaigns.IsSuccess ? campaigns.Value.Select(x => new EditionDetails.RecruitmentCampaignData()
                {
                    ID = x.ID,
                    Name = x.Name,
                    StartDateTime = x.StartDateTime,
                    EndDateTime = x.EndDateTime
                }).ToArray() : new EditionDetails.RecruitmentCampaignData[0],
                CanAddRecruitmentCampaign = canAddRecruitmentCampaign,
                IsCurrent = isCurrent
            });
        }
    }
}

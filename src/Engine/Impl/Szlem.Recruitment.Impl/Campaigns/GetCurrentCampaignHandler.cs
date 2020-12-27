using CSharpFunctionalExtensions;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.Impl.Repositories;

namespace Szlem.Recruitment.Impl.Campaigns
{
    internal class GetCurrentCampaignHandler : IRequestHandler<GetCurrentCampaign.Query, Maybe<Details.Campaign>>
    {
        private readonly IClock _clock;
        private readonly ICampaignRepository _campaignRepo;
        private readonly IRequestHandler<Details.Query, Result<Details.Campaign, Domain.Error>> _detailsHandler;

        public GetCurrentCampaignHandler(IClock clock, ICampaignRepository campaignRepo, IRequestHandler<Details.Query, Result<Details.Campaign, Domain.Error>> detailsHandler)
        {
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _campaignRepo = campaignRepo ?? throw new ArgumentNullException(nameof(campaignRepo));
            _detailsHandler = detailsHandler ?? throw new ArgumentNullException(nameof(detailsHandler));
        }

        public async Task<Maybe<Details.Campaign>> Handle(GetCurrentCampaign.Query request, CancellationToken cancellationToken)
        {
            var now = _clock.GetCurrentInstant();
            var campaigns = await _campaignRepo.GetAll();
            var campaign = campaigns.SingleOrDefault(x => x.StartDateTime.ToInstant() < now && x.EndDateTime.ToInstant() > now);
            if (campaign == null)
                return Maybe<Details.Campaign>.None;

            var campaignDetails = await _detailsHandler.Handle(new Details.Query() { CampaignID = campaign.Id }, cancellationToken);
            return Maybe<Details.Campaign>.From(campaignDetails.Value);
        }
    }
}

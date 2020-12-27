using CSharpFunctionalExtensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.Impl.Repositories;
using static Szlem.Recruitment.Campaigns.Index;

namespace Szlem.Recruitment.Impl.Campaigns
{
    class IndexHandler : IRequestHandler<Query, Result<CampaignSummary[], Error>>
    {
        private readonly ICampaignRepository _repository;

        public IndexHandler(ICampaignRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }


        public async Task<Result<CampaignSummary[], Error>> Handle(Query request, CancellationToken cancellationToken)
        {
            var campaigns = request.EditionID.HasValue ? await _repository.GetByEditionId(request.EditionID.Value) : await _repository.GetAll();

            return Result.Success<CampaignSummary[], Error>(campaigns
                .OrderBy(x => x.StartDateTime, NodaTime.OffsetDateTime.Comparer.Instant)
                .Select(x => new CampaignSummary()
                {
                    ID = x.Id,
                    Name = x.Name,
                    StartDateTime = x.StartDateTime,
                    EndDateTime = x.EndDateTime
                })
                .ToArray());
        }
    }
}

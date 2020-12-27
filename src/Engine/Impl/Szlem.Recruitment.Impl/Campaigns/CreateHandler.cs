using CSharpFunctionalExtensions;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Repositories;

namespace Szlem.Recruitment.Impl.Campaigns
{
    class CreateHandler : IRequestHandler<Create.Command, Result<Create.Response, Error>>
    {
        private readonly IEditionProvider _editionProvider;
        private readonly ICampaignRepository _repository;

        public CreateHandler(IEditionProvider editionProvider, ICampaignRepository repository)
        {
            _editionProvider = editionProvider ?? throw new ArgumentNullException(nameof(editionProvider));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<Result<Create.Response, Error>> Handle(Create.Command request, CancellationToken cancellationToken)
        {
            var maybeEdition = await _editionProvider.GetEdition(request.EditionID);
            if (maybeEdition.HasNoValue)
                return Result.Failure<Create.Response, Error>(new Error.ResourceNotFound($"Edition with ID={request.EditionID} not found"));
            if (maybeEdition.Value.StartDateTime > request.StartDateTime.ToInstant())
                return Result.Failure<Create.Response, Error>(new Error.ValidationFailed(nameof(request.StartDateTime), Create.ErrorMessages.CampaignCannotStartBeforeEditionStart));
            if (maybeEdition.Value.EndDateTime < request.EndDateTime.ToInstant())
                return Result.Failure<Create.Response, Error>(new Error.ValidationFailed(nameof(request.EndDateTime), Create.ErrorMessages.CampaignMustEndBeforeEditionEnd));

            var existingCampaigns = await _repository.GetAll();
            var interval = new Interval(request.StartDateTime.ToInstant(), request.EndDateTime.ToInstant());
            if (existingCampaigns.Any(x => new Interval(x.StartDateTime.ToInstant(), x.EndDateTime.ToInstant()).Overlaps(interval)))
                return Result.Failure<Create.Response, Error>(new Error.DomainError(Create.ErrorMessages.CampaignsCannotOverlap));

            var campaign = new Entities.Campaign(request.StartDateTime, request.EndDateTime, request.EditionID, request.Name);

            var id = await _repository.Insert(campaign);
            return Result.Success<Create.Response, Error>(new Create.Response() { ID = id });
        }
    }
}

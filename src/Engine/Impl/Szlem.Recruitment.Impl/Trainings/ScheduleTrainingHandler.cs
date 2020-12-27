using CSharpFunctionalExtensions;
using MediatR;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Trainings;
using static Szlem.Recruitment.Trainings.ScheduleTraining;

namespace Szlem.Recruitment.Impl.Trainings
{
    internal class ScheduleTrainingHandler : IRequestHandler<Command, Result<Response, Error>>
    {
        private readonly ITrainingRepository _trainingRepo;
        private readonly ICampaignRepository _campaignRepo;
        private readonly IEditionProvider _editionProvider;
        private readonly IClock _clock;
        private readonly IUserAccessor _userAccessor;
        
        public ScheduleTrainingHandler(ITrainingRepository trainingRepo, ICampaignRepository campaignRepo, IClock clock, IEditionProvider editionProvider, IUserAccessor userAccessor)
        {
            _trainingRepo = trainingRepo ?? throw new ArgumentNullException(nameof(trainingRepo));
            _campaignRepo = campaignRepo ?? throw new ArgumentNullException(nameof(campaignRepo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _editionProvider = editionProvider ?? throw new ArgumentNullException(nameof(editionProvider));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        }

        public async Task<Result<Response, Error>> Handle(Command request, CancellationToken cancellationToken)
        {
            var now = _clock.GetCurrentInstant();
            if (request.StartDateTime < now.InMainTimezone().LocalDateTime)
                return Result.Failure<Response, Error>(new Error.DomainError(ErrorMessages.CannotScheduleTrainingInThePast));

            var campaign = await _campaignRepo.GetById(request.CampaignID);
            if (campaign == null)
                return Result.Failure<Response, Error>(new Error.ResourceNotFound($"Campaign with ID={request.CampaignID} not found"));
            if (campaign.StartDateTime.ToInstant() < now)
                return Result.Failure<Response, Error>(new Error.DomainError(ErrorMessages.CannotScheduleTrainingAfterCampaignStart));

            var maybeEdition = await _editionProvider.GetEdition(campaign.EditionId);
            if (maybeEdition.HasNoValue)
                throw new ApplicationException($"DB contains invalid data, edition with ID={campaign.EditionId} not found!");
            if (request.EndDateTime.InMainTimezone().ToInstant() > maybeEdition.Value.EndDateTime)
                return Result.Failure<Response, Error>(new Error.DomainError(ErrorMessages.CannotScheduleTrainingAfterEditionEnd));

            var user = await _userAccessor.GetUser();

            var scheduledTraining = new Training(
                request.Address,
                request.City,
                request.StartDateTime.InMainTimezone().ToOffsetDateTime(),
                request.EndDateTime.InMainTimezone().ToOffsetDateTime(),
                user.Id);

            var result = campaign.ScheduleTraining(scheduledTraining);
            if (result.IsFailure)
                return Result.Failure<Response, Error>(new Error.DomainError(result.Error));

            if (request.Notes != null)
            {
                var addNoteResult = scheduledTraining.AddNote(user.Id, request.Notes, _clock.GetCurrentInstant());
                if (addNoteResult.IsFailure)
                    return Result.Failure<Response, Error>(new Error.DomainError(addNoteResult.Error));
            }

            var id = await _trainingRepo.Insert(scheduledTraining);

            return Result.Success<Response, Error>(new Response() { ID = id });
        }
    }
}

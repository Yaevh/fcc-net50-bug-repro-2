using CSharpFunctionalExtensions;
using MediatR;
using NHibernate;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Trainings;
using static Szlem.Recruitment.Campaigns.Details;

namespace Szlem.Recruitment.Impl.Campaigns
{
    class DetailsHandler : IRequestHandler<Query, Result<Campaign, Error>>
    {
        private readonly ICampaignRepository _repository;
        private readonly IClock _clock;
        private readonly IRequestAuthorizationAnalyzer _authorizationAnalyzer;
        private readonly ITrainerProvider _trainerProvider;

        public DetailsHandler(ICampaignRepository repository, IClock clock, IRequestAuthorizationAnalyzer authorizationAnalyzer, ITrainerProvider trainerProvider)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _authorizationAnalyzer = authorizationAnalyzer ?? throw new ArgumentNullException(nameof(authorizationAnalyzer));
            _trainerProvider = trainerProvider ?? throw new ArgumentNullException(nameof(trainerProvider));
        }

        public async Task<Result<Campaign, Error>> Handle(Query request, CancellationToken cancellationToken)
        {
            var campaign = await _repository.GetById(request.CampaignID);
            if (campaign == null)
                return Result.Failure<Campaign, Error>(new Error.ResourceNotFound($"Nie znaleziono kampanii o ID={request.CampaignID}"));

            var now = _clock.GetCurrentInstant();

            var trainerIDs = campaign.Trainings.Select(x => x.CoordinatorID).Distinct().ToArray();
            var trainers = await _trainerProvider.GetTrainerDetails(trainerIDs);

            var isrecruitmentFormOpen = new Interval(campaign.StartDateTime.ToInstant(), campaign.EndDateTime.ToInstant()).Contains(now);
            var canScheduleTraining = (now < campaign.StartDateTime.ToInstant())
                && (await _authorizationAnalyzer.Authorize(new ScheduleTraining.Command())).Succeeded;

            return Result.Success<Campaign, Error>(new Campaign()
            {
                ID = campaign.Id,
                Name = campaign.Name,
                StartDateTime = campaign.StartDateTime,
                EndDateTime = campaign.EndDateTime,
                Trainings = campaign.Trainings
                    .OrderBy(x => x.StartDateTime, OffsetDateTime.Comparer.Instant)
                    .Select(x => new TrainingSummary()
                    {
                        ID = x.ID,
                        Address = x.Address,
                        City = x.City,
                        CoordinatorID = x.CoordinatorID,
                        CoordinatorName = trainers.Single(y => y.Guid == x.CoordinatorID).Name,
                        StartDateTime = x.StartDateTime,
                        EndDateTime = x.EndDateTime,
                        Timing = x.CalculateTiming(now)
                    })
                    .ToArray(),
                IsRecruitmentFormOpen = isrecruitmentFormOpen,
                CanScheduleTraining = canScheduleTraining
            });
        }
    }
}

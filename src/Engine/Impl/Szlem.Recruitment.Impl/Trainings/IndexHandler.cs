using MediatR;
using NHibernate;
using NHibernate.Linq;
using NodaTime;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Persistence.NHibernate.CustomTypes;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Trainings;
using X.PagedList;

namespace Szlem.Recruitment.Impl.Trainings
{
    internal class IndexHandler : IRequestHandler<Szlem.Recruitment.Trainings.Index.Query, IReadOnlyCollection<TrainingSummary>>
    {
        private readonly ITrainingRepository _repository;
        private readonly ITrainerProvider _trainerProvider;
        private readonly IClock _clock;

        public IndexHandler(ITrainingRepository repository, ITrainerProvider trainerProvider, IClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _trainerProvider = trainerProvider ?? throw new ArgumentNullException(nameof(trainerProvider));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<IReadOnlyCollection<TrainingSummary>> Handle(Recruitment.Trainings.Index.Query request, CancellationToken cancellationToken)
        {
            var query = _repository.Query();

            if (request.CampaignIds.Any())
                query = query.Where(x => request.CampaignIds.Contains(x.Campaign.Id));
            if (!string.IsNullOrWhiteSpace(request.City))
                query = query.Where(x => request.City.Contains(x.City));
            if (request.CoordinatorId.HasValue)
                query = query.Where(x => x.CoordinatorID == request.CoordinatorId);

            var now = _clock.GetCurrentInstant();
            var trainerIds = await query.Select(x => x.CoordinatorID).ToListAsync();
            var trainers = (await _trainerProvider.GetTrainerDetails(trainerIds)).ToDictionary(x => x.Guid);
            
            var results = await query
                .Select(x => new TrainingSummary()
            {
                ID = x.ID,
                Address = x.Address,
                City = x.City,
                StartDateTime = x.StartDateTime,
                EndDateTime = x.EndDateTime,
                CoordinatorID = x.CoordinatorID,
                CoordinatorName = trainers.ContainsKey(x.CoordinatorID) ? trainers[x.CoordinatorID].Name : string.Empty,
                Timing = x.CalculateTiming(now)
            }).ToListAsync() as IEnumerable<TrainingSummary>;

            if (request.From.HasValue)
                results = results.Where(x => x.StartDateTime.ToInstant() >= request.From.Value);
            if (request.To.HasValue)
                results = results.Where(x => x.EndDateTime.ToInstant() <= request.To.Value);

            return results
                .OrderBy(x => x.StartDateTime.ToInstant())
                .ToArray();
        }
    }
}

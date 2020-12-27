using EventFlow.Aggregates;
using EventFlow.ReadStores;
using MediatR;
using NodaTime;
using NodaTime.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Repositories;
using X.PagedList;

namespace Szlem.Recruitment.Impl.Enrollments
{
    internal class GetSubmissionsQueryHandler :
        IRequestHandler<GetSubmissions.Query, IPagedList<GetSubmissions.SubmissionSummary>>
    {
        private readonly IEnrollmentRepository _repo;
        private readonly ICampaignRepository _campaignRepo;
        private readonly IClock _clock;

        public GetSubmissionsQueryHandler(IEnrollmentRepository repo, ICampaignRepository campaignRepo, IClock clock)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _campaignRepo = campaignRepo ?? throw new ArgumentNullException(nameof(campaignRepo));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<IPagedList<GetSubmissions.SubmissionSummary>> Handle(GetSubmissions.Query query, CancellationToken cancellationToken)
        {
            var now = _clock.GetCurrentInstant();
            var today = now.InMainTimezone().Date;

            var latestCampaign = (await _campaignRepo.GetAll())
                .OrderByDescending(x => x.StartDateTime, OffsetDateTime.Comparer.Instant)
                .FirstOrDefault();
            var latestCampaignId = latestCampaign?.Id;

            var results = _repo.Query();
            if (query.SearchPattern != null)
                results = results.Where(x =>
                    x.FullName.ContainsCaseInsensitive(query.SearchPattern)
                    || x.Email.ToString().ContainsCaseInsensitive(query.SearchPattern)
                    || x.PhoneNumber.ToString().ContainsCaseInsensitive(query.SearchPattern)
                    || x.AboutMe.ContainsCaseInsensitive(query.SearchPattern)
                    || x.Region.ContainsCaseInsensitive(query.SearchPattern)
                    || x.PreferredLecturingCities.Any(x => x.ContainsCaseInsensitive(query.SearchPattern))
                    || x.PreferredTrainings.Any(y => y.City.ContainsCaseInsensitive(query.SearchPattern))
                    || x.Id.ToString().ContainsCaseInsensitive(query.SearchPattern)
                );

            if (query.CampaignIds != null && query.CampaignIds.Any())
                results = results.Where(x => query.CampaignIds.Contains(x.Campaign.Id));
            if (query.PreferredTrainingIds != null && query.PreferredTrainingIds.Any())
                results = results.Where(x => x.PreferredTrainings.Select(y => y.ID).Intersect(query.PreferredTrainingIds).Any());
            if (query.HasLecturerRights != null)
                results = results.Where(x => x.HasLecturerRights == query.HasLecturerRights);
            if (query.HasResigned == true)
                results = results.Where(x => x.HasResignedPermanently || x.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()));
            if (query.HasResigned == false)
                results = results.Where(x => x.HasResignedPermanently == false && x.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()) == false);
            if (query.EnrollmentAge == GetSubmissions.EnrollmentAge.LatestCampaign)
                results = results.Where(x => x.Campaign.Id == latestCampaignId);
            else if (query.EnrollmentAge == GetSubmissions.EnrollmentAge.OldCampaign)
                results = results.Where(x => x.Campaign.Id != latestCampaignId);

            if (query.SortBy.HasValue)
                results = results.OrderBy(query.SortBy.Value.ToString());
            else
                results = results.OrderBy(x => x.Timestamp);

            var pagedList = await results
                .Select(x => new GetSubmissions.SubmissionSummary()
                {
                    Id = x.Id.GetGuid(),
                    Timestamp = x.Timestamp.InMainTimezone().ToOffsetDateTime(),
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    FullName = x.FullName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    AboutMe = x.AboutMe,
                    Campaign = new GetSubmissions.CampaignSummary() {
                        ID = x.Campaign.Id,
                        Name = x.Campaign.Name,
                        StartDate = x.Campaign.StartDateTime.Date,
                        EndDate = x.Campaign.EndDateTime.Date
                    },
                    Region = x.Region,
                    PreferredLecturingCities = x.PreferredLecturingCities,
                    PreferredTrainings = x.PreferredTrainings
                        .OrderBy(y => y.StartDateTime, OffsetDateTime.Comparer.Instant)
                        .Select(y => new GetSubmissions.PreferredTrainingSummary() {
                            ID = y.ID,
                            Address = y.Address,
                            City = y.City,
                            StartDateTime = y.StartDateTime,
                            EndDateTime = y.EndDateTime,
                            CoordinatorID = y.CoordinatorID
                        }).ToArray(),
                    HasResignedPermanently = x.HasResignedPermanently,
                    HasResignedTemporarily = x.HasResignedTemporarilyAsOf(_clock.GetCurrentInstant()),
                    ResumeDate = x.ResumeDate.HasValue && x.ResumeDate.Value > today ? x.ResumeDate : null,
                    HasLecturerRights = x.HasLecturerRights,
                    IsCurrentSubmission = x.Campaign.Id == latestCampaignId,
                    IsOldSubmission = x.Campaign.Id != latestCampaignId
                })
                .ToPagedListAsync(query.PageNo, query.PageSize);

            return pagedList;
        }
    }
}

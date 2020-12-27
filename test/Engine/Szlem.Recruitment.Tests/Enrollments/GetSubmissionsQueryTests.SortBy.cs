using FluentAssertions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Repositories;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class GetSubmissionsQueryTests_SortBy
    {
        private readonly IClock clock = SystemClock.Instance;

        private EnrollmentReadModel CreateEmptyReadModel()
        {
            return new EnrollmentReadModel() {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>()
            };
        }

        [Fact(DisplayName = "Wyniki można sortować po imieniu")]
        public async Task CanOrderResultsBy_FirstName()
        {
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetAll() == Task.FromResult<IReadOnlyCollection<Impl.Entities.Campaign>>(Array.Empty<Impl.Entities.Campaign>()), MockBehavior.Strict);

            var enrollment1 = CreateEmptyReadModel();
            enrollment1.FirstName = "b";
            var enrollment2 = CreateEmptyReadModel();
            enrollment2.FirstName = "a";
            var enrollment3 = CreateEmptyReadModel();
            enrollment3.FirstName = "d";
            var enrollment4 = CreateEmptyReadModel();
            enrollment4.FirstName = "c";

            var repo = new EnrollmentRepoStub(enrollment1, enrollment2, enrollment3, enrollment4);
            var handler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            var results = await handler.Handle(new GetSubmissions.Query() { SortBy = GetSubmissions.SortBy.FirstName }, CancellationToken.None);

            results.IsOrderedBy(x => x.FirstName).Should().BeTrue();
        }

        [Fact(DisplayName = "Wyniki można sortować po nazwisku")]
        public async Task CanOrderResultsBy_LastName()
        {
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetAll() == Task.FromResult<IReadOnlyCollection<Impl.Entities.Campaign>>(Array.Empty<Impl.Entities.Campaign>()), MockBehavior.Strict);

            var enrollment1 = CreateEmptyReadModel();
            enrollment1.LastName = "b";
            var enrollment2 = CreateEmptyReadModel();
            enrollment2.LastName = "a";
            var enrollment3 = CreateEmptyReadModel();
            enrollment3.LastName = "d";
            var enrollment4 = CreateEmptyReadModel();
            enrollment4.LastName = "c";

            var repo = new EnrollmentRepoStub(enrollment1, enrollment2, enrollment3, enrollment4);
            var handler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            var results = await handler.Handle(new GetSubmissions.Query() { SortBy = GetSubmissions.SortBy.LastName }, CancellationToken.None);

            results.IsOrderedBy(x => x.LastName).Should().BeTrue();
        }

        [Fact(DisplayName = "Wyniki można sortować po dacie zgłoszenia")]
        public async Task CanOrderResultsBy_Timestamp()
        {
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetAll() == Task.FromResult<IReadOnlyCollection<Impl.Entities.Campaign>>(Array.Empty<Impl.Entities.Campaign>()), MockBehavior.Strict);

            var enrollment1 = CreateEmptyReadModel();
            enrollment1.Timestamp = clock.GetCurrentInstant().Minus(Duration.FromDays(2));
            var enrollment2 = CreateEmptyReadModel();
            enrollment2.Timestamp = clock.GetCurrentInstant().Minus(Duration.FromDays(1));
            var enrollment3 = CreateEmptyReadModel();
            enrollment3.Timestamp = clock.GetCurrentInstant().Minus(Duration.FromDays(4));
            var enrollment4 = CreateEmptyReadModel();
            enrollment4.Timestamp = clock.GetCurrentInstant().Minus(Duration.FromDays(3));

            var repo = new EnrollmentRepoStub(enrollment1, enrollment2, enrollment3, enrollment4);
            var handler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            var results = await handler.Handle(new GetSubmissions.Query() { SortBy = GetSubmissions.SortBy.Timestamp }, CancellationToken.None);

            results.IsOrderedBy(x => x.Timestamp, OffsetDateTime.Comparer.Local).Should().BeTrue();
        }

        [Fact(DisplayName = "Wyniki można sortować po adresie e-mail")]
        public async Task CanOrderResultsBy_Email()
        {
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetAll() == Task.FromResult<IReadOnlyCollection<Impl.Entities.Campaign>>(Array.Empty<Impl.Entities.Campaign>()), MockBehavior.Strict);

            var enrollment1 = CreateEmptyReadModel();
            enrollment1.Email = EmailAddress.Parse("2@mises.pl");
            var enrollment2 = CreateEmptyReadModel();
            enrollment2.Email = EmailAddress.Parse("3@mises.pl");
            var enrollment3 = CreateEmptyReadModel();
            enrollment3.Email = EmailAddress.Parse("1@mises.pl");
            var enrollment4 = CreateEmptyReadModel();
            enrollment4.Email = EmailAddress.Parse("4@mises.pl");

            var repo = new EnrollmentRepoStub(enrollment1, enrollment2, enrollment3, enrollment4);
            var handler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            var results = await handler.Handle(new GetSubmissions.Query() { SortBy = GetSubmissions.SortBy.Email }, CancellationToken.None);

            results.IsOrderedBy(x => x.Email).Should().BeTrue();
        }

        [Fact(DisplayName = "Wyniki można sortować po regionie")]
        public async Task CanOrderResultsBy_Region()
        {
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetAll() == Task.FromResult<IReadOnlyCollection<Impl.Entities.Campaign>>(Array.Empty<Impl.Entities.Campaign>()), MockBehavior.Strict);

            var enrollment1 = CreateEmptyReadModel();
            enrollment1.Region = "A";
            var enrollment2 = CreateEmptyReadModel();
            enrollment2.Region = "C";
            var enrollment3 = CreateEmptyReadModel();
            enrollment3.Region = "B";
            var enrollment4 = CreateEmptyReadModel();
            enrollment4.Region = "A";

            var repo = new EnrollmentRepoStub(enrollment1, enrollment2, enrollment3, enrollment4);
            var handler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            var results = await handler.Handle(new GetSubmissions.Query() { SortBy = GetSubmissions.SortBy.Region }, CancellationToken.None);

            results.IsOrderedBy(x => x.Region).Should().BeTrue();
        }

        [Fact(DisplayName = "Wyniki można sortować po dacie wznowienia")]
        public async Task CanOrderResultsBy_ResumeDate()
        {
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetAll() == Task.FromResult<IReadOnlyCollection<Impl.Entities.Campaign>>(Array.Empty<Impl.Entities.Campaign>()), MockBehavior.Strict);

            var enrollment1 = CreateEmptyReadModel();
            enrollment1.ResumeDate = null;
            var enrollment2 = CreateEmptyReadModel();
            enrollment2.ResumeDate = clock.GetTodayDate().PlusDays(3);
            var enrollment3 = CreateEmptyReadModel();
            enrollment3.ResumeDate = clock.GetTodayDate().PlusDays(2);
            var enrollment4 = CreateEmptyReadModel();
            enrollment4.ResumeDate = clock.GetTodayDate().PlusDays(7);

            var repo = new EnrollmentRepoStub(enrollment1, enrollment2, enrollment3, enrollment4);
            var handler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            var results = await handler.Handle(new GetSubmissions.Query() { SortBy = GetSubmissions.SortBy.ResumeDate }, CancellationToken.None);

            results.IsOrderedBy(x => x.ResumeDate).Should().BeTrue();
        }


        private class EnrollmentRepoStub : IEnrollmentRepository
        {
            private readonly IReadOnlyCollection<EnrollmentReadModel> _source;
            public EnrollmentRepoStub(IEnumerable<EnrollmentReadModel> source) => _source = source?.ToArray() ?? throw new ArgumentNullException(nameof(source));
            public EnrollmentRepoStub(params EnrollmentReadModel[] source) : this(source.AsEnumerable()) { }

            public Task Insert(EnrollmentReadModel entry) => throw new NotSupportedException();

            public IQueryable<EnrollmentReadModel> Query() => _source.AsQueryable();

            public Task Update(EnrollmentReadModel entry) => throw new NotSupportedException();
        }
    }
}

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
    public class GetSubmissionsQueryTests
    {
        private readonly Duration aWeek = Duration.FromDays(7);
        private readonly Duration aDay = Duration.FromDays(1);
        private readonly Duration anHour = Duration.FromHours(1);
        private readonly OffsetDateTime now = SystemClock.Instance.GetOffsetDateTime();
        private readonly IClock clock = SystemClock.Instance;

        [Fact(DisplayName = "Po wypełnieniu formularza rekrutacyjnego z ostatniej kampanii rekrutacyjnej, wynik ma ustawioną flagę IsCurrentSubmission")]
        public async Task When_recruitment_form_from_latest_recruitment_campaign_is_submitted__its_summary_has_flag_IsCurrentSubmission_set_to_true()
        {
            // Arrange
            var oldCampaign = new Impl.Entities.Campaign(now.Minus(Duration.FromDays(30)), now.Minus(Duration.FromDays(25)), editionId: 1);
            oldCampaign.GetType().GetProperty(nameof(oldCampaign.Id)).SetValue(oldCampaign, 1);

            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 2);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(oldCampaign)
                && mock.GetById(2) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { oldCampaign, newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepoStub(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 2 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>()
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var result = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query(), CancellationToken.None);

            // Assert
            var summary = Assert.Single(result);
            Assert.True(summary.Campaign.ID == 2);
            Assert.True(summary.IsCurrentSubmission);
        }

        [Fact(DisplayName = "Po wypełnieniu formularza rekrutacyjnego z ostatniej kampanii rekrutacyjnej, wynik ma wyłączoną flagę IsOldSubmission")]
        public async Task When_recruitment_form_from_latest_recruitment_campaign_is_submitted__its_summary_has_flag_IsOldSubmission_set_to_false()
        {
            // Arrange
            var oldCampaign = new Impl.Entities.Campaign(now.Minus(Duration.FromDays(30)), now.Minus(Duration.FromDays(25)), editionId: 1);
            oldCampaign.GetType().GetProperty(nameof(oldCampaign.Id)).SetValue(oldCampaign, 1);

            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 2);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(oldCampaign)
                && mock.GetById(2) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { oldCampaign, newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepoStub(new EnrollmentReadModel() {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 2 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>()
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var result = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query(), CancellationToken.None);

            // Assert
            var summary = Assert.Single(result);
            Assert.True(summary.Campaign.ID == 2);
            Assert.False(summary.IsOldSubmission);
        }

        [Fact(DisplayName = "Po wypełnieniu formularza rekrutacyjnego z przeszłej kampanii rekrutacyjnej, wynik ma ustawioną flagę IsOldSubmission")]
        public async Task When_recruitment_form_from_past_recruitment_campaign_is_submitted__its_summary_has_flag_IsNewSubmission_set_to_true()
        {
            // Arrange
            var oldCampaign = new Impl.Entities.Campaign(now.Minus(Duration.FromDays(30)), now.Minus(Duration.FromDays(25)), editionId: 1);
            oldCampaign.GetType().GetProperty(nameof(oldCampaign.Id)).SetValue(oldCampaign, 1);

            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 2);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(oldCampaign)
                && mock.GetById(2) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { oldCampaign, newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel() {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>() });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var result = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query(), CancellationToken.None);

            // Assert
            var summary = Assert.Single(result);
            Assert.True(summary.Campaign.ID == 1);
            Assert.True(summary.IsOldSubmission);
        }

        [Fact(DisplayName = "Po zgłoszeniu tymczasowej rezygnacji, jeśli nie podano czasu rezygnacji, wynik ma ustawioną flagę HasTemporarilyResigned bez ResumeTime")]
        public async Task When_temporary_resignation_is_recorded_without_resume_time__summary_has_flag_HasTemporarilyResigned_set_to_true_and_no_ResumeTime()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = null
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var result = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query(), CancellationToken.None);

            // Assert
            var summary = Assert.Single(result);
            Assert.True(summary.HasResignedTemporarily);
            Assert.Null(summary.ResumeDate);
        }

        [Fact(DisplayName = "Po zgłoszeniu tymczasowej rezygnacji, jeśli czas rezygnacji się nie zakończył, wynik ma ustawioną flagę HasTemporarilyResigned i ResumeTime")]
        public async Task When_temporary_resignation_is_recorded_with_resume_time_in_the_future__summary_has_flag_HasTemporarilyResigned_set_to_true_and_specified_ResumeTime()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = now.Plus(aDay).Date
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var result = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query(), CancellationToken.None);

            // Assert
            var summary = Assert.Single(result);
            Assert.True(summary.HasResignedTemporarily);
            Assert.Equal(now.Plus(aDay).Date, summary.ResumeDate);
        }

        [Fact(DisplayName = "Po zgłoszeniu tymczasowej rezygnacji, jeśli czas rezygnacji się zakończył, wynik nie ma ustawionej flagi HasTemporarilyResigned ani ResumeTime")]
        public async Task When_temporary_resignation_is_recorded_with_resume_time_in_the_past__summary_has_flag_HasTemporarilyResigned_set_to_false_and_no_ResumeTime()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = now.Minus(aDay).Date
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var result = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query(), CancellationToken.None);

            // Assert
            var summary = Assert.Single(result);
            Assert.False(summary.HasResignedTemporarily);
            Assert.Null(summary.ResumeDate);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=true zwraca wyniki dla których wpłynęła rezygnacja trwała")]
        public async Task When_queried_with_flag_HasResigned_set_to_true__results_contain_enrollments_with_permanent_resignation()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedPermanently = true
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = true }, CancellationToken.None);

            // Assert
            var summary = Assert.Single(results);
            Assert.True(summary.HasResignedPermanently);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=true zwraca wyniki dla których wpłynęła rezygnacja tymczasowa bez daty wznowienia")]
        public async Task When_queried_with_flag_HasResigned_set_to_true__results_contain_enrollments_with_temporary_resignations_without_ResumeDate()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = null
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = true }, CancellationToken.None);

            // Assert
            var summary = Assert.Single(results);
            Assert.True(summary.HasResignedTemporarily);
            Assert.Null(summary.ResumeDate);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=true zwraca wyniki dla których wpłynęła niezakończona rezygnacja tymczasowa")]
        public async Task When_queried_with_flag_HasResigned_set_to_true__results_contain_enrollments_with_temporary_resignations_with_future_ResumeDate()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = now.Plus(aDay).Date
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = true }, CancellationToken.None);

            // Assert
            var summary = Assert.Single(results);
            Assert.True(summary.HasResignedTemporarily);
            Assert.Equal(now.Plus(aDay).Date, summary.ResumeDate);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=true nie zwraca wyników dla których wpłynęła rezygnacja tymczasowa, ale jej ResumeDate już minął")]
        public async Task When_queried_with_flag_HasResigned_set_to_true__results_does_not_contain_enrollments_with_temporary_resignations_with_past_ResumeDate()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = now.Minus(aDay).Date
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = true }, CancellationToken.None);

            // Assert
            Assert.Empty(results);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=false nie zwraca wyników dla których wpłynęła rezygnacja trwała")]
        public async Task When_queried_with_flag_HasResigned_set_to_false__results_does_not_contain_enrollments_with_permanent_resignations()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedPermanently = true
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = false }, CancellationToken.None);

            // Assert
            Assert.Empty(results);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=false nie zwraca wyników dla których wpłynęła rezygnacja tymczasowa bez daty wznowienia")]
        public async Task When_queried_with_flag_HasResigned_set_to_false__results_do_not_contain_enrollments_with_temporary_resignations_without_ResumeDate()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = false }, CancellationToken.None);

            // Assert
            Assert.Empty(results);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=false nie zwraca wyników dla których wpłynęła niezakończona rezygnacja tymczasowa")]
        public async Task When_queried_with_flag_HasResigned_set_to_false__results_does_not_contain_enrollments_with_temporary_resignations_with_future_ResumeDate()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = now.Plus(aDay).Date
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = false }, CancellationToken.None);

            // Assert
            Assert.Empty(results);
        }

        [Fact(DisplayName = "Zapytanie z flagą HasResigned=false zwraca wyniki dla których wpłynęła rezygnacja tymczasowa, ale jej ResumeDate już minął")]
        public async Task When_queried_with_flag_HasResigned_set_to_false__results_contains_enrollments_with_temporary_resignations_with_past_ResumeDate()
        {
            // Arrange
            var newCampaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            newCampaign.GetType().GetProperty(nameof(newCampaign.Id)).SetValue(newCampaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            newCampaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock =>
                mock.GetById(1) == Task.FromResult(newCampaign)
                && mock.GetAll() == Task.FromResult(new[] { newCampaign } as IReadOnlyCollection<Impl.Entities.Campaign>), MockBehavior.Strict);

            var repo = new EnrollmentRepository();
            await repo.Insert(new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                Campaign = new EnrollmentReadModel.CampaignSummary() { Id = 1 },
                PreferredTrainings = Array.Empty<EnrollmentReadModel.TrainingSummary>(),
                PreferredLecturingCities = Array.Empty<string>(),
                HasResignedTemporarily = true,
                ResumeDate = now.Minus(aDay).Date
            });

            var queryHandler = new GetSubmissionsQueryHandler(repo, campaignRepo, clock);

            // Act
            var results = await queryHandler.Handle(new Recruitment.Enrollments.GetSubmissions.Query() { HasResigned = false }, CancellationToken.None);

            // Assert
            var summary = Assert.Single(results);
            Assert.False(summary.HasResignedTemporarily);
            Assert.Null(summary.ResumeDate);
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

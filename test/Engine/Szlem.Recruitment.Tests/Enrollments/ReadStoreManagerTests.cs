using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class ReadStoreManagerTests
    {
        private readonly Duration aWeek = Duration.FromDays(7);
        private readonly Duration aDay = Duration.FromDays(1);
        private readonly Duration anHour = Duration.FromHours(1);
        private readonly OffsetDateTime now = SystemClock.Instance.GetOffsetDateTime();

        [Fact(DisplayName = "Po wypełnieniu formularza rekrutacyjnego, repo zawiera readmodel tego formularza")]
        public async Task When_recruitment_form_is_submitted_repo_contains_its_readmodel()
        {
            // Arrange
            var repo = new EnrollmentRepository();
            var campaign = new Impl.Entities.Campaign(now.Minus(aWeek), now.Plus(aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);

            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Plus(2 * aWeek), now.Plus(2 * aWeek + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);

            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(MockBehavior.Strict);

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);


            // Act
            var @event = new RecruitmentFormSubmitted(
                now.ToInstant(), "Andrzej", "Strzelba",
                EmailAddress.Parse("andrzej@strzelba.com"), PhoneNumber.Parse("505551888"),
                "no elo",
                campaignID: 1, region: "Wolne Miasto Gdańsk", preferredLecturingCities: new[] { "Rumia", "Reda", "Wejherowo" }, preferredTrainingIds: new[] { 1 },
                gdprConsentGiven: true);
            var domainEvent = new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(@event, new Metadata(), DateTimeOffset.Now, EnrollmentAggregate.EnrollmentId.New, 1);

            await readStoreManager.UpdateReadStoresAsync(new[] { domainEvent }, CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal("Andrzej Strzelba", enrollment.FullName);
            Assert.Equal(EmailAddress.Parse("andrzej@strzelba.com"), enrollment.Email);
            Assert.Equal(PhoneNumber.Parse("505551888"), enrollment.PhoneNumber);
            Assert.Equal("no elo", enrollment.AboutMe);
            Assert.Equal("Wolne Miasto Gdańsk", enrollment.Region);
            Assert.Equal(1, enrollment.Campaign.Id);
            Assert.Null(enrollment.ResumeDate);
            Assert.Collection(enrollment.PreferredLecturingCities,
                first => Assert.Equal("Rumia", first),
                second => Assert.Equal("Reda", second),
                third => Assert.Equal("Wejherowo", third));
            var preferredTraining = Assert.Single(enrollment.PreferredTrainings);
            Assert.Equal("Wadowice", preferredTraining.City);
        }

        [Fact(DisplayName = "Po zarejestrowaniu trwałej rezygnacji, readmodel ma ustawioną flagę HasResignedPermanently")]
        public async Task When_permanent_resignation_is_recorded_readmodel_has_flag_HasResignedPermanently_set_to_true()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedPermanently>(
                    new CandidateResignedPermanently(Guid.NewGuid(), Recruitment.Enrollments.CommunicationChannel.IncomingPhone, string.Empty, string.Empty),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.True(enrollment.HasResignedPermanently);
        }

        [Fact(DisplayName = "Po zarejestrowaniu zdobycia uprawnień prowadzącego, readmodel ma ustawioną flagę HasLecturerRights")]
        public async Task When_candidate_obtains_lecturer_rights_Readmodel_has_flag_HasLecturerRigths_set_to_true()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateObtainedLecturerRights>(
                    new CandidateObtainedLecturerRights(Guid.NewGuid(), "zdobył uprawnienia"),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.True(enrollment.HasLecturerRights);
        }

        [Fact(DisplayName = "Po zarejestrowaniu tymczasowej rezygnacji, readmodel ma ustawioną flagę HasResignedTemporarily")]
        public async Task When_temporary_resignation_is_recorded_readmodel_has_flag_HasResignedTemporarily_set_to_true()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(Guid.NewGuid(), Recruitment.Enrollments.CommunicationChannel.IncomingPhone, string.Empty, string.Empty),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.True(enrollment.HasResignedTemporarily);
        }

        [Fact(DisplayName = "Po zarejestrowaniu tymczasowej rezygnacji z podaniem daty wznowienia, readmodel ma ustawioną datę wznowienia")]
        public async Task When_temporary_resignation_is_recorded_with_date_provided__readmodel_has_that_date_as_suspension_end_date()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(
                        Guid.NewGuid(),
                        Recruitment.Enrollments.CommunicationChannel.IncomingPhone,
                        string.Empty, string.Empty,
                        resumeDate: now.Plus(aWeek).Date),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.True(enrollment.HasResignedTemporarily);
            Assert.Equal(now.Plus(aWeek).Date, enrollment.ResumeDate);
        }

        [Fact(DisplayName = "Po zarejestrowaniu tymczasowej rezygnacji bez podania daty wznowienia, readmodel nie ma ustawionej daty wznowienia")]
        public async Task When_temporary_resignation_is_recorded_without_providing_end_date__readmodel_has_no_suspension_end_date()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(
                        Guid.NewGuid(),
                        Recruitment.Enrollments.CommunicationChannel.IncomingPhone,
                        string.Empty, string.Empty,
                        resumeDate: null),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.True(enrollment.HasResignedTemporarily);
            Assert.Null(enrollment.ResumeDate);
        }

        [Fact(DisplayName = "Po zaakceptowaniu zaproszenia na szkolenie, readmodel zawiera ID tego szkolenia")]
        public async Task When_training_invitation_is_accepted__readmodel_contains_that_training_ID()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(mock => mock.GetById(1) == Task.FromResult(Maybe<Training>.From(training)), MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(
                        Guid.NewGuid(),
                        Recruitment.Enrollments.CommunicationChannel.IncomingPhone,
                        1, string.Empty),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.NotNull(enrollment.SelectedTraining);
            Assert.Equal(1, enrollment.SelectedTraining.ID);
        }

        [Fact(DisplayName = "Po odrzuceniu zaproszenia na szkolenie, readmodel nie zawiera ID szkolenia")]
        public async Task When_training_invitation_is_refused__readmodel_does_not_contain_training_ID()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var repo = new EnrollmentRepository();

            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(2 * aWeek + anHour), now.Minus(2 * aWeek), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var campaignRepo = Mock.Of<ICampaignRepository>(mock => mock.GetById(1) == Task.FromResult(campaign), MockBehavior.Strict);
            var trainingRepo = Mock.Of<ITrainingRepository>(mock => mock.GetById(1) == Task.FromResult(Maybe<Training>.From(training)), MockBehavior.Strict);

            await repo.Insert(new EnrollmentReadModel() { Id = id, SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 1 } });

            var readStoreManager = new ReadStoreManager(repo, campaignRepo, trainingRepo);

            // Act
            await readStoreManager.UpdateReadStoresAsync(new[] {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                    new CandidateRefusedTrainingInvitation(
                        Guid.NewGuid(),
                        Recruitment.Enrollments.CommunicationChannel.IncomingPhone,
                        string.Empty, string.Empty),
                    new Metadata(), DateTimeOffset.Now, id, 1),
                },
                CancellationToken.None);

            // Assert
            var enrollment = Assert.Single(repo.Query());
            Assert.Equal(id, enrollment.Id);
            Assert.Null(enrollment.SelectedTraining);
        }
    }
}

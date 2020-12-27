using CSharpFunctionalExtensions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Recruitment.Trainings;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Impl.Trainings;
using Xunit;
using System.Threading;
using Szlem.Domain;
using EventFlow.Aggregates;
using Szlem.Recruitment.Impl.Enrollments.Events;

namespace Szlem.Recruitment.Tests.Trainings
{
    public class DetailsTests_FutureTraining
    {
        private readonly Duration aWeek = Duration.FromDays(7);
        private readonly Duration aDay = Duration.FromDays(1);
        private readonly Duration anHour = Duration.FromHours(1);
        private readonly OffsetDateTime now = SystemClock.Instance.GetOffsetDateTime();
        private readonly IClock clock = SystemClock.Instance;


        private Campaign BuildPastCampaign(int id)
        {
            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, id);
            return campaign;
        }

        private Training BuildFutureTraining(Guid trainerId, int id)
        {
            var training = new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + anHour), trainerId);
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, id);
            return training;
        }

        private (ICampaignRepository, ITrainingRepository, ITrainerProvider) BuildMocks(Guid trainerId, int campaignId, int trainingId)
        {
            var campaign = BuildPastCampaign(campaignId);
            var training = BuildFutureTraining(trainerId, trainingId);
            campaign.ScheduleTraining(training);

            var campaignRepo = Mock.Of<ICampaignRepository>(
                repo => repo.GetById(campaignId) == Task.FromResult(campaign));
            var trainingRepo = Mock.Of<ITrainingRepository>(
               repo => repo.GetById(trainingId) == Task.FromResult(Maybe<Training>.From(training)),
               MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(new TrainerDetails() { Guid = trainerId })),
                MockBehavior.Strict);

            return (campaignRepo, trainingRepo, trainerProvider);
        }


        #region PreferringCandidates
        [Fact(DisplayName = "Jeśli kandydat preferuje szkolenie, to znajduje się w kolekcji PreferringCandidates")]
        public async Task If_candidate_prefers_training__PreferringCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);

            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);

            var enrollment = new EnrollmentReadModel() {
                Id = EnrollmentAggregate.EnrollmentId.New,
                PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 1 } }
            };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            var candidate = Assert.Single(futureTraining.PreferringCandidates);
            Assert.Equal(enrollment.Id.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat nie preferuje szkolenia, to nie znajduje się w kolekcji PreferringCandidates")]
        public async Task If_candidate_does_not_prefer_training__PreferringCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);

            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);

            var enrollment = new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 2 } }
            };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.PreferringCandidates);
        }
        #endregion

        
        [Fact(DisplayName = "Jeśli kandydat nie został jeszcze zaproszony, to znajduje się w kolekcji AvailableCandidates i nie ma ustawionej żadnej z flag")]
        public async Task If_candidate_was_not_yet_invited__AvailableCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);

            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);

            var enrollment = new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 1 } },
            };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            var candidate = Assert.IsType<Details.FutureTrainingParticipant>(Assert.Single(futureTraining.AvailableCandidates));
            Assert.Equal(enrollment.Id.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat został zaproszony na nadchodzące szkolenie, to nie znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_was_invited_to_training__AvailableCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();

            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);

            var enrollment = new EnrollmentReadModel()
            {
                Id = EnrollmentAggregate.EnrollmentId.New,
                PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 1 } },
                SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 1 }
            };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.AvailableCandidates);
        }


        [Fact(DisplayName = "Jeśli kandydat został zaproszony na nadchodzące szkolenie, a potem zrezygnował ze szkolenia, to znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_was_invited_and_then_resigned_from_that_training__AvailableCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                    new CandidateRefusedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            var candidate = Assert.Single(futureTraining.AvailableCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat został zaproszony na nadchodzące szkolenie, a potem zrezygnował ze szkolenia, to nie znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_and_then_resigned_from_that_training__InvitedCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateRefusedTrainingInvitation>(
                    new CandidateRefusedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.InvitedCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował trwale z udziału w projekcie, to nie znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_resigned_permanently__AvailableCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedPermanently>(
                    new CandidateResignedPermanently(trainerId, Recruitment.Enrollments.CommunicationChannel.IncomingApiRequest, string.Empty, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.AvailableCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat został zaproszony i zrezygnował trwale z udziału w projekcie, to nie znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_but_resigned_permanently__InvitedCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedPermanently>(
                    new CandidateResignedPermanently(trainerId, Recruitment.Enrollments.CommunicationChannel.IncomingApiRequest, string.Empty, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.InvitedCandidates);
            Assert.Empty(futureTraining.AvailableCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo z udziału w projekcie bez podania daty wznowienia, to znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_resigned_temporarily_without_resume_date__AvailableCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty, resumeDate: null),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.InvitedCandidates);
            var candidate = Assert.Single(futureTraining.AvailableCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat został zaproszony i zrezygnował tymczasowo z udziału w projekcie bez podania daty wznowienia, to znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_was_invited_and_resigned_temporarily_without_resume_date__AvailableCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty, resumeDate: null),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            var candidate = Assert.Single(futureTraining.AvailableCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat został zaproszony i zrezygnował tymczasowo z udziału w projekcie bez podania daty wznowienia, to nie znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_and_resigned_temporarily_without_resume_date__InvitedCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty, resumeDate: null),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.InvitedCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo z udziału w projekcie, a data wznowienia jest późniejsza niż data szkolenia, to nie znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_resigned_temporarily_with_resume_date_later_than_training_date__AvailableCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty, resumeDate: now.Plus(2 * aWeek).Date),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.AvailableCandidates);
            Assert.Empty(futureTraining.InvitedCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat zrezygnował tymczasowo z udziału w projekcie, a data wznowienia jest wcześniejsza niż data szkolenia, to znajduje się w kolekcji AvailableCandidates")]
        public async Task If_candidate_resigned_temporarily_with_resume_date_earlier_than_training_date__AvailableCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty, resumeDate: now.Plus(aDay).Date),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            var candidate = Assert.Single(futureTraining.AvailableCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat został zaproszony, zrezygnował tymczasowo z udziału w projekcie, a data wznowienia jest wcześniejsza niż data szkolenia, to nie znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_and_resigned_temporarily_with_resume_date_earlier_than_training_date__InvitedCandidates_do_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            (var campaignRepo, var trainingRepo, var trainerProvider) = BuildMocks(trainerId, 1, 1);
            var enrollmentId = EnrollmentAggregate.EnrollmentId.New;
            var enrollmentRepo = new EnrollmentRepository();
            var readStoreManager = new ReadStoreManager(enrollmentRepo, campaignRepo, trainingRepo);

            var events = new IDomainEvent[]
            {
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, RecruitmentFormSubmitted>(
                    new RecruitmentFormSubmitted(clock.GetCurrentInstant().Minus(anHour), "Andrzej", "Strzelba", EmailAddress.Parse("andrzej@strzelba.com"), Consts.FakePhoneNumber, "jo", 1, "Wolne Miasto Gdańsk", new[] { "Gdańsk", "Wrzeszcz", "Oliwa" }, new[] { 1 }, true),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 1),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAcceptedTrainingInvitation>(
                    new CandidateAcceptedTrainingInvitation(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2),
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateResignedTemporarily>(
                    new CandidateResignedTemporarily(trainerId, Recruitment.Enrollments.CommunicationChannel.OutgoingPhone, string.Empty, string.Empty, resumeDate: now.Plus(aDay).Date),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Future, result.Value.Timing);
            var futureTraining = Assert.IsType<Details.FutureTrainingDetails>(result.Value);
            Assert.Empty(futureTraining.InvitedCandidates);
        }
    }
}

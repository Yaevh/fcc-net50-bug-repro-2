using CSharpFunctionalExtensions;
using EventFlow.Aggregates;
using FluentAssertions;
using Moq;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Impl.Trainings;
using Szlem.Recruitment.Trainings;
using Xunit;

namespace Szlem.Recruitment.Tests.Trainings
{
    public class DetailsTests_PastTraining
    {
        private readonly Duration aWeek = Duration.FromDays(7);
        private readonly Duration aDay = Duration.FromDays(1);
        private readonly Duration anHour = Duration.FromHours(1);
        private readonly OffsetDateTime now = SystemClock.Instance.GetOffsetDateTime();
        private readonly IClock clock = SystemClock.Instance;

        private Impl.Entities.Campaign BuildPastCampaign(int id)
        {
            var campaign = new Impl.Entities.Campaign(now.Minus(3 * aWeek), now.Minus(2 * aWeek), editionId: 1);
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, id);
            return campaign;
        }

        private Impl.Entities.Training BuildPastTraining(Guid trainerId, int id)
        {
            var training = new Impl.Entities.Training("Papieska 21/37", "Wadowice", now.Minus(aWeek), now.Minus(aWeek).Plus(anHour), trainerId);
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, id);
            return training;
        }

        private (ICampaignRepository, ITrainingRepository, ITrainerProvider) BuildMocks(Guid trainerId, int campaignId, int trainingId)
        {
            var campaign = BuildPastCampaign(campaignId);
            var training = BuildPastTraining(trainerId, trainingId);
            campaign.ScheduleTraining(training);

            var campaignRepo = Mock.Of<ICampaignRepository>(
                repo => repo.GetById(campaignId) == Task.FromResult(campaign));
            var trainingRepo = Mock.Of<ITrainingRepository>(
               repo => repo.GetById(trainingId) == Task.FromResult(Maybe<Impl.Entities.Training>.From(training)),
               MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(new TrainerDetails() { Guid = trainerId })),
                MockBehavior.Strict);

            return (campaignRepo, trainingRepo, trainerProvider);
        }


        #region neither present nor absent
        [Fact(DisplayName = "Jeśli kandydat nie został oznaczony ani jako obecny, ani nieobecny, to znajduje się w kolekcji UnreportedCandidates")]
        public async Task If_candidate_was_not_reported_as_present_or_absent__he_is_contained_in_UnreportedCandidates()
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
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            var candidate = Assert.Single(pastTraining.UnreportedCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);
        }

        [Fact(DisplayName = "Jeśli kandydat nie został oznaczony ani jako obecny, ani nieobecny, to nie znajduje się w kolekcji PresentCandidates")]
        public async Task If_candidate_was_not_reported_as_present_or_absent__he_is_not_contained_in_PresentCandidates()
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
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            Assert.Empty(pastTraining.PresentCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat nie został oznaczony ani jako obecny, ani nieobecny, to nie znajduje się w kolekcji AbsentCandidates")]
        public async Task If_candidate_was_not_reported_as_present_or_absent__he_is_not_contained_in_AbsentCandidates()
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
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 2)
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            Assert.Empty(pastTraining.AbsentCandidates);
        }
        #endregion

        #region present
        [Fact(DisplayName = "Jeśli kandydat był obecny, to znajduje się w kolekcji PresentCandidates")]
        public async Task If_candidate_was_present_on_training__he_is_contained_in_PresentCandidates()
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
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAttendedTraining>(
                    new CandidateAttendedTraining(trainerId, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3),
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            var candidate = Assert.Single(pastTraining.PresentCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);
            candidate.HasRefusedTraining.Should().BeFalse();
            candidate.HasResignedPermanently.Should().BeFalse();
            candidate.HasResignedTemporarily.Should().BeFalse();
            candidate.ResignationEndDate.Should().BeNull();
            candidate.WasAbsent.Should().BeFalse();
            candidate.WasPresentButDidNotAcceptedAsLecturer.Should().BeTrue();
        }

        [Fact(DisplayName = "Jeśli kandydat był obecny, to nie znajduje się w kolekcji AbsentCandidates")]
        public async Task If_candidate_was_present_on_training__he_is_not_contained_in_AbsentCandidates()
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
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAttendedTraining>(
                    new CandidateAttendedTraining(trainerId, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3),
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            Assert.Empty(pastTraining.AbsentCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat był obecny, to nie znajduje się w kolekcji UnreportedCandidates")]
        public async Task If_candidate_was_present_on_training__he_is_not_contained_in_UnreportedCandidates()
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
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateAttendedTraining>(
                    new CandidateAttendedTraining(trainerId, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3),
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            Assert.Empty(pastTraining.UnreportedCandidates);
        }
        #endregion

        #region absent
        [Fact(DisplayName = "Jeśli kandydat nie był na szkoleniu mimo zaproszenia, to nie znajduje się w kolekcji PresentCandidates")]
        public async Task If_candidate_was_absent_on_training__he_is_not_contained_in_PresentCandidates()
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
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateWasAbsentFromTraining>(
                    new CandidateWasAbsentFromTraining(trainerId, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3),
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            Assert.Empty(pastTraining.PresentCandidates);
        }

        [Fact(DisplayName = "Jeśli kandydat nie był na szkoleniu mimo zaproszenia, to znajduje się w kolekcji AbsentCandidates")]
        public async Task If_candidate_was_absent_on_training__he_is_contained_in_AbsentCandidates()
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
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateWasAbsentFromTraining>(
                    new CandidateWasAbsentFromTraining(trainerId, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3),
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            var candidate = Assert.Single(pastTraining.AbsentCandidates);
            Assert.Equal(enrollmentId.GetGuid(), candidate.Id);

            candidate.WasAbsent.Should().BeTrue();
            candidate.WasPresentAndAcceptedAsLecturer.Should().BeFalse();
            candidate.WasPresentButDidNotAcceptedAsLecturer.Should().BeFalse();
            candidate.WasAbsent.Should().BeTrue();
            candidate.HasRefusedTraining.Should().BeFalse();
            candidate.HasResignedPermanently.Should().BeFalse();
            candidate.HasResignedTemporarily.Should().BeFalse();
            candidate.ResignationEndDate.Should().BeNull();
        }

        [Fact(DisplayName = "Jeśli kandydat nie był na szkoleniu mimo zaproszenia, to nie znajduje się w kolekcji UnreportedCandidates")]
        public async Task If_candidate_was_absent_on_training__he_is_not_contained_in_UnreportedCandidates()
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
                new DomainEvent<EnrollmentAggregate, EnrollmentAggregate.EnrollmentId, CandidateWasAbsentFromTraining>(
                    new CandidateWasAbsentFromTraining(trainerId, 1, string.Empty),
                    new Metadata(), clock.GetCurrentInstant().ToDateTimeOffset(), enrollmentId, 3),
            };
            await readStoreManager.UpdateReadStoresAsync(events, CancellationToken.None);

            // Act
            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            Assert.Empty(pastTraining.UnreportedCandidates);
        }
        #endregion
    }
}

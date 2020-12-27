using CSharpFunctionalExtensions;
using FluentAssertions;
using FluentAssertions.Common;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Impl.Trainings;
using Szlem.Recruitment.Trainings;
using Xunit;

namespace Szlem.Recruitment.Tests.Trainings
{
    public class DetailsTests
    {
        private readonly Duration aWeek = Duration.FromDays(7);
        private readonly Duration aDay = Duration.FromDays(1);
        private readonly Duration anHour = Duration.FromHours(1);
        private readonly OffsetDateTime now = SystemClock.Instance.GetOffsetDateTime();
        private readonly NodaTime.IClock clock = SystemClock.Instance;


        private void SetId(Training training, int id)
        {
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
        }

        #region typy szkoleń (nadchodzące, trwające, przeszłe)
        [Fact(DisplayName = "Nadchodzące szkolenia mają flagę Timing=Future i typ FutureTrainingDetails")]
        public async Task Future_training_has_flag_Timing_equal_to_Future_and_type_FutureTrainingDetails()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTtraining = new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + anHour), trainerId);
            SetId(sourceTtraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTtraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == Array.Empty<EnrollmentReadModel>().AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            var training = result.Value;
            Assert.Equal(1, training.Id);
            Assert.Equal(TrainingTiming.Future, training.Timing);
            Assert.IsType<Details.FutureTrainingDetails>(training);
        }

        [Fact(DisplayName = "Przeszłe szkolenia mają flagę Timing=Past i typ PastTrainingDetails")]
        public async Task Past_training_has_flag_Timing_equal_to_Past_and_type_PastTrainingDetails()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTtraining = new Training("Papieska 21/37", "Wadowice", now.Minus(aWeek), now.Minus(aWeek).Plus(anHour), trainerId);
            SetId(sourceTtraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTtraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == Array.Empty<EnrollmentReadModel>().AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            var training = result.Value;
            Assert.Equal(1, training.Id);
            Assert.Equal(TrainingTiming.Past, training.Timing);
            Assert.IsType<Details.PastTrainingDetails>(training);
        }

        [Fact(DisplayName = "Trwające szkolenia mają flagę Timing=Current i typ CurrentTrainingDetails")]
        public async Task Current_training_has_flag_Timing_equal_Current_and_type_CurrentTrainingDetails()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTtraining = new Training("Papieska 21/37", "Wadowice", now.Minus(anHour), now.Plus(anHour), trainerId);
            SetId(sourceTtraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTtraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == Array.Empty<EnrollmentReadModel>().AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            var training = result.Value;
            Assert.Equal(1, training.Id);
            Assert.Equal(TrainingTiming.Current, training.Timing);
            Assert.IsType<Details.CurrentTrainingDetails>(training);
        }
        #endregion

        #region InvitedCandidates
        [Fact(DisplayName = "Jeżeli kandydat został zaproszony na nadchodzące szkolenie, to znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_to_future_training__InvitedCandidates_contain_that_candidate()
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
            var enrollment = new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.New, SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 1 } };
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
            futureTraining.InvitedCandidates.Should().ContainSingle();
            
            var candidate = futureTraining.AllCandidates.Should().ContainSingle().Subject
                .Should().BeOfType<Details.FutureTrainingParticipant>().Subject;
            
            candidate.Id.Should().IsSameOrEqualTo(enrollment.Id.GetGuid());
            candidate.HasAccepted.Should().BeTrue();
            candidate.HasLecturerRights.Should().BeFalse();
            candidate.HasRefusedTraining.Should().BeFalse();
            candidate.HasResignedPermanently.Should().BeFalse();
            candidate.HasResignedTemporarily.Should().BeFalse();
            candidate.ResignationEndDate.Should().BeNull();
        }

        [Fact(DisplayName = "Jeżeli kandydat został zaproszony na trwające szkolenie, to znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_to_current_training__InvitedCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Minus(anHour), now.Plus(anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollment = new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.New, SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 1 } };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Current, result.Value.Timing);
            var currentTraining = Assert.IsType<Details.CurrentTrainingDetails>(result.Value);
            var invitedCandidate = Assert.Single(currentTraining.InvitedCandidates);
            Assert.Equal(enrollment.Id.GetGuid(), invitedCandidate.Id);

            var candidate = currentTraining.AllCandidates.Should().ContainSingle().Subject
                .Should().BeOfType<Details.CurrentTrainingParticipant>().Subject;

            candidate.Id.Should().IsSameOrEqualTo(enrollment.Id.GetGuid());
            candidate.ChoseAnotherTraining.Should().BeFalse();
            candidate.HasLecturerRights.Should().BeFalse();
            candidate.HasRefusedTraining.Should().BeFalse();
            candidate.HasResignedPermanently.Should().BeFalse();
            candidate.HasResignedTemporarily.Should().BeFalse();
            candidate.ResignationEndDate.Should().BeNull();
        }

        [Fact(DisplayName = "Jeżeli kandydat preferujący trwające szkolenie wybrał inne szkolenie, to nie znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_preferring_current_training_chose_another_training__InvitedCandidates_does_not_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Minus(anHour), now.Plus(anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollment = new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.New, PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 1 } }, SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 2137 } };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Current, result.Value.Timing);
            var currentTraining = Assert.IsType<Details.CurrentTrainingDetails>(result.Value);
            Assert.Empty(currentTraining.InvitedCandidates);
        }

        [Fact(DisplayName = "Jeżeli kandydat preferujący trwające szkolenie wybrał inne szkolenie, to znajduje się w kolekcji PreferringCandidates")]
        public async Task If_candidate_preferring_current_training_chose_another_training__PreferringCandidates_contains_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Minus(anHour), now.Plus(anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollment = new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.New, PreferredTrainings = new[] { new EnrollmentReadModel.TrainingSummary() { ID = 1 } }, SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 2137 } };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Current, result.Value.Timing);
            var currentTraining = Assert.IsType<Details.CurrentTrainingDetails>(result.Value);
            var preferringCandidate = Assert.Single(currentTraining.PreferringCandidates);
            Assert.Equal(enrollment.Id.GetGuid(), preferringCandidate.Id);

            var candidate = currentTraining.AllCandidates.Should().ContainSingle().Subject
                .Should().BeOfType<Details.CurrentTrainingParticipant>().Subject;
            candidate.Should().BeEquivalentTo(preferringCandidate);

            candidate.Id.Should().IsSameOrEqualTo(enrollment.Id.GetGuid());
            candidate.ChoseAnotherTraining.Should().BeTrue();
            candidate.HasLecturerRights.Should().BeFalse();
            candidate.HasRefusedTraining.Should().BeFalse();
            candidate.HasResignedPermanently.Should().BeFalse();
            candidate.HasResignedTemporarily.Should().BeFalse();
            candidate.ResignationEndDate.Should().BeNull();
        }

        [Fact(DisplayName = "Jeżeli kandydat został zaproszony na przeszłe szkolenie, to znajduje się w kolekcji InvitedCandidates")]
        public async Task If_candidate_was_invited_to_past_training__InvitedCandidates_contain_that_candidate()
        {
            // Arrange
            var trainerId = Guid.NewGuid();
            var sourceTraining = new Training("Papieska 21/37", "Wadowice", now.Minus(aWeek), now.Minus(aWeek + anHour), trainerId);
            sourceTraining.GetType().GetProperty(nameof(sourceTraining.ID)).SetValue(sourceTraining, 1);
            var trainingRepo = Mock.Of<ITrainingRepository>(
                repo => repo.GetById(1) == Task.FromResult(Maybe<Training>.From(sourceTraining)),
                MockBehavior.Strict);
            var trainerProvider = Mock.Of<ITrainerProvider>(
                provider => provider.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(
                    new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict);
            var enrollment = new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.New, SelectedTraining = new EnrollmentReadModel.TrainingSummary() { ID = 1 } };
            var enrollmentRepo = Mock.Of<IEnrollmentRepository>(
                repo => repo.Query() == new[] { enrollment }.AsQueryable(),
                MockBehavior.Strict);

            var handler = new DetailsHandler(clock, trainingRepo, trainerProvider, enrollmentRepo);

            // Act
            var result = await handler.Handle(new Details.Query() { TrainingId = 1 }, CancellationToken.None);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(TrainingTiming.Past, result.Value.Timing);
            var pastTraining = Assert.IsType<Details.PastTrainingDetails>(result.Value);
            var invitedCandidate = Assert.Single(pastTraining.InvitedCandidates);

            var candidate = pastTraining.AllCandidates.Should().ContainSingle().Subject
                .Should().BeOfType<Details.PastTrainingParticipant>().Subject;

            candidate.Id.Should().IsSameOrEqualTo(enrollment.Id.GetGuid());
            candidate.HasLecturerRights.Should().BeFalse();
            candidate.HasRefusedTraining.Should().BeFalse();
            candidate.HasResignedPermanently.Should().BeFalse();
            candidate.HasResignedTemporarily.Should().BeFalse();
            candidate.ResignationEndDate.Should().BeNull();
        }

        #endregion
    }
}

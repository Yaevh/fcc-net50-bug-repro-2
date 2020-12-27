using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Szlem.Recruitment.Trainings;
using Xunit;

namespace Szlem.Recruitment.Tests.Trainings
{
    public class AddNoteTests
    {
        private readonly Duration aWeek = Duration.FromDays(7);
        private readonly Duration aDay = Duration.FromDays(1);
        private readonly Duration anHour = Duration.FromHours(1);
        private readonly OffsetDateTime now = SystemClock.Instance.GetOffsetDateTime();
        private readonly IClock clock = FakeClock.FromUtc(2020, 04, 20);
        private readonly Guid trainerId = Guid.NewGuid();

        private Training Training { get => new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + aDay), trainerId); }

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(Mock.Of<IUserAccessor>(ua =>
                ua.GetUser() == Task.FromResult(new Models.Users.ApplicationUser() { UserName = "Andrzej Strzelba", Id = trainerId })));

            services.AddSingleton(Mock.Of<ITrainerProvider>(tp =>
                tp.GetTrainerDetails(trainerId) == Task.FromResult(Maybe<TrainerDetails>.From(new TrainerDetails() { Guid = trainerId, Name = "Andrzej Strzelba" })),
                MockBehavior.Strict));

            services.AddSingleton<IClock>(clock);
            var editionProviderMock = new Moq.Mock<DependentServices.IEditionProvider>();
            editionProviderMock
                .Setup(x => x.GetEdition(1))
                .Returns(Task.FromResult(Maybe<DependentServices.EditionDetails>.From(new DependentServices.EditionDetails()
                {
                    Id = 1,
                    StartDateTime = new NodaTime.LocalDateTime(2020, 09, 01, 00, 00).InMainTimezone().ToInstant(),
                    EndDateTime = new NodaTime.LocalDateTime(2021, 06, 30, 00, 00).InMainTimezone().ToInstant()
                })));
            services.AddSingleton<DependentServices.IEditionProvider>(editionProviderMock.Object);
        }


        [Theory(DisplayName = "Notatka musi posiadać treść")]
        [InlineData((string)null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Content_is_required(string content)
        {
            var training = new Training("Papieska 21/37", "Wadowice", now.Plus(aWeek), now.Plus(aWeek + aDay), trainerId);
            var result = Training.AddNote(trainerId, content, clock.GetCurrentInstant());
            Assert.False(result.IsSuccess);
            Assert.Empty(training.Notes);
        }

        [Fact(DisplayName = "Notatka musi wskazywać na istniejące szkolenie")]
        public async Task Request_must_point_to_existing_training()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var mediator = sp.GetRequiredService<IMediator>();
            
            var result = await mediator.Send(new AddNote.Command() { Content = "test", TrainingId = 2 });

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
            Assert.Equal(ErrorMessages.TrainingNotFound, error.Message);
        }

        [Fact(DisplayName = "Po dodaniu notatki, szczegóły szkolenia zawierają tą notatkę")]
        public async Task After_adding_note__Training_contains_that_note()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);

            // Arrange
            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(new Recruitment.Campaigns.Create.Command()
                {
                    Name = "test",
                    EditionID = 1,
                    StartDateTime = new NodaTime.LocalDateTime(2020, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    EndDateTime = new NodaTime.LocalDateTime(2020, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
                });

                await mediator.Send(new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2020, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2020, 10, 15, 15, 00),
                    CampaignID = 1
                });
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = sp.GetRequiredService<IMediator>();

                // Act
                var result = await mediator.Send(new AddNote.Command() { Content = "test", TrainingId = 1 });

                // Assert
                Assert.True(result.IsSuccess);

                var maybeTrainingDetails = await mediator.Send(new Details.Query() { TrainingId = 1 });
                Assert.True(maybeTrainingDetails.HasValue);
                var trainingDetails = maybeTrainingDetails.Value;

                var note = Assert.Single(trainingDetails.Notes);
                Assert.Equal("test", note.Content);
                Assert.Equal("Andrzej Strzelba", note.AuthorName);
                Assert.Equal(clock.GetZonedDateTime(), note.Timestamp);
            }
        }
    }
}

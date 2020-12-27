using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl;
using Szlem.Recruitment.Impl.Entities;
using Xunit;

namespace Szlem.Recruitment.Tests.Trainings
{
    public class IndexTests
    {
        private readonly Guid trainerID = Guid.NewGuid();

        private void ConfigureServices(IServiceCollection services)
        {
            var trainer = new TrainerDetails() { Guid = trainerID, Name = "Jan Paweł II" };
            var trainerResult = Maybe<TrainerDetails>.From(trainer);
            var trainerProviderMock = new Mock<ITrainerProvider>(MockBehavior.Strict);
            trainerProviderMock.Setup(x => x.GetTrainerDetails(trainerID)).Returns(Task.FromResult(trainerResult));
            trainerProviderMock.Setup(x => x.GetTrainerDetails(It.IsAny<IReadOnlyCollection<Guid>>())).Returns(Task.FromResult(new[] { trainerResult.Value } as IReadOnlyCollection<TrainerDetails>));

            var editionProviderMock = new Moq.Mock<DependentServices.IEditionProvider>();
            editionProviderMock
                .Setup(x => x.GetEdition(1))
                .Returns(Task.FromResult(Maybe<DependentServices.EditionDetails>.From(new DependentServices.EditionDetails()
                {
                    Id = 1,
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 00, 00).InMainTimezone().ToOffsetDateTime().ToInstant(),
                    EndDateTime = new NodaTime.LocalDateTime(2020, 06, 30, 00, 00).InMainTimezone().ToOffsetDateTime().ToInstant()
                })));
            services.AddSingleton<DependentServices.IEditionProvider>(editionProviderMock.Object);

            services.AddSingleton
                (Mock.Of<Engine.Infrastructure.IRequestAuthorizationAnalyzer>(
                    x => x.Authorize(It.IsAny<IBaseRequest>()) == Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success())));

            services.Remove<NodaTime.IClock>();
            services.AddSingleton<NodaTime.IClock>(NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01));

            services.AddSingleton(trainerProviderMock.Object);
        }

        [Fact]
        public async Task Empty_repository_returns_no_trainings()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var dbSession = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "test");
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query());

                Assert.Empty(results);
            }
        }

        [Fact]
        public async Task Query_with_specified_CampaignId_returns_trainings_from_that_campaign()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 11", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 02, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 02, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var campaign2 = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 10, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign2");
                campaign2.ScheduleTraining(new Training(
                    "Papieska 2", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 11, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 11, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign2.ScheduleTraining(new Training(
                    "Papieska 22", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 11, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 11, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
                await session.SaveAsync(campaign2);
                Assert.Equal(2, campaign2.Id);
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query() { CampaignIds = new[] { 1 } });

                Assert.Equal(2, results.Count);
                Assert.Collection(results,
                    first => Assert.Equal("Papieska 1", first.Address),
                    second => Assert.Equal("Papieska 11", second.Address)
                );
            }
        }

        [Fact]
        public async Task Query_with_specified_From_returns_trainings_beginning_after_that_instant()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 15, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 15, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query() { From = new NodaTime.LocalDateTime(2019, 10, 01, 13, 00).InMainTimezone().ToOffsetDateTime().ToInstant() });

                var singleResult = Assert.Single(results);
                Assert.Equal("Papieska 2", singleResult.Address);
            }
        }

        [Fact]
        public async Task Query_with_specified_To_returns_trainings_ending_before_that_instant()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 15, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 15, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query() { To = new NodaTime.LocalDateTime(2019, 10, 15, 13, 00).InMainTimezone().ToOffsetDateTime().ToInstant() });

                var singleResult = Assert.Single(results);
                Assert.Equal("Papieska 1", singleResult.Address);
            }
        }

        [Fact]
        public async Task Query_with_specified_cities_returns_trainings_happening_in_those_cities()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Sosnowiec",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query() { City = "wadowice" });

                var singleResult = Assert.Single(results);
                Assert.Equal("Wadowice", singleResult.City);
            }
        }

        [Fact]
        public async Task Query_with_specified_coordinator_returns_trainings_conducted_by_that_coordinator()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Sosnowiec",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    Guid.NewGuid()));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query() { CoordinatorId = trainerID });

                var singleResult = Assert.Single(results);
                Assert.Equal("Wadowice", singleResult.City);
                Assert.Equal(trainerID, singleResult.CoordinatorID);
            }
        }

        [Fact(DisplayName = "Nadchodzące szkolenia mają ustawioną flagę Timing=Future")]
        public async Task Trainings_happening_in_the_future_have_Timing_flag_set_to_Future()
        {
            var clock = NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01);

            var sp = new ServiceProviderBuilder().BuildServiceProvider(services =>
            {
                ConfigureServices(services);
                services.Remove<NodaTime.IClock>();
                services.AddSingleton<NodaTime.IClock>(clock);
            });

            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Sosnowiec",
                    new NodaTime.LocalDateTime(2019, 11, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 11, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            clock.Reset(NodaTime.Instant.FromDateTimeUtc(new DateTime(2019, 10, 05, 00, 00, 00, DateTimeKind.Utc)));

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query());

                var singleResult = Assert.Single(results, x => x.Timing == Recruitment.Trainings.TrainingTiming.Future);
                Assert.Equal("Sosnowiec", singleResult.City);
            }
        }

        [Fact(DisplayName = "Przeszłe szkolenia mają ustawioną flagę Timing=Past")]
        public async Task Trainings_happening_in_the_past_have_Timing_flag_set_to_Past()
        {
            var clock = NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01);

            var sp = new ServiceProviderBuilder().BuildServiceProvider(services =>
            {
                ConfigureServices(services);
                services.Remove<NodaTime.IClock>();
                services.AddSingleton<NodaTime.IClock>(clock);
            });

            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Sosnowiec",
                    new NodaTime.LocalDateTime(2019, 11, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 11, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            clock.Reset(NodaTime.Instant.FromDateTimeUtc(new DateTime(2019, 10, 05, 00, 00, 00, DateTimeKind.Utc)));

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query());

                var singleResult = Assert.Single(results, x => x.Timing == Recruitment.Trainings.TrainingTiming.Past);
                Assert.Equal("Wadowice", singleResult.City);
            }
        }

        [Fact(DisplayName = "Szkolenia trwające w chwili zapytania mają ustawioną flagę Timing=HappeningNow")]
        public async Task Trainings_happening_at_the_moment_of_query_have_Timing_flag_set_to_HappeningNow()
        {
            var clock = NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01);

            var sp = new ServiceProviderBuilder().BuildServiceProvider(services =>
            {
                ConfigureServices(services);
                services.Remove<NodaTime.IClock>();
                services.AddSingleton<NodaTime.IClock>(clock);
            });

            using (var scope = sp.CreateScope())
            {
                var dbSessionProvider = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                var campaign = new Campaign(
                    startDateTime: new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    endDateTime: new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    editionId: 1, name: "campaign");
                campaign.ScheduleTraining(new Training(
                    "Papieska 1", "Wadowice",
                    new NodaTime.LocalDateTime(2019, 10, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 10, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));
                campaign.ScheduleTraining(new Training(
                    "Papieska 2", "Sosnowiec",
                    new NodaTime.LocalDateTime(2019, 11, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                    new NodaTime.LocalDateTime(2019, 11, 01, 16, 00).InMainTimezone().ToOffsetDateTime(),
                    trainerID));

                var session = dbSessionProvider.CreateSession();
                await session.SaveAsync(campaign);
                Assert.Equal(1, campaign.Id);
            }

            clock.Reset(NodaTime.Instant.FromDateTimeUtc(new DateTime(2019, 10, 01, 14, 00, 00, DateTimeKind.Utc)));

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var results = await mediator.Send(new Recruitment.Trainings.Index.Query());

                var singleResult = Assert.Single(results, x => x.Timing == Recruitment.Trainings.TrainingTiming.Future);
                Assert.Equal("Sosnowiec", singleResult.City);
            }
        }
    }
}

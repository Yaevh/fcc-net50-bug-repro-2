using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Impl;
using Szlem.Recruitment.Impl.Entities;
using Xunit;

namespace Szlem.Recruitment.Tests.Campaigns
{
    public class DetailsTests
    {
        private readonly Guid trainerID = Guid.NewGuid();

        private void ConfigureServices(IServiceCollection services)
        {
            var trainer = new TrainerDetails() { Guid = trainerID, Name = "Jan Paweł II" };
            var trainerResult = Maybe<TrainerDetails>.From(trainer);
            var trainerProviderMock = new Mock<ITrainerProvider>(MockBehavior.Strict);
            trainerProviderMock.Setup(x => x.GetTrainerDetails(trainerID)).Returns(Task.FromResult(trainerResult));
            trainerProviderMock.Setup(x => x.GetTrainerDetails(It.IsAny<IReadOnlyCollection<Guid>>())).Returns(Task.FromResult(new[] { trainerResult.Value } as IReadOnlyCollection<TrainerDetails>));
            services.AddSingleton(trainerProviderMock.Object);

            var editionProviderMock = new Moq.Mock<DependentServices.IEditionProvider>();
            editionProviderMock
                .Setup(x => x.GetEdition(1))
                .Returns(Task.FromResult(Maybe<DependentServices.EditionDetails>.From(new DependentServices.EditionDetails()
                {
                    Id = 1,
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 00, 00).InMainTimezone().ToInstant(),
                    EndDateTime = new NodaTime.LocalDateTime(2020, 06, 30, 00, 00).InMainTimezone().ToInstant()
                })));
            services.AddSingleton<DependentServices.IEditionProvider>(editionProviderMock.Object);

            var authorizationProviderMock = new Moq.Mock<Engine.Infrastructure.IRequestAuthorizationAnalyzer>();
            authorizationProviderMock
                .Setup(x => x.Authorize(Moq.It.IsAny<IBaseRequest>()))
                .Returns(Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success()));
            services.AddSingleton(authorizationProviderMock.Object);

            services.AddSingleton<NodaTime.IClock>(NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01));
        }

        [Theory(DisplayName = "When querying for non-existent CampaignID, ResourceNotFound is returned")]
        [InlineData(0)]
        [InlineData(2)]
        [InlineData(-1)]
        public async Task QueryingForNonExistentId_ReturnsResourceNotFound(int id)
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var mediator = sp.GetRequiredService<IMediator>();
            var query = new Details.Query() { CampaignID = id };

            var result = await mediator.Send(query);

            Assert.False(result.IsSuccess);
            Assert.IsType<Error.ResourceNotFound>(result.Error);
        }

        [Fact]
        public async Task QueryingForExistingCampaign_ReturnsThatCampaign()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var start = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime();
            var end = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime();

            using (var scope = sp.CreateScope())
            {
                var dbSession = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                await dbSession.CreateSession().SaveAsync(new Campaign(
                    startDateTime: start, endDateTime: end,
                    editionId: 1, name: "test"));
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var query = new Details.Query() { CampaignID = 1 };
                var result = await mediator.Send(query);

                result.IsSuccess.Should().BeTrue();
                result.Value.Should().BeEquivalentTo(new Details.Campaign() {
                    ID = 1, Name = "test",
                    StartDateTime = start, EndDateTime = end,
                    CanScheduleTraining = true, IsRecruitmentFormOpen = false,
                    Trainings = Array.Empty<Recruitment.Trainings.TrainingSummary>()
                });
            }
        }
    }
}

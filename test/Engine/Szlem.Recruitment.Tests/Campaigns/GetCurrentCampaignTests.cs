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
    public class GetCurrentCampaignTests
    {
        private void ConfigureServices(IServiceCollection services)
        {
            var trainerProviderMock = new Mock<ITrainerProvider>(MockBehavior.Strict);
            trainerProviderMock.Setup(x => x.GetTrainerDetails(It.IsAny<IReadOnlyCollection<Guid>>())).Returns(Task.FromResult(new TrainerDetails[0] as IReadOnlyCollection<TrainerDetails>));
            services.AddSingleton(trainerProviderMock.Object);
        }


        [Fact(DisplayName = "Jeśli aktualnie trwa kampania, handler zwraca tą kampanię")]
        public async Task If_a_campaign_is_currently_open__handler_returns_that_campaign()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);

            using (var scope = sp.CreateScope())
            {
                var dbSession = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                await dbSession.CreateSession().SaveAsync(new Campaign(
                    startDateTime: NodaTime.SystemClock.Instance.GetOffsetDateTime().Minus(NodaTime.Duration.FromDays(7)),
                    endDateTime: NodaTime.SystemClock.Instance.GetOffsetDateTime().Plus(NodaTime.Duration.FromDays(7)),
                    editionId: 1, name: "test"));
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var query = new GetCurrentCampaign.Query();
                var result = await mediator.Send(query);

                Assert.True(result.HasValue);
                var campaign = result.Value;
                Assert.Equal("test", campaign.Name);
            }
        }

        [Fact(DisplayName = "Jeśli aktualnie nie trwa żadna kampania, handler zwraca Maybe.None")]
        public async Task If_no_campaign_is_currently_open__handler_returns_Maybe_None()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);

            using (var scope = sp.CreateScope())
            {
                var dbSession = scope.ServiceProvider.GetRequiredService<DbSessionProvider>();
                await dbSession.CreateSession().SaveAsync(new Campaign(
                    startDateTime: NodaTime.SystemClock.Instance.GetOffsetDateTime().Plus(NodaTime.Duration.FromDays(7)),
                    endDateTime: NodaTime.SystemClock.Instance.GetOffsetDateTime().Plus(NodaTime.Duration.FromDays(14)),
                    editionId: 1, name: "test"));
            }

            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var query = new GetCurrentCampaign.Query();
                var result = await mediator.Send(query);

                Assert.True(result.HasNoValue);
            }
        }
    }
}

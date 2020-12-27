using CSharpFunctionalExtensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.Impl;
using Szlem.Test.Helpers;
using Xunit;

namespace Szlem.Recruitment.Tests.Campaigns
{
    public class CreateTests
    {
        private void ConfigureServices(IServiceCollection services)
        {
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
            services.AddSingleton<NodaTime.IClock>(NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01));
        }

        [Fact]
        public async Task CanCreate()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var mediator = sp.GetRequiredService<IMediator>();
            var command = new Create.Command()
            {
                Name = "test",
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 10, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EditionID = 1
            };

            var result = await mediator.Send(command);

            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value.ID);
        }

        [Fact(DisplayName = "Data rozpoczęcia kampanii musi być wcześniejsza od daty jej zakończenia")]
        public async Task CampaignStartDateMustBeEarlierThanEndDate()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var mediator = sp.GetRequiredService<IMediator>();
            var command = new Create.Command()
            {
                Name = "test",
                StartDateTime = new NodaTime.LocalDateTime(2019, 10, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EditionID = 1
            };

            var result = await mediator.Send(command);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            Assert.Collection(error.Failures,
                failure =>
                {
                    Assert.Equal(nameof(command.StartDateTime), failure.PropertyName);
                    Assert.Collection(failure.Errors,
                        single => Assert.Equal(Create.ErrorMessages.StartDateCannotBeGreaterThanEndDate, single));
                }
            );
        }

        [Fact]
        public async Task CampaignCannotStart_BeforeEditionStart()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var mediator = sp.GetRequiredService<IMediator>();
            var command = new Create.Command()
            {
                Name = "test",
                StartDateTime = new NodaTime.LocalDateTime(2019, 08, 15, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 10, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EditionID = 1
            };

            var result = await mediator.Send(command);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            AssertHelpers.SingleError(nameof(command.StartDateTime), Create.ErrorMessages.CampaignCannotStartBeforeEditionStart, error.Failures);
        }

        [Fact]
        public async Task CampaignCannotEnd_AfterEditionEnd()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var mediator = sp.GetRequiredService<IMediator>();
            var command = new Create.Command()
            {
                Name = "test",
                StartDateTime = new NodaTime.LocalDateTime(2020, 06, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2020, 07, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                EditionID = 1
            };

            var result = await mediator.Send(command);

            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.ValidationFailed>(result.Error);
            AssertHelpers.SingleError(nameof(command.EndDateTime), Create.ErrorMessages.CampaignMustEndBeforeEditionEnd, error.Failures);
        }

        [Fact]
        public async Task CampaignsCannotOverlap()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = sp.GetRequiredService<SharedKernel.ISzlemEngine>();
                var command = new Create.Command()
                {
                    Name = "test",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                    EditionID = 1
                };

                var result = await engine.Execute(command);
            }

            using (var scope = sp.CreateScope())
            {
                var engine = sp.GetRequiredService<SharedKernel.ISzlemEngine>();
                var command = new Create.Command()
                {
                    Name = "test",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 01, 12, 00, 00).InMainTimezone().ToOffsetDateTime(),
                    EditionID = 1
                };

                var result = await engine.Execute(command);

                Assert.False(result.IsSuccess);
                var error = Assert.IsType<Error.DomainError>(result.Error);
                Assert.Equal(Create.ErrorMessages.CampaignsCannotOverlap, error.Message);
            }
        }
    }
}

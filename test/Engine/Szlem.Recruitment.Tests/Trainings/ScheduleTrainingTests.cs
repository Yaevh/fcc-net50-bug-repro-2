using CSharpFunctionalExtensions;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Recruitment.Campaigns;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Trainings;
using Szlem.Test.Helpers;
using Xunit;

namespace Szlem.Recruitment.Tests.Trainings
{
    public class ScheduleTrainingTests
    {
        private readonly Guid trainerID = Guid.NewGuid();

        private void ConfigureServices(IServiceCollection services)
        {
            var trainer = new TrainerDetails() { Guid = trainerID, Name = "Jan Paweł II" };
            var trainerProviderMock = new Mock<ITrainerProvider>();
            trainerProviderMock.Setup(x => x.GetTrainerDetails(trainerID)).Returns(Task.FromResult(Maybe<TrainerDetails>.From(trainer)));
            trainerProviderMock.Setup(x => x.GetTrainerDetails(It.IsAny<IReadOnlyCollection<Guid>>())).Returns(Task.FromResult(new[] { trainer } as IReadOnlyCollection<TrainerDetails>));

            ConfigureServicesBase(services);
            services.AddSingleton<NodaTime.IClock>(NodaTime.Testing.FakeClock.FromUtc(2019, 08, 01));
            services.Remove<ITrainerProvider>();
            services.AddSingleton(trainerProviderMock.Object);

            services.AddSingleton(Mock.Of<IUserAccessor>(mock =>
                mock.GetUser() == Task.FromResult(new Models.Users.ApplicationUser() { Id = trainerID, UserName = "Jan Paweł II" }),
                MockBehavior.Strict));
        }

        private void ConfigureServicesBase(IServiceCollection services)
        {
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

            var authorizationProviderMock = new Moq.Mock<Engine.Infrastructure.IRequestAuthorizationAnalyzer>();
            authorizationProviderMock
                .Setup(x => x.Authorize(Moq.It.IsAny<IBaseRequest>()))
                .Returns(Task.FromResult(Microsoft.AspNetCore.Authorization.AuthorizationResult.Success()));
            services.AddSingleton(authorizationProviderMock.Object);
        }

        private async Task CreateCampaign(IServiceProvider sp, Create.Command command)
        {
            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                await mediator.Send(command);
            }
        }


        [Fact(DisplayName = "Valid command passes validation")]
        public async Task Valid_command_passes_validation()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 15, 00),
                    CampaignID = 1
                };

                // act
                var result = await engine.Execute(command);

                Assert.True(result.IsSuccess);
            }
        }

        [Fact(DisplayName = "Training must begin and end on the same day")]
        public async Task Training_must_begin_and_end_on_the_same_day()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 16, 15, 00),
                    CampaignID = 1
                };

                // act
                var result = await engine.Execute(command);

                Assert.False(result.IsSuccess);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                Assert.Collection(error.Failures,
                    first => { Assert.Equal(nameof(command.StartDateTime), first.PropertyName); Assert.Single(first.Errors, ErrorMessages.Training_must_begin_and_end_on_the_same_day); },
                    second => { Assert.Equal(nameof(command.EndDateTime), second.PropertyName); Assert.Single(second.Errors, ErrorMessages.Training_must_begin_and_end_on_the_same_day); }
                );
            }
        }

        [Fact(DisplayName = "ScheduleTraining command produces a scheduled training in the DB")]
        public async Task ScheduleTraining_command_produces_a_scheduled_training()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 15, 00),
                    CampaignID = 1,
                    Notes = "notatka testowa"
                };

                // act
                var result = await engine.Execute(command);

                Assert.True(result.IsSuccess);
                Assert.Equal(1, result.Value.ID);
            }

            // assert
            using (var scope = sp.CreateScope())
            {
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var result = await mediator.Send(new Recruitment.Trainings.Details.Query() { TrainingId = 1 });

                Assert.True(result.HasValue);
                var training = result.Value;

                Assert.Equal(new NodaTime.LocalDateTime(2019, 10, 15, 10, 00).InMainTimezone().ToOffsetDateTime(), training.Start);
                Assert.Equal(new NodaTime.LocalDateTime(2019, 10, 15, 15, 00).InMainTimezone().ToOffsetDateTime(), training.End);
                Assert.Equal("Watykan", training.City);
                Assert.Equal("Papieska 21/37", training.Address);
                Assert.Equal(trainerID, training.CoordinatorId);
                Assert.Equal("Jan Paweł II", training.CoordinatorName);
                var note = Assert.Single(training.Notes);
                Assert.Equal("notatka testowa", note.Content);
                Assert.Equal(new NodaTime.LocalDate(2019, 08, 01), note.Timestamp.Date);
                Assert.Equal("Jan Paweł II", note.AuthorName);
            }
        }

        [Fact(DisplayName = "ScheduleTraining command must have address, start date, end date")]
        public async Task TrainingMustHaveAddressStartEndDateAndCoordinator()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    CampaignID = 1
                };

                // act
                var result = await engine.Execute(command);

                Assert.False(result.IsSuccess);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                Assert.True(error.Failures.ContainsProperty(nameof(ScheduleTraining.Command.StartDateTime)));
                Assert.True(error.Failures.ContainsProperty(nameof(ScheduleTraining.Command.EndDateTime)));
                Assert.True(error.Failures.ContainsProperty(nameof(ScheduleTraining.Command.City)));
                Assert.True(error.Failures.ContainsProperty(nameof(ScheduleTraining.Command.Address)));
            }
        }

        [Fact(DisplayName = "ScheduleTraining command must specify a campaign")]
        public async Task TrainingCommandMustSpecifyCampaign()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 15, 00),
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                var failure = error.Failures.Should().ContainSingle().Subject;
                failure.PropertyName.Should().Be(nameof(ScheduleTraining.Command.CampaignID));
                failure.Errors.Should().ContainSingle()
                    .Which.Should().Be("'Campaign ID' must not be empty.");
            }
        }

        [Fact(DisplayName = "ScheduleTraining command must specify an existing campaign")]
        public async Task TrainingMustBelongToExistingCampaign()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 15, 00),
                    CampaignID = 2137
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
                Assert.Equal($"Campaign with ID={command.CampaignID} not found", error.Message);
            }
        }

        [Fact(DisplayName = "Scheduled training's StartDateTime must be earlier than its EndDateTime")]
        public async Task TrainingStartDateMustBeEarlierThanItsEndDate()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            var createCampaignCommand = new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            };
            await CreateCampaign(sp, createCampaignCommand);

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 15, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    CampaignID = 1
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                AssertHelpers.SingleError(nameof(ScheduleTraining.Command.StartDateTime), ErrorMessages.StartDateTimeCannotBeGreaterThanEndDateTime, error.Failures);
            }
        }

        [Fact(DisplayName = "Training must occur after campaign end")]
        public async Task TrainingMustBeScheduledAfterCampaignEnd()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 09, 15, 15, 00),
                    CampaignID = 1
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.DomainError>(result.Error);
                Assert.Equal(ErrorMessages.ScheduledTrainingMustOccurAfterCampaignEnd, error.Message);
            }
        }

        [Fact(DisplayName = "Training date cannot occur after the campaign's edition end")]
        public async Task TrainingMustBeScheduledDuringEditionDuration()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2020, 09, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2020, 09, 15, 15, 00),
                    CampaignID = 1
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.DomainError>(result.Error);
                Assert.Equal(ErrorMessages.CannotScheduleTrainingAfterEditionEnd, error.Message);
            }
        }

        [Fact(DisplayName = "Training date cannot be scheduled in the past")]
        public async Task TrainingCannotBeScheduledInThePast()
        {
            var clock = NodaTime.Testing.FakeClock.FromUtc(2019, 08, 30);
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services =>
            {
                ConfigureServices(services);
                services.AddSingleton<NodaTime.IClock>(clock);
            });
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                clock.AdvanceDays(20);
                Assert.Equal(new NodaTime.LocalDate(2019, 09, 19), clock.GetCurrentInstant().InUtc().Date);
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 09, 15, 15, 00),
                    CampaignID = 1
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.DomainError>(result.Error);
                Assert.Equal(ErrorMessages.CannotScheduleTrainingInThePast, error.Message);
            }
        }

        [Fact(DisplayName = "Training cannot be scheduled after campaign start")]
        public async Task TrainingCannotBeAddedAfterCampaignStart()
        {
            var clock = NodaTime.Testing.FakeClock.FromUtc(2019, 08, 30);
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services =>
            {
                ConfigureServices(services);
                //services.Remove<NodaTime.IClock>();
                services.AddSingleton<NodaTime.IClock>(clock);
            });
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                clock.AdvanceDays(3);
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 09, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 09, 15, 15, 00),
                    CampaignID = 1
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.DomainError>(result.Error);
                Assert.Equal(ErrorMessages.CannotScheduleTrainingAfterCampaignStart, error.Message);
            }
        }

        [Fact(DisplayName = "Jeśli szkolenie zawiera notatkę, ta notatka nie może być pusta")]
        public async Task If_scheduled_training_contains_a_note__that_note_cannot_be_empty()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            await CreateCampaign(sp, new Create.Command()
            {
                Name = "test",
                EditionID = 1,
                StartDateTime = new NodaTime.LocalDateTime(2019, 09, 01, 12, 00).InMainTimezone().ToOffsetDateTime(),
                EndDateTime = new NodaTime.LocalDateTime(2019, 09, 30, 12, 00).InMainTimezone().ToOffsetDateTime()
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<Szlem.SharedKernel.ISzlemEngine>();
                var command = new ScheduleTraining.Command
                {
                    City = "Watykan",
                    Address = "Papieska 21/37",
                    StartDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 10, 00),
                    EndDateTime = new NodaTime.LocalDateTime(2019, 10, 15, 15, 00),
                    CampaignID = 1,
                    Notes = "   "
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.DomainError>(result.Error);
                Assert.Equal(ErrorMessages.NoteCannotBeEmpty, error.Message);
            }
        }
    }
}

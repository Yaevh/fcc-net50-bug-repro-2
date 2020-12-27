using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Org.BouncyCastle.Crypto.Prng;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.Models.Users;
using Szlem.Recruitment.DependentServices;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class GetEnrollmentDetailsTests
    {
        private readonly Guid trainerID = Guid.NewGuid();
        
        private class MockTrainerProvider : ITrainerProvider
        {
            private readonly IReadOnlyDictionary<Guid, TrainerDetails> _trainers;

            public MockTrainerProvider(IReadOnlyCollection<TrainerDetails> trainers)
            {
                _trainers = trainers.ToDictionary(x => x.Guid, x => x);
            }

            public Task<Maybe<TrainerDetails>> GetTrainerDetails(Guid guid)
            {
                if (_trainers.ContainsKey(guid))
                    return Task.FromResult(Maybe<TrainerDetails>.From(_trainers[guid]));
                else
                    return Task.FromResult(Maybe<TrainerDetails>.None);
            }

            public async Task<IReadOnlyCollection<TrainerDetails>> GetTrainerDetails(IReadOnlyCollection<Guid> guids)
            {
                var trainerMaybes = await guids.Distinct().SelectAsync(async guid => await GetTrainerDetails(guid));
                return trainerMaybes.Where(x => x.HasValue).Select(x => x.Value).ToArray();
            }
        }

        private static NodaTime.OffsetDateTime CreateOffsetDateTime(int daysOffset)
        {
            return NodaTime.LocalDateTime.FromDateTime(DateTime.Now.AddDays(daysOffset)).InMainTimezone().ToOffsetDateTime();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var trainerProvider = new MockTrainerProvider(new[] { new TrainerDetails() { Guid = trainerID, Name = "Jan Paweł II" } });

            var campaign = new Impl.Entities.Campaign(CreateOffsetDateTime(-7), CreateOffsetDateTime(+7), 1, "kampania testowa");
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Impl.Entities.Training(
                address: "Papieska 21/37",
                city: "Wadowice",
                startDateTime: CreateOffsetDateTime(14),
                endDateTime: CreateOffsetDateTime(15),
                coordinatorId: trainerID);
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);

            var training2 = new Impl.Entities.Training(
                address: "Papieska 21/37",
                city: "Wadowice",
                startDateTime: CreateOffsetDateTime(45),
                endDateTime: CreateOffsetDateTime(46),
                coordinatorId: trainerID);
            training2.GetType().GetProperty(nameof(training.ID)).SetValue(training2, 2);
            campaign.ScheduleTraining(training2);

            var campaignRepoMock = new Mock<Impl.Repositories.ICampaignRepository>();
            campaignRepoMock.Setup(repo => repo.GetById(1)).ReturnsAsync(campaign);
            campaignRepoMock.Setup(repo => repo.GetAll()).ReturnsAsync(new[] { campaign });

            var trainingRepoMock = new Mock<Impl.Repositories.ITrainingRepository>();
            trainingRepoMock
                .Setup(repo => repo.GetByIds(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync(
                    (IReadOnlyCollection<int> query) => new[] { training, training2 }.Where(y => query.Contains(y.ID)).ToArray()
                );

            var userAccessor = Mock.Of<IUserAccessor>(ua => ua.GetUser() == Task.FromResult(new ApplicationUser() { }));
            var authService = Mock.Of<IAuthorizationService>(service =>
                service.AuthorizeAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>())
                    == Task.FromResult(AuthorizationResult.Success()));

            var userStore = Mock.Of<IUserStore<ApplicationUser>>(
                um => um.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()) == Task.FromResult(new ApplicationUser()), MockBehavior.Strict);

            services.Remove<Impl.Repositories.ICampaignRepository>();
            services.Remove<Impl.Repositories.ITrainingRepository>();
            services.Remove<ITrainerProvider>();
            services.AddSingleton<Impl.Repositories.ICampaignRepository>(campaignRepoMock.Object);
            services.AddSingleton<Impl.Repositories.ITrainingRepository>(sp => trainingRepoMock.Object);
            services.AddSingleton<ITrainerProvider>(trainerProvider);
            services.AddSingleton(userAccessor);
            services.AddSingleton(authService);
            services.AddSingleton(new UserManager<ApplicationUser>(userStore, null, null, null, null, null, null, null, null));
        }

        [Fact]
        public async Task Querying_for_nonexistent_enrollment_returns_not_found_failure()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var result = await engine.Query(new GetEnrollmentDetails.QueryByEnrollmentId() { EnrollmentID = Guid.Empty });

                Assert.False(result.IsSuccess);
                var error = Assert.IsType<Szlem.Domain.Error.ResourceNotFound>(result.Error);
            }
        }

        [Fact]
        public async Task Querying_for_existing_enrollment_returns_that_enrollment_summary()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command() {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wadowice" },
                    PreferredTrainingIds = new[] { 1, 2 },
                    GdprConsentGiven = true
                };
                var result = await engine.Execute(command);
                Assert.True(result.IsSuccess);
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var enrollments = await engine.Query(new GetSubmissions.Query());
                var result = await engine.Query(new GetEnrollmentDetails.QueryByEnrollmentId() { EnrollmentID = enrollments.Single().Id });

                Assert.True(result.IsSuccess);
                var enrollment = result.Value;
                Assert.Equal(enrollments.Single().Id, enrollment.ID);
                Assert.Equal("Andrzej", enrollment.FirstName);
                Assert.Equal("Strzelba", enrollment.LastName);
                Assert.Equal(EmailAddress.Parse("andrzej@strzelba.com"), enrollment.Email);
                Assert.Equal(Consts.FakePhoneNumber, enrollment.PhoneNumber);
                Assert.Single(enrollment.PreferredLecturingCities, "Wadowice");

                Assert.Collection(enrollment.PreferredTrainings,
                    first => {
                        Assert.Equal(1, first.ID);
                        Assert.Equal(trainerID, first.CoordinatorID);
                        Assert.Equal("Jan Paweł II", first.CoordinatorName);
                        Assert.Equal("Wadowice", first.City);
                    },
                    second => {
                        Assert.Equal(2, second.ID);
                        Assert.Equal(trainerID, second.CoordinatorID);
                        Assert.Equal("Jan Paweł II", second.CoordinatorName);
                        Assert.Equal("Wadowice", second.City);
                    });

                enrollment.IsCurrentSubmission.Should().BeTrue();
                enrollment.IsOldSubmission.Should().BeFalse();
                enrollment.CanRecordTrainingResults.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Querying_for_existing_enrollment_returns_that_enrollment_events()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command() {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wadowice" },
                    PreferredTrainingIds = new[] { 1 },
                    GdprConsentGiven = true
                };
                var result = await engine.Execute(command);
                Assert.True(result.IsSuccess);
            }

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var enrollments = await engine.Query(new GetSubmissions.Query());
                var result = await engine.Query(new GetEnrollmentDetails.QueryByEnrollmentId() { EnrollmentID = enrollments.Single().Id });

                Assert.True(result.IsSuccess);
                var enrollment = result.Value;
                Assert.Contains(enrollment.Events, evt => evt.GetType() == typeof(GetEnrollmentDetails.RecruitmentFormSubmittedEventData));
            }
        }
    }
}

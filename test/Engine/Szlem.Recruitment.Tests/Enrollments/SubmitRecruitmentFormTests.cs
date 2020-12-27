using CSharpFunctionalExtensions;
using EventFlow;
using EventFlow.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.Engine.Infrastructure;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.Recruitment.Impl.Enrollments.Events;
using Szlem.Recruitment.Impl.Entities;
using Szlem.SharedKernel;
using Szlem.Test.Helpers;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class SubmitRecruitmentFormTests
    {
        private OffsetDateTime CreateOffsetDateTimeDaysInTheFuture(int daysOffset)
        {
            return LocalDateTime.FromDateTime(DateTime.Now.AddDays(daysOffset)).InZoneLeniently(Consts.MainTimezone).ToOffsetDateTime();
        }

        private Training BuildScheduledTraining(int id, OffsetDateTime startDateTime, OffsetDateTime endDateTime)
        {
            var training = new Impl.Entities.Training(
                address: "Papieska 21/37",
                city: "Wadowice",
                startDateTime: startDateTime,
                endDateTime: endDateTime,
                coordinatorId: Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, id);
            return training;
        }

        private void ConfigureServices(IServiceCollection services)
        {
            var campaign = new Impl.Entities.Campaign(CreateOffsetDateTimeDaysInTheFuture(-7), CreateOffsetDateTimeDaysInTheFuture(+7), 1, "kampania testowa");
            var training = BuildScheduledTraining(1, CreateOffsetDateTimeDaysInTheFuture(14), CreateOffsetDateTimeDaysInTheFuture(15));
            campaign.ScheduleTraining(training);

            var campaign2 = new Impl.Entities.Campaign(CreateOffsetDateTimeDaysInTheFuture(30), CreateOffsetDateTimeDaysInTheFuture(60), 1, "kampania testowa");
            var training2 = BuildScheduledTraining(2, CreateOffsetDateTimeDaysInTheFuture(45), CreateOffsetDateTimeDaysInTheFuture(46));
            campaign2.ScheduleTraining(training);

            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            campaign2.GetType().GetProperty(nameof(campaign2.Id)).SetValue(campaign2, 2);

            var campaignRepoMock = new Mock<Impl.Repositories.ICampaignRepository>();
            campaignRepoMock.Setup(repo => repo.GetById(1)).ReturnsAsync(campaign);
            campaignRepoMock.Setup(repo => repo.GetById(2)).ReturnsAsync(campaign2);
            campaignRepoMock.Setup(repo => repo.GetAll()).ReturnsAsync(new[] { campaign, campaign2 });

            var trainingRepoMock = new Mock<Impl.Repositories.ITrainingRepository>();
            trainingRepoMock
                .Setup(repo => repo.GetByIds(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync(
                    (IReadOnlyCollection<int> query) => new[] { training, training2 }.Where(y => query.Contains(y.ID)).ToArray()
                );

            services.Remove<Impl.Repositories.ICampaignRepository>();
            services.Remove<Impl.Repositories.ITrainingRepository>();
            services.AddSingleton<Impl.Repositories.ICampaignRepository>(campaignRepoMock.Object);
            services.AddSingleton<Impl.Repositories.ITrainingRepository>(sp => trainingRepoMock.Object);
        }


        [Fact(DisplayName = "Poprawna komenda wykonuje się poprawnie")]
        public async Task Valid_command_succeeds()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
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
        }

        [Fact(DisplayName = "Poprawna komenda powoduje wyemitowanie eventu RecruitmentFormSubmitted")]
        public async Task Valid_command_emits_single_RecruitmentFormSubmitted_event()
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
                    Region = "małopolskie",
                    PreferredLecturingCities = new[] { "Wadowice" },
                    PreferredTrainingIds = new[] { 1 },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsSuccess);
                var eventStore = scope.ServiceProvider.GetRequiredService<EventFlow.EventStores.IEventStore>();
                var events = await eventStore.LoadAllEventsAsync(EventFlow.EventStores.GlobalPosition.Start, 2, System.Threading.CancellationToken.None);
                var domainEvent = Assert.Single(events.DomainEvents, ev => ev.EventType == typeof(RecruitmentFormSubmitted));
                var e = Assert.IsType<RecruitmentFormSubmitted>(domainEvent.GetAggregateEvent());
                Assert.Equal("Andrzej", e.FirstName);
                Assert.Equal("Strzelba", e.LastName);
                Assert.Equal("andrzej@strzelba.com", e.Email.ToString());
                Assert.Equal(Consts.FakePhoneNumber, e.PhoneNumber);
                Assert.Equal("ala ma kota", e.AboutMe);
                Assert.Equal("małopolskie", e.Region);
                Assert.Single(e.PreferredLecturingCities, "Wadowice");
                Assert.Single(e.PreferredTrainingIds, 1);
                Assert.True(e.GdprConsentGiven);
            }
        }

        [Fact(DisplayName = "Jeśli EmailService zdoła wysłać maila, po zarejestrowaniu agregat zawiera event EmailSent")]
        public async Task Valid_command_emits_single_EmailSent_event_when_EmailService_sends_email()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
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
                var eventStore = scope.ServiceProvider.GetRequiredService<EventFlow.EventStores.IEventStore>();
                var events = await eventStore.LoadAllEventsAsync(EventFlow.EventStores.GlobalPosition.Start, 2, System.Threading.CancellationToken.None);
                var domainEvent = Assert.Single(events.DomainEvents, ev => ev.EventType == typeof(EmailSent));
                var ev = Assert.IsType<EmailSent>(domainEvent.GetAggregateEvent());
                Assert.Equal(SubmitRecruitmentFormHandler.MessageTitle, ev.Subject);
            }
        }

        [Fact(DisplayName = "Jeśli EmailService nie zdoła wysłać maila, po zarejestrowaniu agregat zawiera event EmailSendingFailed")]
        public async Task Valid_command_emits_single_EmailSendingFailed_event_when_EmailService_fails_to_send_email()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(services =>
            {
                ConfigureServices(services);

                var failingEmailMock = new Mock<IEmailService>();
                failingEmailMock
                    .Setup(x => x.CreateMessage(It.IsAny<EmailAddress>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<IEnumerable<EmailAttachment>>()))
                    .Returns((EmailAddress email, string subject, string body, bool isHtml, IEnumerable<EmailAttachment> attachments) => new SucceedingEmailService().CreateMessage(email, subject, body, isHtml, attachments));
                failingEmailMock.Setup(x => x.Send(It.IsAny<EmailMessage>(), CancellationToken.None)).Returns(Task.FromResult(Result.Failure("error")));

                services.Remove<IEmailService>();
                services.AddSingleton(failingEmailMock.Object);
            });

            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "woj. małopolskie",
                    PreferredLecturingCities = new[] { "Wadowice" },
                    PreferredTrainingIds = new[] { 1 },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsSuccess);
                var eventStore = scope.ServiceProvider.GetRequiredService<EventFlow.EventStores.IEventStore>();
                var events = await eventStore.LoadAllEventsAsync(EventFlow.EventStores.GlobalPosition.Start, 2, CancellationToken.None);
                var domainEvent = Assert.Single(events.DomainEvents, ev => ev.EventType == typeof(EmailSendingFailed));
                var ev = Assert.IsType<EmailSendingFailed>(domainEvent.GetAggregateEvent());
                Assert.Equal(SubmitRecruitmentFormHandler.MessageTitle, ev.Subject);
            }
        }

        [Fact(DisplayName = "Komenda musi zawierać: datę zgłoszenia, imię, nazwisko, email, telefon, o mnie")]
        public async Task Command_MustContain_SubmissionDate_FirstName_LastName_EmailAddress_PhoneNumber_AboutMe()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    PreferredTrainingIds = new[] { 1 }, GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                Assert.Collection(error.Failures,
                    error => AssertHelpers.SingleError(nameof(command.FirstName), SubmitRecruitmentForm_ErrorMessages.FirstNameIsRequired, error),
                    error => AssertHelpers.SingleError(nameof(command.LastName), SubmitRecruitmentForm_ErrorMessages.LastNameIsRequired, error),
                    error => AssertHelpers.SingleError(nameof(command.Email), SubmitRecruitmentForm_ErrorMessages.EmailIsRequired, error),
                    error => AssertHelpers.SingleError(nameof(command.PhoneNumber), SubmitRecruitmentForm_ErrorMessages.PhoneNumberIsRequired, error),
                    error => AssertHelpers.SingleError(nameof(command.AboutMe), SubmitRecruitmentForm_ErrorMessages.AboutMeIsRequired, error),
                    error => AssertHelpers.SingleError(nameof(command.Region), SubmitRecruitmentForm_ErrorMessages.RegionIsRequired, error),
                    error => AssertHelpers.SingleError(nameof(command.PreferredLecturingCities), SubmitRecruitmentForm_ErrorMessages.PreferredLecturingCities_must_be_specified, error)
                );
            }
        }

        [Fact(DisplayName = "Komenda musi zawierać zgodę RODO")]
        public async Task Command_must_contain_GDPR_consent()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk" },
                    PreferredTrainingIds = new[] { 1 },
                    GdprConsentGiven = false
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                AssertHelpers.SingleError(nameof(command.GdprConsentGiven), SubmitRecruitmentForm_ErrorMessages.GdprConsentIsRequired, error.Failures);
            }
        }

        [Fact(DisplayName = "Komenda musi wskazywać na co najmniej jedno szkolenie")]
        public async Task Command_must_specify_at_least_one_training()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk" },
                    PreferredTrainingIds = Array.Empty<int>(),
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ValidationFailed>(result.Error);
                AssertHelpers.SingleError(nameof(command.PreferredTrainingIds), SubmitRecruitmentForm_ErrorMessages.PreferredTrainingsMustBeSpecified, error.Failures);
            }
        }

        [Fact(DisplayName = "Wszystkie wskazane przez komendę szkolenia muszą istnieć")]
        public async Task All_specified_trainings_must_exist()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk" },
                    PreferredTrainingIds = new[] { 1, 21, 37 },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Error.ResourceNotFound>(result.Error);
                Assert.Equal(SubmitRecruitmentForm_ErrorMessages.SomeTrainingsWereNotFound, error.Message);
            }
        }

        [Fact(DisplayName = "Wszystkie wskazane przez komendę szkolenia muszą odbywać się w przyszłości")]
        public void All_specified_trainings_must_be_held_in_the_future()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var selectedTraining = BuildScheduledTraining(1, CreateOffsetDateTimeDaysInTheFuture(-3), CreateOffsetDateTimeDaysInTheFuture(-2));
            selectedTraining.Campaign = new Campaign(CreateOffsetDateTimeDaysInTheFuture(-7), CreateOffsetDateTimeDaysInTheFuture(+7), 1, "kampania testowa");
            selectedTraining.Campaign.GetType().GetProperty(nameof(selectedTraining.Campaign.Id)).SetValue(selectedTraining.Campaign, 1);

            var enrollment = new EnrollmentAggregate(id);

            // Act
            var result = enrollment.SubmitRecruitmentForm(new SubmitRecruitmentForm.Command() {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wadowice" },
                    PreferredTrainingIds = new[] { 1 },
                    GdprConsentGiven = true
                },
                new[] { selectedTraining },
                NodaTime.SystemClock.Instance.GetCurrentInstant());

            // Assert
            Assert.False(result.IsSuccess);
            var error = Assert.IsType<Error.DomainError>(result.Error);
            Assert.Equal(SubmitRecruitmentForm_ErrorMessages.PreferredTrainingsMustOccurInTheFuture, error.Message);
        }

        [Fact(DisplayName = "Komenda nie może zawierać zduplikowanych preferowanych szkoleń")]
        public async Task Command_cannot_specify_duplicate_preferred_trainings()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk" },
                    PreferredTrainingIds = new[] { 1, 1, 2, 3, 5, 8, 13 },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Domain.Error.ValidationFailed>(result.Error);
                var failure = Assert.Single(error.Failures);
                Assert.Equal(nameof(command.PreferredTrainingIds), failure.PropertyName);
                Assert.Single(failure.Errors);
                Assert.Equal(SubmitRecruitmentForm_ErrorMessages.DuplicatePreferredTrainingsWereSpecified, failure.Errors.Single());
            }
        }

        [Fact(DisplayName = "Wszystkie szkolenia w komendzie muszą należeć do tej samej kampanii")]
        public async Task All_tranings_in_command_must_belong_to_the_same_campaign()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk" },
                    PreferredTrainingIds = new[] { 1, 2 },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Domain.Error.DomainError>(result.Error);
                Assert.Equal(SubmitRecruitmentForm_ErrorMessages.PreferredTrainingsMustBelongToTheSameCampaign, error.Message);
            }
        }

        [Fact(DisplayName = "Komenda może zostać wydana tylko w trakcie kampanii do której należy szkolenie na którą wskazuje")]
        public async Task Command_must_be_issued_during_campaign()
        {
            var clock = new NodaTime.Testing.FakeClock(NodaTime.Instant.FromDateTimeUtc(DateTime.UtcNow).Plus(NodaTime.Duration.FromDays(30)));

            var sp = new ServiceProviderBuilder().BuildServiceProvider(services => {
                ConfigureServices(services);
                services.Remove<NodaTime.IClock>();
                services.AddSingleton<NodaTime.IClock>(clock);
            });
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredTrainingIds = new[] { 1 },
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk" },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Domain.Error.DomainError>(result.Error);
                Assert.Equal(SubmitRecruitmentForm_ErrorMessages.SubmissionMustOccurDuringCampaign, error.Message);
            }
        }

        [Fact(DisplayName = "Komenda nie może zawierać zduplikowanych preferowanych miast")]
        public async Task Command_cannot_specify_duplicate_preferred_cities()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wolne Miasto Gdańsk", "Wolne Miasto Gdańsk" },
                    PreferredTrainingIds = new[] { 1, 2, 3, 5, 8, 13 },
                    GdprConsentGiven = true
                };

                var result = await engine.Execute(command);

                Assert.True(result.IsFailure);
                var error = Assert.IsType<Domain.Error.ValidationFailed>(result.Error);
                var failure = Assert.Single(error.Failures);
                Assert.Equal(nameof(command.PreferredLecturingCities), failure.PropertyName);
                Assert.Single(failure.Errors);
                Assert.Equal(SubmitRecruitmentForm_ErrorMessages.PreferredLecturingCities_cannot_have_duplicates, failure.Errors.Single());
            }
        }

        [Fact(DisplayName = "Po wykonaniu komendy agregat zawiera: imię, nazwisko, email, telefon, region, miasta, preferowane szkolenia, ID kampanii")]
        public void After_executing_command__aggregate_contains_name_email_phone_region_cities_trainings_and_campaign()
        {
            // Arrange
            var now = NodaTime.SystemClock.Instance.GetOffsetDateTime();
            var command = new SubmitRecruitmentForm.Command() {
                FirstName = "Andrzej",
                LastName = "Strzelba",
                Email = EmailAddress.Parse("andrzej@strzelba.com"),
                PhoneNumber = Consts.FakePhoneNumber,
                AboutMe = "ala ma kota",
                Region = "małopolskie",
                PreferredLecturingCities = new[] { "Wadowice" },
                PreferredTrainingIds = new[] { 1 },
                GdprConsentGiven = true
            };
            var enrollment = new EnrollmentAggregate(EnrollmentAggregate.EnrollmentId.New);
            var campaign = new Campaign(
                startDateTime: now.Minus(Duration.FromDays(3)),
                endDateTime: now.Plus(Duration.FromDays(3)),
                editionId: 1, name: "kampania testowa");
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Training(
                address: "Papieska 21/37",
                city: "Wadowice",
                startDateTime: now.Plus(Duration.FromDays(7)),
                endDateTime: now.Plus(Duration.FromDays(7) + Duration.FromHours(4)),
                coordinatorId: Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            var trainings = new[] { training };

            // Act
            var result = enrollment.SubmitRecruitmentForm(command, trainings, now.ToInstant());

            // Assert
            result.IsSuccess.Should().BeTrue();
            enrollment.FirstName.Should().Be("Andrzej");
            enrollment.LastName.Should().Be("Strzelba");
            enrollment.FullName.Should().Be("Andrzej Strzelba");
            enrollment.Email.ToString().Should().Be("andrzej@strzelba.com");
            enrollment.PhoneNumber.Should().Be(Consts.FakePhoneNumber);

            enrollment.Region.Should().Be("małopolskie");
            enrollment.PreferredLecturingCities.Should().BeEquivalentTo(new[] { "Wadowice" });
            enrollment.PreferredTrainingIds.Should().BeEquivalentTo(new[] { 1 });
            enrollment.CampaignId.Should().Be(1);
        }

        [Fact(DisplayName = "Po wykonaniu komendy lista zgłoszeń zawiera dodane zgłoszenie")]
        public async Task After_executing_command_enrollments_contain_that_submission()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider(ConfigureServices);
            using (var scope = sp.CreateScope())
            {
                var engine = scope.ServiceProvider.GetRequiredService<ISzlemEngine>();
                var command = new SubmitRecruitmentForm.Command()
                {
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
                var results = await engine.Query(new GetSubmissions.Query());

                var readModel = Assert.Single(results);
                var id = EnrollmentAggregate.EnrollmentId.With(readModel.Id);
                Assert.Equal("Andrzej", readModel.FirstName);
                Assert.Equal("Strzelba", readModel.LastName);
                Assert.Equal(EmailAddress.Parse("andrzej@strzelba.com"), readModel.Email);
                Assert.Equal(Consts.FakePhoneNumber, readModel.PhoneNumber);
                Assert.Equal("ala ma kota", readModel.AboutMe);
                Assert.Equal(1, readModel.Campaign.ID);
                Assert.Equal("Wolne Miasto Gdańsk", readModel.Region);
                var preferredTraining = Assert.Single(readModel.PreferredTrainings);
                Assert.Equal(1, preferredTraining.ID);
                Assert.Single(readModel.PreferredLecturingCities, "Wadowice");
            }
        }

        [Fact(DisplayName = "Po wypełnieniu formularza zgłoszeniowego kandydat może zostać zaproszony na szkolenie")]
        public void After_submitting_recruitment_form_candidate_can_be_invited_to_training()
        {
            // Arrange
            var id = EnrollmentAggregate.EnrollmentId.New;
            var selectedTraining = BuildScheduledTraining(1, CreateOffsetDateTimeDaysInTheFuture(14), CreateOffsetDateTimeDaysInTheFuture(15));
            selectedTraining.Campaign = new Campaign(CreateOffsetDateTimeDaysInTheFuture(-7), CreateOffsetDateTimeDaysInTheFuture(+7), 1, "kampania testowa");
            selectedTraining.Campaign.GetType().GetProperty(nameof(selectedTraining.Campaign.Id)).SetValue(selectedTraining.Campaign, 1);

            var now = NodaTime.SystemClock.Instance.GetCurrentInstant();
            var enrollment = new EnrollmentAggregate(id);

            // Act
            var result = enrollment.SubmitRecruitmentForm(new SubmitRecruitmentForm.Command() {
                    FirstName = "Andrzej",
                    LastName = "Strzelba",
                    Email = EmailAddress.Parse("andrzej@strzelba.com"),
                    PhoneNumber = Consts.FakePhoneNumber,
                    AboutMe = "ala ma kota",
                    Region = "Wolne Miasto Gdańsk",
                    PreferredLecturingCities = new[] { "Wadowice" },
                    PreferredTrainingIds = new[] { 1 },
                    GdprConsentGiven = true
                },
                new[] { selectedTraining },
                now);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.True(enrollment.CanAcceptTrainingInvitation(new[] { selectedTraining }, now).IsSuccess);
        }
    }
}

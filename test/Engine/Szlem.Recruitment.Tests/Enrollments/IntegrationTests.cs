using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NodaTime;
using NodaTime.Serialization.JsonNet;
using NodaTime.Serialization.SystemTextJson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.AspNetCore.WebApi.Tests;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.Recruitment.Enrollments;
using Szlem.Recruitment.Impl.Entities;
using Szlem.Recruitment.Impl.Repositories;
using Xunit;

namespace Szlem.Recruitment.Tests.Enrollments
{
    public class IntegrationTests : IClassFixture<Szlem.IntegrationTestsInfrastructure.TestFixture>
    {
        private readonly Instant _now;
        private Duration aWeek => Duration.FromDays(7);
        private Duration aDay => Duration.FromDays(1);
        private Duration anHour => Duration.FromHours(1);
        private OffsetDateTime FromNow(Duration duration) => (_now + duration).InMainTimezone().ToOffsetDateTime();


        private readonly Szlem.IntegrationTestsInfrastructure.TestFixture _fixture;
        private readonly Xunit.Abstractions.ITestOutputHelper _outputHelper;

        public IntegrationTests(Szlem.IntegrationTestsInfrastructure.TestFixture fixture, Xunit.Abstractions.ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper ?? throw new ArgumentNullException(nameof(outputHelper));
            _fixture = fixture
                .ConfigureOutput(outputHelper)
                .ConfigureServices(ConfigureServices)
                .ConfigureInitialization(Initialize)
                .BeforeEachTest(BeforeTest);
            _now = _fixture.TestStart;
        }

        #region test setup
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<EventFlow.EventStores.IEventPersistence, EventFlow.EventStores.InMemory.InMemoryEventPersistence>();

            var dbName = $"{this.GetType().FullName}-{Guid.NewGuid()}";
            services.RemoveAll<Persistence.EF.AppDbContext>();
            services.RemoveAll<DbContextOptions<Persistence.EF.AppDbContext>>();
            services.AddDbContext<Persistence.EF.AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            var campaign = new Campaign(FromNow(-aDay), FromNow(aDay), 1, "kampania testowa");
            campaign.GetType().GetProperty(nameof(campaign.Id)).SetValue(campaign, 1);
            var training = new Training("Papieska 21/37", "Wadowice", FromNow(aDay), FromNow(aDay + anHour), Guid.NewGuid());
            training.GetType().GetProperty(nameof(training.ID)).SetValue(training, 1);
            campaign.ScheduleTraining(training);
            
            var trainingRepository = Mock.Of<ITrainingRepository>(repo =>
                    repo.GetById(1) == Task.FromResult(Maybe<Training>.From(training))
                    && repo.GetByIds(new[] { 1 }) == Task.FromResult(new[] { training } as IReadOnlyCollection<Training>),
                MockBehavior.Strict);
            services.AddSingleton(trainingRepository);

            var campaignRepository = Mock.Of<ICampaignRepository>(repo =>
                    repo.GetById(1) == Task.FromResult(campaign) && repo.GetAll() == Task.FromResult(new[] { campaign } as IReadOnlyCollection<Campaign>),
                MockBehavior.Strict);
            services.AddSingleton(campaignRepository);

            services.Remove<IClock>();
            services.AddSingleton<IClock>(new NodaTime.Testing.FakeClock(_fixture.TestStart));
        }

        private async Task Initialize(IServiceProvider serviceProvider)
        {
            var user = new Models.Users.ApplicationUser { UserName = "admin", Email = "admin@test.com", EmailConfirmed = true };
            var userManager = serviceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Models.Users.ApplicationUser>>();
            await userManager.CreateAsync(user, "test");
            await userManager.AddToRoleAsync(user, SharedKernel.AuthorizationRoles.Admin);
        }

        private Task BeforeTest(IServiceProvider services)
        {
            (services.GetRequiredService<IClock>() as NodaTime.Testing.FakeClock).Reset(_fixture.TestStart);
            return Task.CompletedTask;

            //var eventPersistence = services.GetRequiredService<EventFlow.EventStores.IEventPersistence>();
            //var events = await eventPersistence.LoadAllCommittedEvents(EventFlow.EventStores.GlobalPosition.Start, 100, CancellationToken.None);
            //foreach (var id in events.CommittedDomainEvents.GroupBy(x => x.AggregateId))
            //    eventPersistence.DeleteEventsAsync
        }
        #endregion



        [Fact(DisplayName = "Scenariusz 1: kandydat wypełnia zgłoszenie, otrzymuje zaproszenie na szkolenie i je odrzuca")]
        public async Task Candidate_can_submit_recruitment_form_and_refuse_training_invitation()
        {
            using var client = await _fixture.BuildClient();
            
            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej1@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej1@strzelba.com")));

                enrollmentId = submission.Id;

                Assert.Equal("Andrzej Strzelba", submission.FullName);
                Assert.Equal("Andrzej", submission.FirstName);
                Assert.Equal("Strzelba", submission.LastName);
                Assert.Equal("andrzej1@strzelba.com", submission.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, submission.PhoneNumber);
                Assert.Equal("pomorskie", submission.Region);
                Assert.Equal("Gdańsk", Assert.Single(submission.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(submission.PreferredTrainings).City);
                Assert.Equal("kampania testowa", submission.Campaign.Name);
                Unauthorize(client);
            }

            { // record training invitation refusal
                await AuthorizeAsAdmin(client);
                var trainingInvitationRefusalResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordRefusedTrainingInvitation,
                    new RecordRefusedTrainingInvitation.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.OutgoingPhone,
                        RefusalReason = "kandydatowi nie pasuje termin",
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingInvitationRefusalResult);
                Assert.True(trainingInvitationRefusalResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingInvitationRefusalResult.StatusCode);
            }

            { // assert training refusal saved
                await AuthorizeAsAdmin(client);
                var trainingRefusalValidationResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var submission = Deserialize<GetEnrollmentDetails.Details>(trainingRefusalValidationResult);
                Assert.Equal("Andrzej Strzelba", submission.FullName);
                Assert.Equal("Andrzej", submission.FirstName);
                Assert.Equal("Strzelba", submission.LastName);
                Assert.Equal("andrzej1@strzelba.com", submission.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, submission.PhoneNumber);
                Assert.Equal("pomorskie", submission.Region);
                Assert.Equal("Gdańsk", Assert.Single(submission.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(submission.PreferredTrainings).City);
                Assert.Null(submission.SelectedTraining);

                var submissionEvent = Assert.Single(submission.Events, ev => ev is GetEnrollmentDetails.RecruitmentFormSubmittedEventData) as GetEnrollmentDetails.RecruitmentFormSubmittedEventData;
                Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);

                var refusalEvent = Assert.Single(submission.Events, ev => ev is GetEnrollmentDetails.CandidateRefusedTrainingInvitationEventData) as GetEnrollmentDetails.CandidateRefusedTrainingInvitationEventData;
                Assert.Equal("kandydatowi nie pasuje termin", refusalEvent.RefusalReason);
            }
        }

        [Fact(DisplayName = "Scenariusz 2: zgłoszony kandydat akceptuje zaproszenie, ale nie pojawia się na szkoleniu")]
        public async Task Scenario2()
        {
            using var client = await _fixture.BuildClient();

            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej2@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej2@strzelba.com")));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            { // record training invitation acceptance
                await AuthorizeAsAdmin(client);
                var trainingInvitationAcceptanceResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordAcceptedTrainingInvitation,
                    new RecordAcceptedTrainingInvitation.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.OutgoingPhone,
                        SelectedTrainingID = 1,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingInvitationAcceptanceResult);
                Assert.True(trainingInvitationAcceptanceResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingInvitationAcceptanceResult.StatusCode);
            }

            (_fixture.Services.GetRequiredService<IClock>() as NodaTime.Testing.FakeClock).Advance(2 * aDay);

            { // record training result (absence)
                await AuthorizeAsAdmin(client);
                var trainingResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordTrainingResults,
                    new RecordTrainingResults.Command() {
                        EnrollmentId = enrollmentId,
                        TrainingId = 1,
                        TrainingResult = RecordTrainingResults.TrainingResult.Absent,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingResult);
                Assert.True(trainingResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingResult.StatusCode);
            }

            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej2@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                Assert.Equal(1, candidate.SelectedTraining.ID);
                Assert.False(candidate.HasLecturerRights);

                Assert.Collection(candidate.Events,
                    first => {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third => {
                        var acceptanceEvent = Assert.IsType<GetEnrollmentDetails.CandidateAcceptedTrainingInvitationEventData>(third);
                        Assert.Equal(1, acceptanceEvent.SelectedTraining.ID);
                        Assert.Equal("dodatkowe notatki", acceptanceEvent.AdditionalNotes);
                    },
                    fourth => {
                        var trainingAbsenceEvent = Assert.IsType<GetEnrollmentDetails.CandidateWasAbsentFromTrainingEventData>(fourth);
                        Assert.Equal(1, trainingAbsenceEvent.Training.ID);
                        Assert.Equal("dodatkowe notatki", trainingAbsenceEvent.AdditionalNotes);
                    });
            }
        }

        [Fact(DisplayName = "Scenariusz 3: zgłoszony kandydat akceptuje zaproszenie, pojawia się na szkoleniu, ale nie uzyskuje uprawnień prowadzącego")]
        public async Task Scenario3()
        {
            using var client = await _fixture.BuildClient();

            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej3@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej3@strzelba.com")));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            { // record training invitation acceptance
                await AuthorizeAsAdmin(client);
                var trainingInvitationAcceptanceResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordAcceptedTrainingInvitation,
                    new RecordAcceptedTrainingInvitation.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.OutgoingPhone,
                        SelectedTrainingID = 1,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingInvitationAcceptanceResult);
                Assert.True(trainingInvitationAcceptanceResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingInvitationAcceptanceResult.StatusCode);
            }

            (_fixture.Services.GetRequiredService<IClock>() as NodaTime.Testing.FakeClock).Advance(2 * aDay);

            { // record training result (not accepted)
                await AuthorizeAsAdmin(client);
                var trainingResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordTrainingResults,
                    new RecordTrainingResults.Command()
                    {
                        EnrollmentId = enrollmentId,
                        TrainingId = 1,
                        TrainingResult = RecordTrainingResults.TrainingResult.PresentButNotAcceptedAsLecturer,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingResult);
                Assert.True(trainingResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingResult.StatusCode);
            }


            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej3@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                Assert.Equal(1, candidate.SelectedTraining.ID);
                Assert.False(candidate.HasLecturerRights);

                Assert.Collection(candidate.Events,
                    first => {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third => {
                        var acceptanceEvent = Assert.IsType<GetEnrollmentDetails.CandidateAcceptedTrainingInvitationEventData>(third);
                        Assert.Equal(1, acceptanceEvent.SelectedTraining.ID);
                        Assert.Equal("dodatkowe notatki", acceptanceEvent.AdditionalNotes);
                    },
                    fourth => {
                        var trainingAttendanceEvent = Assert.IsType<GetEnrollmentDetails.CandidateAttendedTrainingEventData>(fourth);
                        Assert.Equal(1, trainingAttendanceEvent.Training.ID);
                        Assert.Equal("dodatkowe notatki", trainingAttendanceEvent.AdditionalNotes);
                    });
            }
        }

        [Fact(DisplayName = "Scenariusz 4: zgłoszony kandydat akceptuje zaproszenie, pojawia się na szkoleniu i uzyskuje uprawnienia prowadzącego")]
        public async Task Scenario4()
        {
            
            using var client = await _fixture.BuildClient();

            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej4@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej4@strzelba.com")));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            { // record training invitation acceptance
                await AuthorizeAsAdmin(client);
                var trainingInvitationAcceptanceResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordAcceptedTrainingInvitation,
                    new RecordAcceptedTrainingInvitation.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.OutgoingPhone,
                        SelectedTrainingID = 1,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingInvitationAcceptanceResult);
                Assert.True(trainingInvitationAcceptanceResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingInvitationAcceptanceResult.StatusCode);
            }

            (_fixture.Services.GetRequiredService<IClock>() as NodaTime.Testing.FakeClock).Advance(2 * aDay);

            { // record training result (not accepted)
                await AuthorizeAsAdmin(client);
                var trainingResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordTrainingResults,
                    new RecordTrainingResults.Command() {
                        EnrollmentId = enrollmentId,
                        TrainingId = 1,
                        TrainingResult = RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingResult);
                Assert.True(trainingResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingResult.StatusCode);
            }


            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej4@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                Assert.Equal(1, candidate.SelectedTraining.ID);
                Assert.True(candidate.HasLecturerRights);

                Assert.Collection(candidate.Events,
                    first => {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third => {
                        var acceptanceEvent = Assert.IsType<GetEnrollmentDetails.CandidateAcceptedTrainingInvitationEventData>(third);
                        Assert.Equal(1, acceptanceEvent.SelectedTraining.ID);
                        Assert.Equal("dodatkowe notatki", acceptanceEvent.AdditionalNotes);
                    },
                    fourth => {
                        var trainingAttendanceEvent = Assert.IsType<GetEnrollmentDetails.CandidateAttendedTrainingEventData>(fourth);
                        Assert.Equal(1, trainingAttendanceEvent.Training.ID);
                        Assert.Equal("dodatkowe notatki", trainingAttendanceEvent.AdditionalNotes);
                    },
                    fifth => {
                        var lecturerRightsObtainedEvent = Assert.IsType<GetEnrollmentDetails.CandidateObtainedLecturerRightsEventData>(fifth);
                        Assert.Equal("dodatkowe notatki", lecturerRightsObtainedEvent.AdditionalNotes);
                    });
            }
        }

        [Fact(DisplayName = "Scenariusz 5: zgłoszony kandydat rezygnuje trwale z udziału w projekcie")]
        public async Task Scenario5()
        {
            using var client = await _fixture.BuildClient();
            
            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej5@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej5@strzelba.com")));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            { // record resignation
                await AuthorizeAsAdmin(client);
                var recordResignationResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordResignation,
                    new RecordResignation.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.IncomingApiRequest,
                        ResignationType = RecordResignation.ResignationType.Permanent,
                        ResignationReason = "kandydat nie ma czasu",
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(recordResignationResult);
                Assert.True(recordResignationResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, recordResignationResult.StatusCode);
                Unauthorize(client);
            }


            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej5@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                Assert.Null(candidate.SelectedTraining);
                Assert.False(candidate.HasLecturerRights);
                Assert.True(candidate.HasResigned);

                Assert.Collection(candidate.Events,
                    first =>
                    {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third =>
                    {
                        var resignationEvent = Assert.IsType<GetEnrollmentDetails.CandidateResignedPermanentlyEventData>(third);
                        Assert.Equal("kandydat nie ma czasu", resignationEvent.ResignationReason);
                        Assert.Equal("dodatkowe notatki", resignationEvent.AdditionalNotes);
                    });
            }
        }

        [Fact(DisplayName = "Scenariusz 6: zgłoszony kandydat rezygnuje tymczasowo z udziału w projekcie")]
        public async Task Scenario6()
        {
            using var client = await _fixture.BuildClient();

            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej6@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej6@strzelba.com")));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            { // record resignation
                await AuthorizeAsAdmin(client);
                var recordResignationResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordResignation,
                    new RecordResignation.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.IncomingApiRequest,
                        ResignationType = RecordResignation.ResignationType.Temporary,
                        ResignationReason = "kandydat nie ma czasu",
                        ResumeDate = FromNow(aWeek).Date,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(recordResignationResult);
                Assert.True(recordResignationResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, recordResignationResult.StatusCode);
                Unauthorize(client);
            }


            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej6@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                Assert.Null(candidate.SelectedTraining);
                Assert.False(candidate.HasLecturerRights);
                Assert.True(candidate.HasResigned);

                Assert.Collection(candidate.Events,
                    first =>
                    {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third =>
                    {
                        var resignationEvent = Assert.IsType<GetEnrollmentDetails.CandidateResignedTemporarilyEventData>(third);
                        Assert.Equal("kandydat nie ma czasu", resignationEvent.ResignationReason);
                        Assert.Equal("dodatkowe notatki", resignationEvent.AdditionalNotes);
                        Assert.Equal(FromNow(aWeek).Date, resignationEvent.ResumeDate);
                    });
            }
        }

        [Fact(DisplayName = "Scenariusz 7: kontaktujemy się ze zgłoszonym kandydatem")]
        public async Task Scenario7()
        {
            using var client = await _fixture.BuildClient();

            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej7@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissions = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissions));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            { // record resignation
                await AuthorizeAsAdmin(client);
                var recordContactResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordContact,
                    new RecordContact.Command() {
                        EnrollmentId = enrollmentId,
                        CommunicationChannel = CommunicationChannel.IncomingApiRequest,
                        Content = "ala ma kota",
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(recordContactResult);
                Assert.True(recordContactResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, recordContactResult.StatusCode);
                Unauthorize(client);
            }

            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej7@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                Assert.Null(candidate.SelectedTraining);
                Assert.False(candidate.HasLecturerRights);
                Assert.False(candidate.HasResigned);

                Assert.Collection(candidate.Events,
                    first =>
                    {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third =>
                    {
                        var contactEvent = Assert.IsType<GetEnrollmentDetails.ContactOccuredEventData>(third);
                        Assert.Equal(CommunicationChannel.IncomingApiRequest, contactEvent.CommunicationChannel);
                        Assert.Equal("ala ma kota", contactEvent.Content);
                        Assert.Equal("dodatkowe notatki", contactEvent.AdditionalNotes);
                    });
            }
        }

        [Fact(DisplayName = "Scenariusz 8: zgłoszony kandydat pojawia się na szkoleniu, choć nie został zaproszony")]
        public async Task Scenario8()
        {
            using var client = await _fixture.BuildClient();

            Guid enrollmentId;
            { // submit recruitment form
                var submissionResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.SubmitRecruitmentForm,
                    new SubmitRecruitmentForm.Command() {
                        FirstName = "Andrzej",
                        LastName = "Strzelba",
                        Email = EmailAddress.Parse("andrzej8@strzelba.com"),
                        PhoneNumber = Szlem.Domain.Consts.FakePhoneNumber,
                        Region = "pomorskie",
                        PreferredLecturingCities = new[] { "Gdańsk" },
                        PreferredTrainingIds = new[] { 1 },
                        AboutMe = "lubię placki",
                        GdprConsentGiven = true
                    });

                await PrintOutProblemDetailsIfFaulty(submissionResult);
                Assert.True(submissionResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, submissionResult.StatusCode);
            }

            { // assert recruitment form saved
                await AuthorizeAsAdmin(client);
                var submissionResult = await client.GetStringAsync(Szlem.AspNetCore.Routes.v1.Enrollments.GetSubmissions);
                var submission = Assert.Single(Deserialize<GetSubmissions.SubmissionSummary[]>(submissionResult)
                    .Where(x => x.Email == EmailAddress.Parse("andrzej8@strzelba.com")));
                enrollmentId = submission.Id;
                Unauthorize(client);
            }

            (_fixture.Services.GetRequiredService<IClock>() as NodaTime.Testing.FakeClock).Advance(2 * aDay);

            { // record training result
                await AuthorizeAsAdmin(client);
                var trainingResult = await client.PostAsJsonAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.RecordTrainingResults,
                    new RecordTrainingResults.Command() {
                        EnrollmentId = enrollmentId,
                        TrainingId = 1,
                        TrainingResult = RecordTrainingResults.TrainingResult.PresentAndAcceptedAsLecturer,
                        AdditionalNotes = "dodatkowe notatki"
                    });
                await PrintOutProblemDetailsIfFaulty(trainingResult);
                Assert.True(trainingResult.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, trainingResult.StatusCode);
            }


            { // assert all data saved
                await AuthorizeAsAdmin(client);
                var detailsResult = await client.GetStringAsync(
                    Szlem.AspNetCore.Routes.v1.Enrollments.GetEnrollment.Replace("{enrollmentID}", enrollmentId.ToString()));
                var candidate = Deserialize<GetEnrollmentDetails.Details>(detailsResult);
                Assert.Equal("Andrzej Strzelba", candidate.FullName);
                Assert.Equal("Andrzej", candidate.FirstName);
                Assert.Equal("Strzelba", candidate.LastName);
                Assert.Equal("andrzej8@strzelba.com", candidate.Email.ToString());
                Assert.Equal(Domain.Consts.FakePhoneNumber, candidate.PhoneNumber);
                Assert.Equal("pomorskie", candidate.Region);
                Assert.Equal("Gdańsk", Assert.Single(candidate.PreferredLecturingCities));
                Assert.Equal("Wadowice", Assert.Single(candidate.PreferredTrainings).City);
                candidate.SelectedTraining.Should().BeNull();
                Assert.True(candidate.HasLecturerRights);

                Assert.Collection(candidate.Events,
                    first => {
                        var submissionEvent = Assert.IsType<GetEnrollmentDetails.RecruitmentFormSubmittedEventData>(first);
                        Assert.Equal("Andrzej Strzelba", submissionEvent.FullName);
                        Assert.Equal("lubię placki", submissionEvent.AboutMe);
                    },
                    second => { /* email sending - ignored */ },
                    third => {
                        var trainingAttendanceEvent = Assert.IsType<GetEnrollmentDetails.CandidateAttendedTrainingEventData>(third);
                        Assert.Equal(1, trainingAttendanceEvent.Training.ID);
                        Assert.Equal("dodatkowe notatki", trainingAttendanceEvent.AdditionalNotes);
                    },
                    fourth => {
                        var lecturerRightsEvent = Assert.IsType<GetEnrollmentDetails.CandidateObtainedLecturerRightsEventData>(fourth);
                        Assert.Equal("dodatkowe notatki", lecturerRightsEvent.AdditionalNotes);
                    });
            }
        }


        #region supporting code
        private async Task AuthorizeAsAdmin(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("admin@test.com"), "test");
        }

        private void Unauthorize(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = null;
        }

        private async Task Authorize(HttpClient client, EmailAddress email, string password)
        {
            var result = await client.PostAsJsonAsync(Szlem.AspNetCore.Routes.v1.Identity.Login, new Szlem.AspNetCore.Contracts.Identity.Login.Request() { Email = email, Password = password });
            var token = await result.Content.ReadAsStringAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, token);
        }


        private async Task PrintOutProblemDetailsIfFaulty(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await response.Content.ReadAsStringAsync());
            _outputHelper.WriteLine(problemDetails.Detail);
            Assert.False(true, problemDetails.Detail);
        }
        
        private T Deserialize<T>(string json)
        {
            var serializerOptions = new Newtonsoft.Json.JsonSerializerSettings();
            serializerOptions = serializerOptions.ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, serializerOptions);
        }
        #endregion
    }
}

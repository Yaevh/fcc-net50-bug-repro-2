using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Szlem.AspNetCore;
using Szlem.DependencyInjection.AspNetCore;
using Szlem.Domain;
using Szlem.IntegrationTestsInfrastructure;
using Szlem.SchoolManagement.Impl;
using Szlem.SharedKernel;
using Xunit;
using Xunit.Abstractions;

namespace Szlem.SchoolManagement.Tests
{
    public class IntegrationTests : IClassFixture<TestFixture>
    {
        #region initialization
        private static string _adminName;
        private static string _coordinatorName;

        private readonly TestFixture _fixture;
        private readonly ITestOutputHelper _output;
        public IntegrationTests(TestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture
                .ConfigureOutput(output)
                .ConfigureServices(ConfigureServices)
                .ConfigureInitialization(Initialize)
                ?? throw new ArgumentNullException(nameof(fixture));
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        private void ConfigureServices(IServiceCollection services)
        {
            services.Remove<NodaTime.IClock>();
            services.AddSingleton<NodaTime.IClock>(new NodaTime.Testing.FakeClock(_fixture.TestStart));
        }

        private async Task Initialize(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<Models.Users.ApplicationUser>>();

            var admin = new Models.Users.ApplicationUser { UserName = "admin", Email = "admin@test.com", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(admin, "test"));
            AssertSuccess(await userManager.AddToRoleAsync(admin, AuthorizationRoles.Admin));
            _adminName = admin.DisplayName;

            var coordinator = new Models.Users.ApplicationUser { UserName = "coordinator", Email = "coordinator@test.com", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(coordinator, "test"));
            AssertSuccess(await userManager.AddToRoleAsync(coordinator, AuthorizationRoles.RegionalCoordinator));
            _coordinatorName = coordinator.DisplayName;
        }
        #endregion


        #region actual tests
        [Fact(DisplayName = "Scenariusz 1. Rejestracja szkoły, kontakt, wstępna zgoda, podpisanie umowy")]
        public async Task Scenario1()
        {
            var now = _fixture.TestStart.InMainTimezone().ToOffsetDateTime();
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator(client);

            #region arrange
            var schoolResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.Register, new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 1",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                            Comment = "test1"
                        },
                    }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();

            var contactResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordContact, new RecordContact.Command() {
                    SchoolId = schoolId,
                    CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                    EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                    ContactPersonName = "sekretariat",
                    Content = "kontakt testowy",
                    ContactTimestamp = now.ToInstant() - NodaTime.Duration.FromDays(1),
                    AdditionalNotes = "test2"
                });
            await PrintOutProblemDetailsIfFaulty(contactResponse);

            var initialAgreementResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordInitialAgreement, new RecordInitialAgreement.Command() {
                    SchoolId = schoolId, AgreeingPersonName = "Andrzej Strzelba", AdditionalNotes = "test3"
                });
            await PrintOutProblemDetailsIfFaulty(initialAgreementResponse);

            var agreeementSignedResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordAgreementSigned, new RecordAgreementSigned.Command() {
                    SchoolId = schoolId,
                    Duration = RecordAgreementSigned.AgreementDuration.FixedTerm,
                    AgreementEndDate = (now + NodaTime.Duration.FromDays(7)).Date,
                    AdditionalNotes = "test4",
                    ScannedDocument = new byte[] { 0x00 },
                    ScannedDocumentExtension = ".jpg", ScannedDocumentContentType = "image/jpeg"
                });
            await PrintOutProblemDetailsIfFaulty(agreeementSignedResponse);
            var agreementId = await agreeementSignedResponse.Content.ReadAsAsync<Guid>();
            #endregion

            #region assert
            var schoolSummariesResponse = await client.GetAsync(Routes.v1.Schools.Index);
            await PrintOutProblemDetailsIfFaulty(schoolSummariesResponse);
            var schoolSummaries = await schoolSummariesResponse.Content.ReadAsAsync<GetSchools.Summary[]>();
            schoolSummaries.Should().ContainEquivalentOf(new GetSchools.Summary() {
                Id = schoolId, Name = "I Liceum Ogólnokształcące",
                City = "Gdańsk", Address = "Wały Piastowskie 1",
                Status = GetSchools.SchoolStatus.HasSignedAgreement
            });

            var schoolDetailsResponse = await client.GetAsync(Routes.v1.Schools.DetailsFor(schoolId));
            await PrintOutProblemDetailsIfFaulty(schoolDetailsResponse);
            var schoolDetails = Deserialize<GetDetails.SchoolDetails>(await schoolDetailsResponse.Content.ReadAsStringAsync());
            schoolDetails.Should().BeEquivalentTo(
                new GetDetails.SchoolDetails() {
                    Id = schoolId, Name = "I Liceum Ogólnokształcące",
                    Address = "Wały Piastowskie 1", City = "Gdańsk",
                    ContactData = new[] { new ContactData() {
                        Name = "sekretariat",
                        EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                        PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                        Comment = "test1"
                    } },
                    HasSignedFixedTermAgreement = true,
                    AgreementEndDate = (now + NodaTime.Duration.FromDays(7)).Date
                },
                options => options.Excluding(x => x.Events));

            var events = schoolDetails.Events.ToArray();
            events.Should().HaveCount(4);
            events[0].Should().BeOfType<GetDetails.SchoolRegisteredEventData>();
            events[1].Should().BeOfType<GetDetails.ContactOccuredEventData>();
            events[1].Should().BeEquivalentTo(new {
                DateTime = now - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"), PhoneNumber = (PhoneNumber)null,
                ContactPersonName = "sekretariat",
                Content = "kontakt testowy",
                AdditionalNotes = "test2",
                RecordingUser = _coordinatorName,
            });
            events[2].Should().BeOfType<GetDetails.InitialAgreementAchievedEventData>();
            events[2].Should().BeEquivalentTo(new {
                AgreeingPersonName = "Andrzej Strzelba", AdditionalNotes = "test3", RecordingUser = _coordinatorName,
            });
            events[3].Should().BeOfType<GetDetails.FixedTermAgreementSignedEventData>();
            events[3].Should().BeEquivalentTo(new {
                AgreementId = agreementId,
                AgreementEndDate = (now + NodaTime.Duration.FromDays(7)).Date,
                AdditionalNotes = "test4",
                RecordingUser = _coordinatorName
            });
            #endregion
        }

        [Fact(DisplayName = "Scenariusz 2. Rejestracja szkoły, podpisanie umowy, rezygnacja")]
        public async Task Scenario2()
        {
            var now = _fixture.TestStart.InMainTimezone().ToOffsetDateTime();
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator(client);

            #region arrange
            var schoolResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.Register, new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 2",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                            Comment = "test1"
                        },
                    }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();

            var agreeementSignedResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordAgreementSigned, new RecordAgreementSigned.Command() {
                    SchoolId = schoolId,
                    Duration = RecordAgreementSigned.AgreementDuration.Permanent,
                    AdditionalNotes = "test2",
                    ScannedDocument = new byte[] { 0x00 },
                    ScannedDocumentExtension = ".jpg",
                    ScannedDocumentContentType = "image/jpeg"
                });
            await PrintOutProblemDetailsIfFaulty(agreeementSignedResponse);
            var agreementId = await agreeementSignedResponse.Content.ReadAsAsync<Guid>();

            var resignationResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordResignation, new RecordResignation.Command() {
                    SchoolId = schoolId,
                    PotentialNextContactDate = now.Plus(NodaTime.Duration.FromDays(30)).Date,
                    AdditionalNotes = "test3"
                });
            await PrintOutProblemDetailsIfFaulty(resignationResponse);
            #endregion

            #region assert
            var schoolSummariesResponse = await client.GetAsync(Routes.v1.Schools.Index);
            await PrintOutProblemDetailsIfFaulty(schoolSummariesResponse);
            var schoolSummaries = await schoolSummariesResponse.Content.ReadAsAsync<GetSchools.Summary[]>();
            schoolSummaries.Should().ContainEquivalentOf(new GetSchools.Summary() {
                Id = schoolId,
                Name = "I Liceum Ogólnokształcące",
                City = "Gdańsk", Address = "Wały Piastowskie 2",
                Status = GetSchools.SchoolStatus.HasResigned
            });

            var schoolDetailsResponse = await client.GetAsync(Routes.v1.Schools.DetailsFor(schoolId));
            await PrintOutProblemDetailsIfFaulty(schoolDetailsResponse);
            var schoolDetails = Deserialize<GetDetails.SchoolDetails>(await schoolDetailsResponse.Content.ReadAsStringAsync());
            schoolDetails.Should().BeEquivalentTo(
                new GetDetails.SchoolDetails() {
                    Id = schoolId, Name = "I Liceum Ogólnokształcące",
                    Address = "Wały Piastowskie 2", City = "Gdańsk",
                    ContactData = new[] { new ContactData() {
                        Name = "sekretariat",
                        EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                        PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                        Comment = "test1"
                    } },
                    HasResignedTemporarily = true,
                    ResignationEndDate = now.Plus(NodaTime.Duration.FromDays(30)).Date
                },
                options => options.Excluding(x => x.Events));

            var events = schoolDetails.Events.ToArray();
            events.Should().HaveCount(3);
            events[0].Should().BeOfType<GetDetails.SchoolRegisteredEventData>();
            events[1].Should().BeOfType<GetDetails.PermanentAgreementSignedEventData>();
            events[1].Should().BeEquivalentTo(new {
                RecordingUser = _coordinatorName,
                AgreementId = agreementId,
                AdditionalNotes = "test2"
            });
            events[2].Should().BeOfType<GetDetails.SchoolResignedFromCooperationEventData>();
            events[2].Should().BeEquivalentTo(new {
                RecordingUser = _coordinatorName,
                PotentialNextContactDate = now.Plus(NodaTime.Duration.FromDays(30)).Date,
                AdditionalNotes = "test3"
            });
            #endregion
        }

        [Fact(DisplayName = "Scenariusz 3. Rejestracja szkoły, rezygnacja, kontakt, wstępna zgoda")]
        public async Task Scenario3()
        {
            var now = _fixture.TestStart.InMainTimezone().ToOffsetDateTime();
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator(client);

            #region arrange
            var schoolResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.Register, new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 3",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                            Comment = "test1"
                        },
                    }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();

            var resignationResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordResignation, new RecordResignation.Command() {
                    SchoolId = schoolId,
                    PotentialNextContactDate = now.Plus(NodaTime.Duration.FromDays(30)).Date,
                    AdditionalNotes = "test2"
                });
            await PrintOutProblemDetailsIfFaulty(resignationResponse);

            var contactResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordContact, new RecordContact.Command() {
                    SchoolId = schoolId,
                    CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                    EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                    ContactPersonName = "sekretariat",
                    Content = "kontakt testowy",
                    ContactTimestamp = now.ToInstant() - NodaTime.Duration.FromDays(1),
                    AdditionalNotes = "test3"
                });
            await PrintOutProblemDetailsIfFaulty(contactResponse);

            var initialAgreementResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.RecordInitialAgreement, new RecordInitialAgreement.Command() {
                    SchoolId = schoolId,
                    AgreeingPersonName = "Andrzej Strzelba",
                    AdditionalNotes = "test4"
                });
            await PrintOutProblemDetailsIfFaulty(initialAgreementResponse);
            #endregion

            #region assert
            var schoolSummariesResponse = await client.GetAsync(Routes.v1.Schools.Index);
            await PrintOutProblemDetailsIfFaulty(schoolSummariesResponse);
            var schoolSummaries = await schoolSummariesResponse.Content.ReadAsAsync<GetSchools.Summary[]>();
            schoolSummaries.Should().ContainEquivalentOf(new GetSchools.Summary() {
                Id = schoolId,
                Name = "I Liceum Ogólnokształcące",
                City = "Gdańsk",
                Address = "Wały Piastowskie 3",
                Status = GetSchools.SchoolStatus.HasAgreedInitially
            });

            var schoolDetailsResponse = await client.GetAsync(Routes.v1.Schools.DetailsFor(schoolId));
            await PrintOutProblemDetailsIfFaulty(schoolDetailsResponse);
            var schoolDetails = Deserialize<GetDetails.SchoolDetails>(await schoolDetailsResponse.Content.ReadAsStringAsync());

            schoolDetails.Should().BeEquivalentTo(
                new GetDetails.SchoolDetails() {
                    Id = schoolId, Name = "I Liceum Ogólnokształcące",
                    Address = "Wały Piastowskie 3", City = "Gdańsk",
                    ContactData = new[] { new ContactData() {
                        Name = "sekretariat",
                        EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                        PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                        Comment = "test1"
                    } },
                    HasAgreedInitially = true
                },
                options => options.Excluding(x => x.Events).Excluding(x => x.Notes));

            var events = schoolDetails.Events.ToArray();
            events.Should().HaveCount(4);
            events[0].Should().BeOfType<GetDetails.SchoolRegisteredEventData>();
            events[1].Should().BeOfType<GetDetails.SchoolResignedFromCooperationEventData>();
            events[1].Should().BeEquivalentTo(new {
                RecordingUser = _coordinatorName,
                PotentialNextContactDate = now.Plus(NodaTime.Duration.FromDays(30)).Date,
                AdditionalNotes = "test2"
            });
            events[2].Should().BeOfType<GetDetails.ContactOccuredEventData>();
            events[2].Should().BeEquivalentTo(new {
                DateTime = now - NodaTime.Duration.FromDays(1),
                CommunicationChannel = CommunicationChannelType.OutgoingEmail,
                EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"), PhoneNumber = (PhoneNumber)null,
                ContactPersonName = "sekretariat",
                Content = "kontakt testowy",
                AdditionalNotes = "test3",
                RecordingUser = _coordinatorName,
            });
            events[3].Should().BeOfType<GetDetails.InitialAgreementAchievedEventData>();
            events[3].Should().BeEquivalentTo(new {
                AgreeingPersonName = "Andrzej Strzelba", AdditionalNotes = "test4", RecordingUser = _coordinatorName,
            });
            #endregion
        }

        [Fact(DisplayName = "Scenariusz 4. Rejestracja szkoły, dodanie notatki, edycja notatki, usunięcie notatki")]
        public async Task Scenario4()
        {
            var now = _fixture.TestStart.InMainTimezone();
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator(client);

            #region arrange
            var schoolResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.Register, new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 4",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                            Comment = "test1"
                        },
                    }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();

            var addNoteResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test2" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();

            var editNoteResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.EditNote,
                new EditNote.Command() { SchoolId = schoolId, NoteId = noteId, Content = "test3" });
            await PrintOutProblemDetailsIfFaulty(editNoteResponse);

            var deleteNoteResponse = await client.PostAsJsonAsync(
                Routes.v1.Schools.DeleteNote,
                new DeleteNote.Command() { SchoolId = schoolId, NoteId = noteId });
            await PrintOutProblemDetailsIfFaulty(deleteNoteResponse);

            var addNoteResponse2 = await client.PostAsJsonAsync(
                Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test4" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse2);
            var noteId2 = await addNoteResponse2.Content.ReadAsAsync<Guid>();
            #endregion


            #region assert
            var schoolSummariesResponse = await client.GetAsync(Routes.v1.Schools.Index);
            await PrintOutProblemDetailsIfFaulty(schoolSummariesResponse);
            var schoolSummaries = await schoolSummariesResponse.Content.ReadAsAsync<GetSchools.Summary[]>();
            schoolSummaries.Should().ContainEquivalentOf(new GetSchools.Summary() {
                Id = schoolId,
                Name = "I Liceum Ogólnokształcące",
                City = "Gdańsk",
                Address = "Wały Piastowskie 4",
                Status = GetSchools.SchoolStatus.Unknown
            });

            var schoolDetailsResponse = await client.GetAsync(Routes.v1.Schools.DetailsFor(schoolId));
            await PrintOutProblemDetailsIfFaulty(schoolDetailsResponse);
            var schoolDetails = Deserialize<GetDetails.SchoolDetails>(await schoolDetailsResponse.Content.ReadAsStringAsync());
            schoolDetails.Should().BeEquivalentTo(
                new GetDetails.SchoolDetails() {
                    Id = schoolId, Name = "I Liceum Ogólnokształcące",
                    Address = "Wały Piastowskie 4", City = "Gdańsk",
                    ContactData = new[] { new ContactData() {
                        Name = "sekretariat",
                        EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                        PhoneNumber = PhoneNumber.Parse("58 301-67-34"),
                        Comment = "test1"
                    } }
                },
                options => options.Excluding(x => x.Events).Excluding(x => x.Notes));

            schoolDetails.Notes.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { Id = noteId2, Content = "test4", Author = _coordinatorName });

            var events = schoolDetails.Events.ToArray();
            events.Should().HaveCount(1);
            events[0].Should().BeOfType<GetDetails.SchoolRegisteredEventData>();
            #endregion
        }
        #endregion


        #region supporting code
        private async Task AuthorizeAsCoordinator(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("coordinator@test.com"), "test");
        }

        private void Unauthorize(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = null;
        }

        private async Task Authorize(HttpClient client, EmailAddress email, string password)
        {
            Unauthorize(client);
            var result = await client.PostAsJsonAsync(AspNetCore.Routes.v1.Identity.Login, new Szlem.AspNetCore.Contracts.Identity.Login.Request() { Email = email, Password = password });
            result.IsSuccessStatusCode.Should().BeTrue();
            var token = await result.Content.ReadAsStringAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, token);
        }

        private void AssertSuccess(IdentityResult result)
        {
            result.Succeeded.Should().BeTrue(result.Errors.FirstOrDefault()?.Description);
        }

        private async Task PrintOutProblemDetailsIfFaulty(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await response.Content.ReadAsStringAsync());
            _output.WriteLine(problemDetails.Title);
            Assert.False(true, problemDetails.Title);
        }

        private T Deserialize<T>(string json)
        {
            var serializerOptions = new Newtonsoft.Json.JsonSerializerSettings()
                .ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, serializerOptions);
        }
        #endregion
    }
}

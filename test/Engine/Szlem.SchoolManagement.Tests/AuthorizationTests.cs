using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.IntegrationTestsInfrastructure;
using Szlem.SchoolManagement.Impl;
using Szlem.SharedKernel;
using Xunit;
using Xunit.Abstractions;

namespace Szlem.SchoolManagement.Tests
{
    public class AuthorizationTests : IClassFixture<TestFixture>
    {
        private readonly TestFixture _fixture;
        private readonly ITestOutputHelper _output;
        public AuthorizationTests(TestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture
                .ConfigureOutput(output)
                .ConfigureServices(ConfigureServices)
                .ConfigureInitialization(Initialize)
                ?? throw new ArgumentNullException(nameof(fixture));
            _output = output;
        }


        private void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped(typeof(MediatR.IPipelineBehavior<,>), typeof(Engine.Behaviors.AuthorizationBehavior<,>));
        }

        private async Task Initialize(IServiceProvider services)
        {
            var userManager = services.GetRequiredService<UserManager<Models.Users.ApplicationUser>>();

            var admin = new Models.Users.ApplicationUser { UserName = "admin", Email = "admin@test.com", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(admin, "test"));
            AssertSuccess(await userManager.AddToRoleAsync(admin, AuthorizationRoles.Admin));

            var coordinator1 = new Models.Users.ApplicationUser { UserName = "coordinator1", Email = "coordinator1@test.com", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(coordinator1, "test"));
            AssertSuccess(await userManager.AddToRoleAsync(coordinator1, AuthorizationRoles.RegionalCoordinator));

            var coordinator2 = new Models.Users.ApplicationUser { UserName = "coordinator2", Email = "coordinator2@test.com", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(coordinator2, "test"));
            AssertSuccess(await userManager.AddToRoleAsync(coordinator2, AuthorizationRoles.RegionalCoordinator));

            var regularUser = new Models.Users.ApplicationUser { UserName = "jp2", Email = "jp2@pope.va", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(regularUser, "test"));
        }



        [Fact(DisplayName = "Notes.Add.1 Koordynator może dodawać notatki")]
        public async Task Administrator_can_add_note()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
            });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsCoordinator1(client);
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });


            // assert
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            addNoteResponse.IsSuccessStatusCode.Should().BeTrue();
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Content = "test" });
        }

        
        [Fact(DisplayName = "Notes.Delete.1 Administrator może usuwać cudze notatki")]
        public async Task Administrator_can_delete_note()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command() {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsAdmin(client);
            var deleteNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.DeleteNote,
                new DeleteNote.Command() { SchoolId = schoolId, NoteId = noteId });


            // assert
            await PrintOutProblemDetailsIfFaulty(deleteNoteResponse);
            deleteNoteResponse.IsSuccessStatusCode.Should().BeTrue();
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().BeEmpty();
        }

        [Fact(DisplayName = "Notes.Delete.2 Autor może usuwać własne notatki")]
        public async Task Author_can_delete_note()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command()
                {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsCoordinator1(client);
            var deleteNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.DeleteNote,
                new DeleteNote.Command() { SchoolId = schoolId, NoteId = noteId });


            // assert
            await PrintOutProblemDetailsIfFaulty(deleteNoteResponse);
            deleteNoteResponse.IsSuccessStatusCode.Should().BeTrue();
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().BeEmpty();
        }

        [Fact(DisplayName = "Notes.Delete.3 Koordynator nie będący autorem nie może usuwać cudzych notatek")]
        public async Task Coordinator_cannot_delete_note()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command()
                {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsCoordinator2(client);
            var deleteNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.DeleteNote,
                new DeleteNote.Command() { SchoolId = schoolId, NoteId = noteId });


            // assert
            deleteNoteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Content = "test" });
        }


        [Fact(DisplayName = "Notes.Edit.1 Administrator może edytować notatki")]
        public async Task Administrator_can_edit_notes()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command()
                {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsAdmin(client);
            var editNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.EditNote,
                new EditNote.Command() { SchoolId = schoolId, NoteId = noteId, Content = "test2" });


            // assert
            await PrintOutProblemDetailsIfFaulty(editNoteResponse);
            editNoteResponse.IsSuccessStatusCode.Should().BeTrue();
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { NoteId = noteId, Content = "test2" });
        }

        [Fact(DisplayName = "Notes.Edit.2 Autor może edytować notatki")]
        public async Task Author_can_edit_note()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command()
                {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsCoordinator1(client);
            var editNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.EditNote,
                new EditNote.Command() { SchoolId = schoolId, NoteId = noteId, Content = "test2" });


            // assert
            await PrintOutProblemDetailsIfFaulty(editNoteResponse);
            editNoteResponse.IsSuccessStatusCode.Should().BeTrue();
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().ContainSingle()
                .Which.Should().BeEquivalentTo(new { NoteId = noteId, Content = "test2" });
        }

        [Fact(DisplayName = "Notes.Edit.3 Koordynator nie będący autorem nie może edytować notatki")]
        public async Task Coordinator_cannot_edit_note()
        {
            // arrange
            using var client = await _fixture.BuildClient();

            await AuthorizeAsCoordinator1(client);
            var schoolResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.Register, new RegisterSchool.Command()
                {
                    Name = "I Liceum Ogólnokształcące",
                    City = "Gdańsk",
                    Address = "Wały Piastowskie 6",
                    ContactData = new[] {
                        new ContactData() {
                            Name = "sekretariat",
                            EmailAddress = EmailAddress.Parse("szkola@ilo.gda.pl"),
                            PhoneNumber = PhoneNumber.Parse("58 301-67-34") },
                        }
                });
            await PrintOutProblemDetailsIfFaulty(schoolResponse);
            var schoolId = await schoolResponse.Content.ReadAsAsync<Guid>();
            var addNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.AddNote,
                new AddNote.Command() { SchoolId = schoolId, Content = "test" });
            await PrintOutProblemDetailsIfFaulty(addNoteResponse);
            var noteId = await addNoteResponse.Content.ReadAsAsync<Guid>();


            // act
            await AuthorizeAsCoordinator2(client);
            var editNoteResponse = await client.PostAsJsonAsync(
                Szlem.AspNetCore.Routes.v1.Schools.EditNote,
                new EditNote.Command() { SchoolId = schoolId, NoteId = noteId, Content = "test2" });


            // assert
            editNoteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
            var school = await _fixture.Services.GetRequiredService<EventFlow.Aggregates.IAggregateStore>()
                .LoadAsync<SchoolAggregate, SchoolId>(SchoolId.With(schoolId), CancellationToken.None);
            school.Notes.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Content = "test" });
        }


        #region supporting code
        private async Task AuthorizeAsAdmin(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("admin@test.com"), "test");
        }

        private async Task AuthorizeAsCoordinator1(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("coordinator1@test.com"), "test");
        }

        private async Task AuthorizeAsCoordinator2(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("coordinator2@test.com"), "test");
        }

        private async Task AuthorizeAsRegularUser(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("jp2@pope.va"), "test");
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
            _output.WriteLine(problemDetails.Detail);
            Assert.False(true, problemDetails.Detail);
        }
        #endregion
    }
}

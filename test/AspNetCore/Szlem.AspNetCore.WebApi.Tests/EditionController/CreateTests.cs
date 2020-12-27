using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Editions.Editions;
using Szlem.Models.Editions;
using Szlem.Models.Users;
using Szlem.Persistence.EF;
using Szlem.SharedKernel;
using Xunit;
using Xunit.Abstractions;

namespace Szlem.AspNetCore.WebApi.Tests.EditionController
{
    [Collection("Szlem.AspNetCore.WebApi.Tests.EditionController")]
    public class CreateTests
    {
        private readonly EditionFixture _fixture;

        private readonly System.Net.Http.Formatting.MediaTypeFormatter[] _defaultFormatters;

        public CreateTests(EditionFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture
                .ConfigureOutput(output)
                as EditionFixture
                ?? throw new ArgumentNullException(nameof(fixture));
            var formatter = new System.Net.Http.Formatting.JsonMediaTypeFormatter();
            formatter.SupportedMediaTypes.Add(new System.Net.Http.Headers.MediaTypeHeaderValue("application/problem+json"));
            _defaultFormatters = new[] { formatter };
        }


        [Fact]
        public async Task AsAnonymousUser_FailsWith401Unauthorized()
        {
            using var client = await _fixture.BuildClient();

            _fixture.Unauthorize(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command()
            {
                Name = "test",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            });

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Unauthorized", problemDetails.Title);
        }

        [Fact]
        public async Task AsRegularUser_FailsWith403Forbidden()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsRegularUser(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command() {
                Name = "test",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            });

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
            Assert.Equal("https://httpstatuses.com/403", problemDetails.Type);
        }

        [Fact]
        public async Task AsAdmin_Succeeds()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command() {
                Name = "test2",
                StartDate = DateTime.Today.AddMonths(1),
                EndDate = DateTime.Today.AddMonths(1).AddDays(7)
            });

            Assert.Equal(System.Net.HttpStatusCode.Created, result.StatusCode);
            Assert.Equal(Routes.v1.Editions.DetailsFor(2), result.Headers.Location.OriginalString);
        }


        [Fact]
        public async Task OverlappingAnotherEdition_Returns400BadRequest()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command()
            {
                Name = "test",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7)
            });

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
            var content = await result.Content.ReadAsStringAsync();
            Assert.Contains("New Edition overlaps old editions", content);
        }

        [Fact]
        public async Task StartDateLaterThanEndDate_Returns400BadRequest()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command()
            {
                Name = "test",
                StartDate = DateTime.Today.AddMonths(1).AddDays(7),
                EndDate = DateTime.Today.AddMonths(1)
            });

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
            var failures = await result.Content.ReadAsAsync<ValidationProblemDetails>(_defaultFormatters);
            Assert.Collection(failures.Errors,
                first => {
                    Assert.Equal(nameof(CreateUseCase.Command.EndDate), first.Key);
                    Assert.Collection(first.Value, single => Assert.Equal(CreateUseCase.CommandValidator.EndDateMustBeLaterThanStartDate, single));
                },
                second => {
                    Assert.Equal(nameof(CreateUseCase.Command.StartDate), second.Key);
                    Assert.Collection(second.Value, single => Assert.Equal(CreateUseCase.CommandValidator.StartDateMustBeEarlierThanEndDate, single));
                }
            );
        }

        [Fact]
        public async Task WithEmptyStartDate_Returns400BadRequest()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command()
            {
                Name = "test",
                EndDate = DateTime.Today
            });

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
            var failures = await result.Content.ReadAsAsync<ValidationProblemDetails>(_defaultFormatters);
            Assert.Collection(failures.Errors,
                first => {
                    Assert.Equal(nameof(CreateUseCase.Command.StartDate), first.Key);
                    Assert.Collection(first.Value, single => Assert.Equal(CreateUseCase.CommandValidator.StartDateCannotBeEmpty, single));
                }
            );
        }

        [Fact]
        public async Task WithEmptyEndDate_Returns400BadRequest()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.PostAsJsonAsync(Routes.v1.Editions.Create, new CreateUseCase.Command()
            {
                Name = "test",
                StartDate = DateTime.Today
            });

            Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
            var failures = await result.Content.ReadAsAsync<ValidationProblemDetails>(_defaultFormatters);
            Assert.Collection(failures.Errors,
                first => {
                    Assert.Equal(nameof(CreateUseCase.Command.EndDate), first.Key);
                    Assert.Collection(first.Value, single => Assert.Equal(CreateUseCase.CommandValidator.EndDateCannotBeEmpty, single));
                }
            );
        }
    }
}

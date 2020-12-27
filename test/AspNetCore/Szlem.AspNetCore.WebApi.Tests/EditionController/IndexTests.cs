using FluentAssertions;
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
using Szlem.Models.Editions;
using Szlem.Models.Users;
using Szlem.Persistence.EF;
using Xunit;
using Xunit.Abstractions;
using static Szlem.Engine.Editions.Editions.IndexUseCase;

namespace Szlem.AspNetCore.WebApi.Tests.EditionController
{
    [Collection("Szlem.AspNetCore.WebApi.Tests.EditionController")]
    public class IndexTests
    {
        private readonly EditionFixture _fixture;

        public IndexTests(EditionFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture
                .ConfigureOutput(output)
                //.ConfigureInitialization(Initialize)
                as EditionFixture
                ?? throw new ArgumentNullException(nameof(fixture));
        }


        [Fact]
        public async Task WithoutAuthentication_Returns401Unauthorized()
        {
            using var client = await _fixture.BuildClient();

            _fixture.Unauthorize(client);

            var result = await client.GetAsync(Routes.v1.Editions.Index);

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
            Assert.Equal("Unauthorized", problemDetails.Title);
        }

        [Fact]
        public async Task AsRegularUser_Returns403Forbidden()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsRegularUser(client);

            var result = await client.GetAsync(Routes.v1.Editions.Index);

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
            Assert.Equal("https://httpstatuses.com/403", problemDetails.Type);
        }

        [Fact]
        public async Task AsAdmin_ReturnsSeededEdition()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.GetAsync(Routes.v1.Editions.Index);

            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            var editions = await result.Content.ReadAsAsync<EditionSummary[]>();
            editions.Should().ContainEquivalentOf(new EditionSummary()
            {
                ID = 1,
                Name = "test",
                StartDate = NodaTime.LocalDate.FromDateTime(DateTime.Today),
                EndDate = NodaTime.LocalDate.FromDateTime(DateTime.Today).PlusDays(7),
                CanShowDetails = true
            });
        }
    }
}

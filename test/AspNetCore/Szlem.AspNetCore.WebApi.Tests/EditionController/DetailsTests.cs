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
using Szlem.SharedKernel;
using Xunit;
using Xunit.Abstractions;
using static Szlem.Engine.Editions.Editions.DetailsUseCase;

namespace Szlem.AspNetCore.WebApi.Tests.EditionController
{
    [Collection("Szlem.AspNetCore.WebApi.Tests.EditionController")]
    public class DetailsTests
    {
        private readonly EditionFixture _fixture;

        public DetailsTests(EditionFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture
                .ConfigureOutput(output)
                as EditionFixture
                ?? throw new ArgumentNullException(nameof(fixture));
        }


        [Fact]
        public async Task AsAnonymousUser_ForExistingEdition_Returns401Unauthorized()
        {
            using var client = await _fixture.BuildClient();

            _fixture.Unauthorize(client);

            var result = await client.GetAsync(Routes.v1.Editions.DetailsFor(1));

            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, result.StatusCode);
            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
            Assert.Equal("https://httpstatuses.com/401", problemDetails.Type);
        }

        [Fact]
        public async Task AsRegularUser_ForExistingEdition_Returns403Forbidden()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsRegularUser(client);

            var result = await client.GetAsync(Routes.v1.Editions.DetailsFor(1));

            Assert.Equal(System.Net.HttpStatusCode.Forbidden, result.StatusCode);
            var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
            Assert.Equal("https://httpstatuses.com/403", problemDetails.Type);
        }

        [Fact]
        public async Task AsAdmin_ForExistingEdition_ReturnsDetails()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.GetAsync(Routes.v1.Editions.DetailsFor(1));

            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            var edition = await result.Content.ReadAsAsync<EditionDetails>();
            Assert.Equal(1, edition.ID);
            Assert.True(edition.IsCurrent);
            Assert.Equal("test", edition.Name);
            Assert.Equal(NodaTime.LocalDate.FromDateTime(DateTime.Today), edition.StartDate);
            Assert.Equal(NodaTime.LocalDate.FromDateTime(DateTime.Today.AddDays(7)), edition.EndDate);
        }

        [Fact]
        public async Task AsAdmin_ForNonExistingEdition_Returns404NotFound()
        {
            using var client = await _fixture.BuildClient();

            await _fixture.AuthorizeAsAdmin(client);

            var result = await client.GetAsync(Routes.v1.Editions.DetailsFor(42));

            Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
        }
    }
}

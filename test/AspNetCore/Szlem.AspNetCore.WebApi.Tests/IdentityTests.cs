using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Szlem.AspNetCore.Contracts.Identity;
using Szlem.Domain;
using Szlem.Models.Users;
using Szlem.Persistence.EF;
using Szlem.SharedKernel;
using Xunit;
using Xunit.Abstractions;

namespace Szlem.AspNetCore.WebApi.Tests
{
    public class IdentityTests : IClassFixture<Szlem.IntegrationTestsInfrastructure.TestFixture>
    {
        private readonly Szlem.IntegrationTestsInfrastructure.TestFixture _fixture;
        public IdentityTests(Szlem.IntegrationTestsInfrastructure.TestFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture
                .ConfigureOutput(output)
                .ConfigureServices(services => {
                    var dbName = $"{nameof(IdentityTests)}-{Guid.NewGuid()}";
                    services.RemoveAll<AppDbContext>();
                    services.RemoveAll<DbContextOptions<AppDbContext>>();
                    services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(dbName));

                    services.AddSingleton<EventFlow.EventStores.IEventPersistence, EventFlow.EventStores.InMemory.InMemoryEventPersistence>();
                })
                .ConfigureInitialization(async services => {
                    var user = new ApplicationUser { UserName = "andrzej@strzelba.com", Email = "andrzej@strzelba.com", EmailConfirmed = true };
                    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                    await userManager.CreateAsync(user, "test");
                });
        }



        [Fact]
        public async Task ExistingUser_CanLogIn()
        {
            using var client = await _fixture.BuildClient();

            var result = await client.PostAsJsonAsync(Routes.v1.Identity.Login, new Login.Request() { Email = EmailAddress.Parse("andrzej@strzelba.com"), Password = "test" });

            Assert.True(result.IsSuccessStatusCode);
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

            var content = await result.Content.ReadAsStringAsync();
            Assert.True(new JwtSecurityTokenHandler().CanReadToken(content));
            var token = new JwtSecurityTokenHandler().ReadJwtToken(content);
        }

        [Fact]
        public async Task ExistingUser_WithWrongPassword_CannotLogIn()
        {
            using (var client = await _fixture.BuildClient())
            {
                var result = await client.PostAsJsonAsync(Routes.v1.Identity.Login, new Login.Request() { Email = EmailAddress.Parse("andrzej@strzelba.com"), Password = "ala ma kota" });

                Assert.False(result.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
                var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Invalid login attempt", problemDetails.Title);
                Assert.Equal("Invalid login attempt", problemDetails.Detail);
            }
        }

        [Fact]
        public async Task NonExistingUser_CannotLogIn()
        {
            using (var client = await _fixture.BuildClient())
            {
                var result = await client.PostAsJsonAsync(Routes.v1.Identity.Login, new Login.Request() { Email = EmailAddress.Parse("jp2@pope.va"), Password = "test" });

                Assert.False(result.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
                var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Invalid login attempt", problemDetails.Title);
                Assert.Equal("Invalid login attempt", problemDetails.Detail);
            }
        }

        [Fact]
        public async Task NewUser_CanRegister()
        {
            using (var client = await _fixture.BuildClient())
            {
                var result = await client.PostAsJsonAsync(Routes.v1.Identity.Register, new Register.Request() { Email = EmailAddress.Parse("jp2@pope.va"), Password = "test" });

                Assert.True(result.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);

                var content = await result.Content.ReadAsStringAsync();
                Assert.True(new JwtSecurityTokenHandler().CanReadToken(content));
            }
        }

        [Fact]
        public async Task ExistingUser_WithExistingPassword_CannotRegisterAgain()
        {
            using (var client = await _fixture.BuildClient())
            {
                var result = await client.PostAsJsonAsync(Routes.v1.Identity.Register, new Register.Request() { Email = EmailAddress.Parse("andrzej@strzelba.com"), Password = "test" });

                Assert.False(result.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
                var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Email 'andrzej@strzelba.com' is already taken.", problemDetails.Title);
                Assert.Equal("Email 'andrzej@strzelba.com' is already taken.", problemDetails.Detail);
            }
        }

        [Fact]
        public async Task ExistingUser_WithNewPassword_CannotRegisterAgain()
        {
            using (var client = await _fixture.BuildClient())
            {
                var result = await client.PostAsJsonAsync(Routes.v1.Identity.Register, new Register.Request() { Email = EmailAddress.Parse("andrzej@strzelba.com"), Password = "ala ma kota" });

                Assert.False(result.IsSuccessStatusCode);
                Assert.Equal(System.Net.HttpStatusCode.BadRequest, result.StatusCode);
                var problemDetails = System.Text.Json.JsonSerializer.Deserialize<ProblemDetails>(await result.Content.ReadAsStringAsync());
                Assert.Equal("Email 'andrzej@strzelba.com' is already taken.", problemDetails.Title);
                Assert.Equal("Email 'andrzej@strzelba.com' is already taken.", problemDetails.Detail);
            }
        }

    }
}

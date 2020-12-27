using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.IntegrationTestsInfrastructure;
using Szlem.Models.Editions;
using Szlem.Models.Users;
using Szlem.Persistence.EF;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.AspNetCore.WebApi.Tests.EditionController
{
    public class EditionFixture : TestFixture
    {
        protected override void ConfigureServicesInternal(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<EventFlow.EventStores.IEventPersistence, EventFlow.EventStores.InMemory.InMemoryEventPersistence>();

            var dbName = $"{nameof(EditionFixture)}-{Guid.NewGuid()}";
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            services.RemoveAll<Recruitment.Impl.Repositories.ICampaignRepository>();
            services.AddSingleton(
                Mock.Of<Recruitment.Impl.Repositories.ICampaignRepository>(repo =>
                    repo.GetByEditionId(It.IsAny<int>()) == Task.FromResult((IReadOnlyCollection<Recruitment.Impl.Entities.Campaign>)Array.Empty<Recruitment.Impl.Entities.Campaign>())));
        }

        protected override async Task Initialize(IServiceProvider serviceProvider)
        {
            var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
            dbContext.Editions.Add(new Edition() { Name = "test", StartDate = DateTime.Today, EndDate = DateTime.Today.AddDays(7) });

            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            var admin = new ApplicationUser { UserName = "admin", Email = "admin@test.com", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(admin, "test"));
            AssertSuccess(await userManager.AddToRoleAsync(admin, AuthorizationRoles.Admin));

            var regularUser = new ApplicationUser { UserName = "jp2", Email = "jp2@pope.va", EmailConfirmed = true };
            AssertSuccess(await userManager.CreateAsync(regularUser, "test"));
        }


        public async Task AuthorizeAsAdmin(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("admin@test.com"), "test");
        }

        public async Task AuthorizeAsRegularUser(HttpClient client)
        {
            await Authorize(client, EmailAddress.Parse("jp2@pope.va"), "test");
        }

        public void Unauthorize(HttpClient client)
        {
            client.DefaultRequestHeaders.Authorization = null;
        }

        private async Task Authorize(HttpClient client, EmailAddress email, string password)
        {
            var result = await client.PostAsJsonAsync(Szlem.AspNetCore.Routes.v1.Identity.Login, new Szlem.AspNetCore.Contracts.Identity.Login.Request() { Email = email, Password = password });
            result.IsSuccessStatusCode.Should().BeTrue();
            var token = await result.Content.ReadAsStringAsync();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, token);
        }

        private void AssertSuccess(IdentityResult result)
        {
            result.Succeeded.Should().BeTrue(result.Errors.FirstOrDefault()?.Description);
        }
    }

    [CollectionDefinition("Szlem.AspNetCore.WebApi.Tests.EditionController")]
    public class Editioncollection : ICollectionFixture<EditionFixture> { }
}

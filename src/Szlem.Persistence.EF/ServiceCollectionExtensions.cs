using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddEFUnitOfWork(this IServiceCollection services, Action<DbContextOptionsBuilder> options)
        {
            return services
                .AddDbContext<AppDbContext>(options)
                .AddAsyncInitializer<Initializer>()
                .Decorate<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimPrincipalFactory>();
        }

        public static IdentityBuilder AddEFStores(this IdentityBuilder builder)
        {
            return builder.AddEntityFrameworkStores<AppDbContext>();
        }
    }
}

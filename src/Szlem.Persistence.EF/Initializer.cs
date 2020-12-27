using Extensions.Hosting.AsyncInitialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Engine.Infrastructure;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF
{
    internal class Initializer : IAsyncInitializer
    {
        private readonly AppDbContext _dbContext;
        private readonly RoleManager<ApplicationIdentityRole> _roleManager;
        private readonly IHostingEnvironment _environment;

        public Initializer(AppDbContext dbContext, RoleManager<ApplicationIdentityRole> roleManager, IHostingEnvironment environment)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }


        public async Task InitializeAsync()
        {
            if (_environment.IsDevelopment() && _dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_dbContext.Database.GetDbConnection().DataSource));
                await _dbContext.Database.MigrateAsync();
            }

            foreach (var role in UserRoleDefinitions.Roles)
            {
                if (await _roleManager.RoleExistsAsync(role.Name) == false)
                    await _roleManager.CreateAsync(role);
            }
        }
    }
}

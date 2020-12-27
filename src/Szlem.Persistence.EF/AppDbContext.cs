using FluentValidation;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Models;
using Szlem.Models.Schools;
using Szlem.Models.Users;
using Szlem.Persistence.EF.Extensions;

namespace Szlem.Persistence.EF
{
    public class AppDbContext : IdentityDbContext<ApplicationUser, ApplicationIdentityRole, Guid>
    {
        private readonly IValidatorFactory _validatorFactory;

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public AppDbContext(DbContextOptions<AppDbContext> options, IValidatorFactory validatorFactory) : this(options)
        {
            _validatorFactory = validatorFactory;
        }


        public DbSet<Szlem.Models.Editions.Edition> Editions => Set<Szlem.Models.Editions.Edition>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            ConfigureModel(modelBuilder);
        }

        private static void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyAllConfigurations();
        }


        #region validate on save

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            ValidateEntries().ConfigureAwait(false).GetAwaiter().GetResult();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            await ValidateEntries(cancellationToken);
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private async Task ValidateEntries(CancellationToken cancellationToken = default)
        {
            if (_validatorFactory == null)
                return;

            var addedOrModifiedEntries = ChangeTracker.Entries()
                .Where(x => x.State == EntityState.Added || x.State == EntityState.Modified)
                .ToList();
            foreach (var entry in addedOrModifiedEntries)
            {
                var validator = _validatorFactory.GetValidator(entry.Metadata.ClrType);
                if (validator == null)
                    continue;
                var validationContext = ValidationContext<object>.CreateWithOptions(entry.Entity, options => options.IncludeRuleSets(RuleSetNames.All));
                var validationResult = await validator.ValidateAsync(validationContext, cancellationToken);
                if (validationResult.IsValid == false)
                    throw new ValidationException(validationResult.Errors);
            }
        }

        #endregion
    }
}

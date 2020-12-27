using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF.Configuration
{
    public class ApplicationIdentityRoleConfig : IEntityTypeConfiguration<ApplicationIdentityRole>
    {
        public void Configure(EntityTypeBuilder<ApplicationIdentityRole> builder)
        {
            builder.Property(x => x.Id).HasConversion<string>();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Editions;

namespace Szlem.Persistence.EF.Configuration.Editions
{
    public class EditionConfig : IEntityTypeConfiguration<Edition>
    {
        public void Configure(EntityTypeBuilder<Edition> builder)
        {
            builder.ToTable("Edition");

            builder.Property(x => x.ThisEditionStatistics).HasSimpleJsonConversion();
            builder.Property(x => x.CumulativeStatistics).HasSimpleJsonConversion();
        }
    }
}

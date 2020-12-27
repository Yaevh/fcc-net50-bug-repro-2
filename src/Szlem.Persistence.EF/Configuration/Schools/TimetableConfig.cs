using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Schools;

namespace Szlem.Persistence.EF.Configuration.Schools
{
    class TimetableConfig : IEntityTypeConfiguration<Timetable>
    {
        public void Configure(EntityTypeBuilder<Timetable> builder)
        {
            builder.Property(x => x.Id).HasColumnName("ID");
            builder.Ignore(x => x.Days);
            builder.Property(x => x.TimeSlots).HasSimpleJsonConversion();
            builder.Property(x => x.Lessons).HasSimpleJsonConversion();
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Schools;

namespace Szlem.Persistence.EF.Configuration
{
    class ContactPersonConfig : IEntityTypeConfiguration<ContactPerson>
    {
        public void Configure(EntityTypeBuilder<ContactPerson> builder)
        {
            builder.Property(x => x.PhoneNumber).HasPhoneNumberConversion();
        }
    }
}

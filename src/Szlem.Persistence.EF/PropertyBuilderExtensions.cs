using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Persistence.EF
{
    public static class PropertyBuilderExtensions
    {
        public static PropertyBuilder<TProperty> HasSimpleJsonConversion<TProperty>(this PropertyBuilder<TProperty> builder)
        {
            return builder.HasConversion(
                x => JsonConvert.SerializeObject(x),
                x => JsonConvert.DeserializeObject<TProperty>(x));
        }

        public static PropertyBuilder<Domain.PhoneNumber> HasPhoneNumberConversion(this PropertyBuilder<Domain.PhoneNumber> builder)
        {
            return builder.HasConversion(
                x => x.ToString(),
                x => Domain.PhoneNumber.Create(x).Value);
        }

        public static PropertyBuilder<Domain.EmailAddress> HasEmailAddressConversion(this PropertyBuilder<Domain.EmailAddress> builder)
        {
            return builder.HasConversion(
                x => x.ToString(),
                x => Domain.EmailAddress.Create(x).Value);
        }
    }
}

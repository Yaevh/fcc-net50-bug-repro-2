using Microsoft.AspNetCore.Mvc.ModelBinding;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Common.Infrastructure.ModelBinders
{
    public class NodaTimeModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (context.Metadata.ModelType == typeof(LocalDate))
                return new NodaLocalDateModelBinder();
            else if (context.Metadata.ModelType == typeof(LocalDateTime))
                return new NodaLocalDateTimeModelBinder();
            else if (context.Metadata.ModelType == typeof(ZonedDateTime))
                return new NodaZonedDateTimeModelBinder();
            else if (context.Metadata.ModelType == typeof(OffsetDateTime))
                return new NodaOffsetDateTimeModelBinder();
            else if (context.Metadata.ModelType == typeof(Instant))
                return new NodaInstantModelBinder();
            return null;
        }
    }

    public class NodaLocalDateModelBinder : CustomModelBinderBase<LocalDate>
    {
        protected override bool TryParse(ValueProviderResult value, out LocalDate result)
        {
            if (DateTime.TryParse(value.Values, out DateTime dateTime))
            {
                result = LocalDate.FromDateTime(dateTime);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }

    public class NodaLocalDateTimeModelBinder : CustomModelBinderBase<LocalDateTime>
    {
        protected override bool TryParse(ValueProviderResult value, out LocalDateTime result)
        {
            if (DateTime.TryParse(value.Values, out DateTime dateTime))
            {
                result = LocalDateTime.FromDateTime(dateTime);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }

    public class NodaZonedDateTimeModelBinder : CustomModelBinderBase<ZonedDateTime>
    {
        protected override bool TryParse(ValueProviderResult value, out ZonedDateTime result)
        {
            if (DateTimeOffset.TryParse(value.Values, out DateTimeOffset dateTime))
            {
                result = ZonedDateTime.FromDateTimeOffset(dateTime);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }

    public class NodaOffsetDateTimeModelBinder : CustomModelBinderBase<OffsetDateTime>
    {
        protected override bool TryParse(ValueProviderResult value, out OffsetDateTime result)
        {
            if (DateTimeOffset.TryParse(value.Values, out DateTimeOffset dateTime))
            {
                result = OffsetDateTime.FromDateTimeOffset(dateTime);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }

    public class NodaInstantModelBinder : CustomModelBinderBase<Instant>
    {
        protected override bool TryParse(ValueProviderResult value, out Instant result)
        {
            if (DateTimeOffset.TryParse(value.Values, out DateTimeOffset dateTime))
            {
                result = OffsetDateTime.FromDateTimeOffset(dateTime).ToInstant();
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Models;

namespace Szlem.AspNetCore.Common.Infrastructure.ModelBinders
{
    public class PhoneNumberModelBinder : CustomModelBinderBase<PhoneNumber>
    {
        protected override bool TryParse(ValueProviderResult value, out PhoneNumber result)
        {
            return PhoneNumber.TryParse(value.Values, out result);
        }
    }

    public class PhoneNumberModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Metadata.ModelType == typeof(PhoneNumber))
                return new PhoneNumberModelBinder();
            return null;
        }
    }
}

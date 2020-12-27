using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Models;

namespace Szlem.AspNetCore.Common.Infrastructure.ModelBinders
{
    public class EmailAddressModelBinder : CustomModelBinderBase<EmailAddress>
    {
        protected override bool TryParse(ValueProviderResult value, out EmailAddress result)
        {
            return EmailAddress.TryParse(value.Values, out result);
        }
    }

    public class EmailAddressModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (context.Metadata.ModelType == typeof(EmailAddress))
                return new EmailAddressModelBinder();
            return null;
        }
    }
}

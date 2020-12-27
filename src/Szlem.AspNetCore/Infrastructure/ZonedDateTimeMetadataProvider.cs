using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Infrastructure
{
    public class ZonedDateTimeMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.Key.ModelType != typeof(NodaTime.ZonedDateTime))
                return;
            context.DisplayMetadata.EditFormatStringProvider = () => "{0:dd'.'MM'.'uuuu' 'HH':'mm}";
        }
    }
}

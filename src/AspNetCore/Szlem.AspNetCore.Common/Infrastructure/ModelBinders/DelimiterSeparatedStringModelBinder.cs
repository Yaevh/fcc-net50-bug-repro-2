using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Common.Infrastructure.ModelBinders
{
    public class DelimiterSeparatedStringModelBinderProvider : IModelBinderProvider
    {

        public IModelBinder GetBinder(ModelBinderProviderContext context)
        {
            if (context.Metadata.ModelType.Implements<IReadOnlyCollection<string>>() && context.Metadata.ElementType == typeof(string))
                return BuildModelBinder(context);
            return null;
        }

        private IModelBinder BuildModelBinder(ModelBinderProviderContext context)
        {
            return new DelimiterSeparatedStringModelBinder(new[] { ",", ";" });
        }
    }

    public class DelimiterSeparatedStringModelBinder : IModelBinder
    {
        private string[] _delimiters { get; }

        public DelimiterSeparatedStringModelBinder(IEnumerable<string> delimiters)
        {
            _delimiters = delimiters.ToArray();
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            var values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            bindingContext.ModelState.SetModelValue(bindingContext.ModelName, values);

            var result = values
                .SelectMany(x => x.Split(_delimiters, StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim());

            if (bindingContext.ModelType.IsArray)
                bindingContext.Result = ModelBindingResult.Success(result.ToArray());
            else
                bindingContext.Result = ModelBindingResult.Success(result.ToList());
            return Task.CompletedTask;
        }
    }
}

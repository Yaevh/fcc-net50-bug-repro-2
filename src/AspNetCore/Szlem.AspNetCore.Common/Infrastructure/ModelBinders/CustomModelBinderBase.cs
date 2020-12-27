using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Common.Infrastructure.ModelBinders
{
    public abstract class CustomModelBinderBase<T> : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));
            var data = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);

            if (string.IsNullOrEmpty(data.Values))
                return Task.CompletedTask;

            if (TryParse(data, out T result))
            {
                bindingContext.Result = ModelBindingResult.Success(result);
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, data);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    bindingContext.ModelMetadata.ModelBindingMessageProvider.AttemptedValueIsInvalidAccessor(
                        data.ToString(), bindingContext.FieldName));
            }

            return Task.CompletedTask;
        }

        protected abstract bool TryParse(ValueProviderResult value, out T result);
    }
}

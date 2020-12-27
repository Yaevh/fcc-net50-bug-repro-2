using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Common.Infrastructure
{
    /// <summary>
    /// Persists ModelState of a Razor page in TempData between redirects in POST-REDIRECT-GET pattern
    /// </summary>
    public class SerializeModelStateFilter : IPageFilter
    {
        public static readonly string Key = $"{nameof(SerializeModelStateFilter)}Data";
        public void OnPageHandlerSelected(PageHandlerSelectedContext context)
        {
            if (!(context.HandlerInstance is PageModel page))
                return;

            var serializedModelState = page.TempData[Key] as string;
            if (serializedModelState.IsNullOrEmpty())
                return;

            var modelState = ModelStateSerializer.Deserialize(serializedModelState);
            page.ModelState.Merge(modelState);
        }

        public void OnPageHandlerExecuting(PageHandlerExecutingContext context) { }

        public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
    }


    public static class ModelStateSerializer
    {
        private class ModelStateTransferValue
        {
            public string Key { get; set; }
            public string AttemptedValue { get; set; }
            public object RawValue { get; set; }
            public ICollection<string> ErrorMessages { get; set; } = new List<string>();
        }

        public static string Serialize(ModelStateDictionary modelState)
        {
            var errorList = modelState
                .Select(kvp => new ModelStateTransferValue
                {
                    Key = kvp.Key,
                    AttemptedValue = kvp.Value.AttemptedValue,
                    RawValue = kvp.Value.RawValue,
                    ErrorMessages = kvp.Value.Errors.Select(err => err.ErrorMessage).ToList(),
                });

            return System.Text.Json.JsonSerializer.Serialize(errorList);
        }

        public static ModelStateDictionary Deserialize(string serializedErrorList)
        {
            var errorList = System.Text.Json.JsonSerializer.Deserialize<List<ModelStateTransferValue>>(serializedErrorList);
            var modelState = new ModelStateDictionary();

            foreach (var item in errorList)
            {
                modelState.SetModelValue(item.Key, item.RawValue, item.AttemptedValue);
                foreach (var error in item.ErrorMessages)
                    modelState.AddModelError(item.Key, error);
            }
            return modelState;
        }
    }
}

namespace Microsoft.AspNetCore.Mvc
{
    public static class KeepTempDataResultExtensions
    {
        public static IKeepTempDataResult WithModelStateOf(this IKeepTempDataResult actionResult, PageModel page)
        {
            if (page.ModelState.IsValid)
                return actionResult;
            var modelState = Szlem.AspNetCore.Common.Infrastructure.ModelStateSerializer.Serialize(page.ModelState);
            page.TempData[Szlem.AspNetCore.Common.Infrastructure.SerializeModelStateFilter.Key] = modelState;
            return actionResult;
        }
    }
}

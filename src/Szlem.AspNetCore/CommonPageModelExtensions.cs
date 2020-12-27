using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Domain.Exceptions;

namespace Szlem.AspNetCore
{
    public static class CommonPageModelExtensions
    {
        public static void AddModelErrors(this ModelStateDictionary modelState, Error error, string requestPropertyName = null)
        {
            if (error is Error.ValidationFailed vf)
            {
                foreach (var failure in vf.Failures)
                    TryAddValidationFailure(modelState, failure, requestPropertyName);
                if (vf.Message.IsNullOrWhiteSpace() == false)
                    modelState.AddModelError(string.Empty, vf.Message);
            }
            else
            {
                modelState.AddModelError(string.Empty, error.Message);
            }
        }

        public static void AddModelErrors(this ModelStateDictionary modelState, ValidationFailure failure, string requestPropertyName = null)
        {
            TryAddValidationFailure(modelState, failure, requestPropertyName);
        }

        public static void Validate<T>(this ModelStateDictionary modelState, IValidator<T> validator, T instanceToValidate)
        {
            var result = validator.Validate(instanceToValidate);
            if (result.Errors.Any())
                modelState.AddModelErrors(new Error.ValidationFailed(result));
        }

        private static void TryAddValidationFailure(ModelStateDictionary modelState, ValidationFailure failure, string requestPropertyName = null)
        {
            foreach (var errorMessage in failure.Errors)
                TryAddValidationError(modelState, failure, errorMessage, requestPropertyName);
        }

        private static void TryAddValidationError(
            ModelStateDictionary modelState,
            ValidationFailure failure,
            string errorMessage,
            string requestPropertyName = null)
        {
            var key = GetPropertyName(failure, requestPropertyName);
            if (modelState.ContainsKey(key) && modelState[key].Errors.Any(x => x.ErrorMessage == errorMessage))
                return;
            if (requestPropertyName.IsNullOrWhiteSpace())
                modelState.AddModelError(failure.PropertyName, errorMessage);
            else
                modelState.AddModelError($"{requestPropertyName}.{failure.PropertyName}", errorMessage);
        }

        private static string GetPropertyName(ValidationFailure failure, string requestPropertyName = null)
        {
            if (requestPropertyName.IsNullOrWhiteSpace())
                return failure.PropertyName;
            else
                return $"{requestPropertyName}.{failure.PropertyName}";
        }
    }
}

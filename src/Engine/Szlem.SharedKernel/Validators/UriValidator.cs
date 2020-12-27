using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Text;

namespace FluentValidation
{
    public class UriValidator : PropertyValidator
    {
        public UriValidator() : this("'{PropertyName}' is not a valid URL.") { }

        public UriValidator(string message) : base(message) { }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            if (context.PropertyValue == null)
                return true;

            if (context.PropertyValue is Uri)
                return true;

            var uri = (string)context.PropertyValue;
            return Uri.TryCreate(uri, UriKind.Absolute, out _);
        }
    }

    public static partial class RuleBuilderExtensions
    {
        public static IRuleBuilderOptions<T, TProperty> Uri<T, TProperty>(this IRuleBuilder<T, TProperty> builder)
        {
            return builder.SetValidator(new UriValidator());
        }

        public static IRuleBuilderOptions<T, TProperty> Uri<T, TProperty>(this IRuleBuilder<T, TProperty> builder, string message)
        {
            return builder.SetValidator(new UriValidator(message));
        }
    }
}

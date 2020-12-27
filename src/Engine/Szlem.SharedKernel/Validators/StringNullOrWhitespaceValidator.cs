using FluentValidation.Validators;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FluentValidation
{
    public class StringNullOrWhitespaceValidator : PropertyValidator
    {
        public StringNullOrWhitespaceValidator() : this("'{PropertyName}' cannot be empty or whitespace only.") { }

        public StringNullOrWhitespaceValidator(string message) : base(message) { }

        protected override bool IsValid(PropertyValidatorContext context)
        {
            var value = context.PropertyValue as string;
            if (value.IsNullOrWhiteSpace())
                return false;
            else
                return true;
        }

        protected override Task<bool> IsValidAsync(PropertyValidatorContext context, CancellationToken cancellation) => Task.FromResult(IsValid(context));
    }

    public static partial class RuleBuilderExtensions
    {
        public static IRuleBuilderOptions<T, string> NotNullOrWhitespace<T>(this IRuleBuilder<T, string> builder)
        {
            return builder.SetValidator(new StringNullOrWhitespaceValidator());
        }

        public static IRuleBuilderOptions<T, string> NotNullOrWhitespace<T>(this IRuleBuilder<T, string> builder, string message)
        {
            return builder.SetValidator(new StringNullOrWhitespaceValidator(message));
        }
    }
}

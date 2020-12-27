using FluentValidation;
using FluentValidation.AspNetCore;
using FluentValidation.Internal;
using FluentValidation.Resources;
using FluentValidation.Validators;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore.Infrastructure.ClientValidators
{
    internal class PhoneNumberClientValidator : ClientValidatorBase
    {
        public PhoneNumberClientValidator(PropertyRule rule, IPropertyValidator validator) : base(rule, validator)
        {
        }

        public override void AddValidation(ClientModelValidationContext context)
        {
            var formatter = ValidatorOptions.MessageFormatterFactory().AppendPropertyName(Rule.GetDisplayName());

            string messageTemplate;
            try
            {
                messageTemplate = Validator.Options.ErrorMessageSource.GetString(null);
            }
            catch (FluentValidationMessageFormatException)
            {
                messageTemplate = ValidatorOptions.LanguageManager.GetStringForValidator<EmailValidator>();
            }

            string message = formatter.BuildMessage(messageTemplate);
            MergeAttribute(context.Attributes, "data-val", "true");
            MergeAttribute(context.Attributes, "data-val-phonePL", message);
        }
    }
}

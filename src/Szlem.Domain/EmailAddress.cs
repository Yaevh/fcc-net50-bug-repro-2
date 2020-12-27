using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;

namespace Szlem.Domain
{
    [TypeConverter(typeof(EmailAddress.TypeConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(EmailAddress.NewtonsoftJsonConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(EmailAddress.TextJsonConverter))]
    public class EmailAddress : ValueObject, IEquatable<EmailAddress>, IComparable<EmailAddress>
    {
        internal const string InvalidEmailAddress = "Błędny adres e-mail";

        private readonly System.Net.Mail.MailAddress _value;

        #region constructor

        protected internal EmailAddress(string address)
        {
            _value = new System.Net.Mail.MailAddress(address);
        }

        public static Result<EmailAddress> Create(string address)
        {
            if (address == null || address.Contains(" "))
                return Result.Failure<EmailAddress>(InvalidEmailAddress);

            try
            {
                var email = new EmailAddress(address);
                return Result.Success(email);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is FormatException)
            {
                return Result.Failure<EmailAddress>(InvalidEmailAddress);
            }
        }

        #endregion


        #region Equals() && GetHashCode()

        public bool Equals(EmailAddress other)
        {
            return this == other;
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return _value;
        }

        #endregion


        #region Parse()

        public static EmailAddress Parse(string source)
        {
            return new EmailAddress(source);
        }

        public static bool TryParse(string source, out EmailAddress address)
        {
            address = null;
            var result = Create(source);
            if (result.IsFailure)
                return false;
            address = result.Value;
            return true;
        }

        #endregion


        public override string ToString() => _value.Address;

        public int CompareTo(EmailAddress other)
        {
            if (other is null)
                return 1;
            else
                return _value.ToString().CompareTo(other._value.ToString());
        }

        public static implicit operator string(EmailAddress emailAddress) => emailAddress.ToString();




        private class TypeConverter : System.ComponentModel.TypeConverter
        {
            [Pure]
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            [Pure]
            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) =>
                value is string text ? EmailAddress.Parse(text) : base.ConvertFrom(context, culture, value);
        }

        private class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<EmailAddress>
        {
            public override EmailAddress ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, EmailAddress existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == Newtonsoft.Json.JsonToken.Null)
                    return null;
                else if (reader.TokenType == Newtonsoft.Json.JsonToken.String && EmailAddress.TryParse((string)reader.Value, out EmailAddress address))
                    return address;
                else
                    throw new Newtonsoft.Json.JsonSerializationException($@"Value ""{reader.Value}"" is not a valid {nameof(EmailAddress)}");
            }

            public override void WriteJson(Newtonsoft.Json.JsonWriter writer, EmailAddress value, Newtonsoft.Json.JsonSerializer serializer)
            {
                serializer.Serialize(writer, value.ToString());
            }
        }

        private class TextJsonConverter : System.Text.Json.Serialization.JsonConverter<EmailAddress>
        {
            public override EmailAddress Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
                    return null;
                else if (reader.TokenType == System.Text.Json.JsonTokenType.String && EmailAddress.TryParse(reader.GetString(), out EmailAddress address))
                    return address;
                else
                    throw new System.Text.Json.JsonException($"Current value  is not a valid {nameof(EmailAddress)}");
            }

            public override void Write(System.Text.Json.Utf8JsonWriter writer, EmailAddress value, System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        public class Validator : FluentValidation.Validators.PropertyValidator
        {
            protected override bool IsValid(FluentValidation.Validators.PropertyValidatorContext context)
            {
                if (context.PropertyValue is null)
                    return true;
                if (context.PropertyValue is EmailAddress) // EmailAddress is always valid
                    return true;
                if (TryParse(context.PropertyValue as string, out EmailAddress _))
                    return true;
                return false;
            }
        }
    }

    public static partial class RuleBuilderExtensions
    {
        public static FluentValidation.IRuleBuilderOptions<T, string> EmailAddress<T>(this FluentValidation.IRuleBuilder<T, string> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new EmailAddress.Validator());
        }

        public static FluentValidation.IRuleBuilderOptions<T, EmailAddress> EmailAddress<T>(this FluentValidation.IRuleBuilder<T, EmailAddress> ruleBuilder)
        {
            return ruleBuilder.SetValidator(new EmailAddress.Validator());
        }
    }
}

using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Text;
using System.Text.RegularExpressions;

namespace Szlem.Domain
{
    [TypeConverter(typeof(PhoneNumber.TypeConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(PhoneNumber.NewtonsoftJsonConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(PhoneNumber.TextJsonConverter))]
    public class PhoneNumber : ValueObject, IEquatable<PhoneNumber>
    {
        internal const string InvalidPhoneNumber_Message = "Błędny numer telefonu";

        private readonly PhoneNumbers.PhoneNumber _value;
        private static readonly PhoneNumbers.PhoneNumberUtil _util = PhoneNumbers.PhoneNumberUtil.GetInstance();


        #region constructor & factory

        protected internal PhoneNumber(string number)
        {
            _value = _util.Parse(number, GetCurrentCultureCode());
            Validate();
        }

        private void Validate()
        {
            if (_util.IsValidNumber(_value) == false)
                throw new FormatException();
        }

        public static Result<PhoneNumber> Create(string number)
        {
            try
            {
                var phoneNumber = new PhoneNumber(number);
                return Result.Success(phoneNumber);
            }
            catch (Exception ex) when (ex is FormatException || ex is PhoneNumbers.NumberParseException)
            {
                return Result.Failure<PhoneNumber>(InvalidPhoneNumber_Message);
            }
        }
        
        #endregion


        #region ToString()

        public override string ToString()
        {
            return _util.Format(_value, PhoneNumbers.PhoneNumberFormat.INTERNATIONAL);
        }

        public string ToString(PhoneNumbers.PhoneNumberFormat format)
        {
            return _util.Format(_value, format);
        }

        public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.ToString();

        #endregion


        #region Equals() && GetHashCode()

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return _value;
        }
        
        public bool Equals(PhoneNumber other)
        {
            return this == other;
        }

        #endregion


        #region Parse()

        public static PhoneNumber Parse(string source)
        {
            return new PhoneNumber(source);
        }

        public static bool TryParse(string source, out PhoneNumber phoneNumber)
        {
            phoneNumber = null;
            var result = Create(source);
            if (result.IsFailure)
                return false;
            phoneNumber = result.Value;
            return result.IsSuccess;
        }

        #endregion


        private string GetCurrentCultureCode() => new System.Globalization.CultureInfo("pl-PL").TwoLetterISOLanguageName.ToUpperInvariant();


        private class TypeConverter : System.ComponentModel.TypeConverter
        {
            [Pure]
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            [Pure]
            public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) =>
                value is string text ? PhoneNumber.Parse(text) : base.ConvertFrom(context, culture, value);
        }

        private class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter<PhoneNumber>
        {
            public override PhoneNumber ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, PhoneNumber existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == Newtonsoft.Json.JsonToken.Null)
                    return null;
                else if (reader.TokenType == Newtonsoft.Json.JsonToken.String && PhoneNumber.TryParse((string)reader.Value, out PhoneNumber phoneNumber))
                    return phoneNumber;
                else
                    throw new Newtonsoft.Json.JsonSerializationException($@"Value ""{reader.Value}"" is not a valid {nameof(PhoneNumber)}");
            }

            public override void WriteJson(Newtonsoft.Json.JsonWriter writer, PhoneNumber value, Newtonsoft.Json.JsonSerializer serializer)
            {
                serializer.Serialize(writer, value.ToString());
            }
        }

        private class TextJsonConverter : System.Text.Json.Serialization.JsonConverter<PhoneNumber>
        {
            public override PhoneNumber Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType == System.Text.Json.JsonTokenType.Null)
                    return null;
                else if (reader.TokenType == System.Text.Json.JsonTokenType.String && PhoneNumber.TryParse(reader.GetString(), out PhoneNumber phoneNumber))
                    return phoneNumber;
                else
                    throw new System.Text.Json.JsonException($"Current value  is not a valid {nameof(PhoneNumber)}");
            }

            public override void Write(System.Text.Json.Utf8JsonWriter writer, PhoneNumber value, System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStringValue(System.Text.Json.JsonEncodedText.Encode(value.ToString(), System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping));
            }
        }
    }
}

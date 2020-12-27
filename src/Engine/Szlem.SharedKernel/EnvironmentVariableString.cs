using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Szlem.SharedKernel
{
    /// <summary>
    /// Represents a string containing environment variables, that can be automatically expanded
    /// (meaning a process in which environment variable instances in a string are replaced
    /// with their respective values)
    /// </summary>
    [System.ComponentModel.TypeConverter(typeof(TypeConverter))]
    [System.Text.Json.Serialization.JsonConverter(typeof(SystemTextConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(NewtonsoftConverter))]
    public class EnvironmentVariableString : IEquatable<EnvironmentVariableString>
    {
        public EnvironmentVariableString(string inputString)
        {
            RawValue = inputString ?? string.Empty;
            ExpandedValue = Environment.ExpandEnvironmentVariables(RawValue);
        }

        public string RawValue { get; }

        public string ExpandedValue { get; }


        public override string ToString() => ExpandedValue;

        public static implicit operator string(EnvironmentVariableString source) => source?.ToString();

        public static implicit operator EnvironmentVariableString(string source) => new EnvironmentVariableString(source);


        #region GetHashCode & Equals

        public override int GetHashCode() => HashCode.Combine(RawValue, ExpandedValue);

        public override bool Equals(object obj)
        {
            if (obj is EnvironmentVariableString evs)
                return this == evs;
            else
                return false;
        }

        public bool Equals(EnvironmentVariableString other)
        {
            return RawValue == other?.RawValue;
        }

        public static bool operator ==(EnvironmentVariableString left, EnvironmentVariableString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EnvironmentVariableString left, EnvironmentVariableString right)
        {
            return !(left == right);
        }

        #endregion

        private class TypeConverter : System.ComponentModel.TypeConverter
        {
            [System.Diagnostics.Contracts.Pure]
            public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) => sourceType == typeof(string);

            [System.Diagnostics.Contracts.Pure]
            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) =>
                value is string text ? new EnvironmentVariableString(text) : base.ConvertFrom(context, culture, value);
        }

        private class SystemTextConverter : System.Text.Json.Serialization.JsonConverter<EnvironmentVariableString>
        {
            public override EnvironmentVariableString Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                if (reader.TokenType == System.Text.Json.JsonTokenType.String)
                    return new EnvironmentVariableString(reader.GetString());
                else
                    throw new System.Text.Json.JsonException($"Current value  is not a valid {nameof(EnvironmentVariableString)}");
            }

            public override void Write(System.Text.Json.Utf8JsonWriter writer, EnvironmentVariableString value, System.Text.Json.JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }

        private class NewtonsoftConverter : Newtonsoft.Json.JsonConverter<EnvironmentVariableString>
        {
            public override EnvironmentVariableString ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, [AllowNull] EnvironmentVariableString existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                if (reader.TokenType == Newtonsoft.Json.JsonToken.String)
                    return new EnvironmentVariableString((string)reader.Value);
                else
                    throw new Newtonsoft.Json.JsonSerializationException($@"Value ""{reader.Value}"" is not a valid {nameof(EnvironmentVariableString)}");
            }

            public override void WriteJson(Newtonsoft.Json.JsonWriter writer, [AllowNull] EnvironmentVariableString value, Newtonsoft.Json.JsonSerializer serializer)
            {
                serializer.Serialize(writer, value.ToString());
            }
        }
    }
}

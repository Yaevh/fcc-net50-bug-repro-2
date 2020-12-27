using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Szlem.SharedKernel
{
    public static class MaybeConverters
    {
        public class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
        {
            private static readonly Type _maybeGenericType = typeof(Maybe<>);

            public override bool CanConvert(Type objectType)
            {
                return objectType.IsConstructedGenericType && objectType.GetGenericTypeDefinition() == _maybeGenericType;
            }

            public override object ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
            {
                switch (reader.TokenType)
                {
                    case Newtonsoft.Json.JsonToken.Null:
                        return objectType.GetProperty(nameof(Maybe<object>.None), BindingFlags.Public | BindingFlags.Static).GetValue(null);
                    default:
                        var deserialized = serializer.Deserialize(reader);
                        return objectType.GetMethod(nameof(Maybe<object>.From), BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { deserialized });
                }
            }

            public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
            {
                var hasValue = (bool)value.GetType().GetProperty(nameof(Maybe<object>.HasValue)).GetValue(value);

                if (hasValue)
                {
                    var underlyingValue = value.GetType().GetProperty(nameof(Maybe<object>.Value)).GetValue(value);
                    serializer.Serialize(writer, underlyingValue);
                }
                else
                {
                    serializer.Serialize(writer, null);
                }
            }
        }


        public class SystemTextJsonConverterFactory : System.Text.Json.Serialization.JsonConverterFactory
        {
            private static readonly Type _maybeGenericType = typeof(Maybe<>);
            private static readonly Type _converterGenericType = typeof(SystemTextJsonConverter<>);
            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert.IsConstructedGenericType && typeToConvert.GetGenericTypeDefinition() == _maybeGenericType;
            }

            public override System.Text.Json.Serialization.JsonConverter CreateConverter(Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                return Activator.CreateInstance(_converterGenericType.MakeGenericType(typeToConvert.GetGenericArguments())) as System.Text.Json.Serialization.JsonConverter;
            }
        }


        public class SystemTextJsonConverter<T> : System.Text.Json.Serialization.JsonConverter<Maybe<T>>
        {
            public override Maybe<T> Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
            {
                switch (reader.TokenType)
                {
                    case System.Text.Json.JsonTokenType.Null:
                        return Maybe<T>.None;
                    default:
                        var deserialized = System.Text.Json.JsonSerializer.Deserialize<T>(ref reader, options);
                        return Maybe<T>.From(deserialized);
                }
            }

            public override void Write(System.Text.Json.Utf8JsonWriter writer, Maybe<T> value, System.Text.Json.JsonSerializerOptions options)
            {
                if (value.HasValue)
                {
                    System.Text.Json.JsonSerializer.Serialize(writer, value.Value, options);
                }
                else
                {
                    writer.WriteNullValue();
                }
            }
        }
    }
}

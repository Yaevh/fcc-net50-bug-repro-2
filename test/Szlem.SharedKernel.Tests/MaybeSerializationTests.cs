using CSharpFunctionalExtensions;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.SharedKernel.Tests
{
    public class MaybeSerializationTests
    {
        [Fact]
        public void Newtonsoft__Maybe_None_serializes_properly()
        {
            var maybe = Maybe<string>.None;
            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(maybe, new MaybeConverters.NewtonsoftJsonConverter());
            Assert.Equal("null", serialized);
        }

        [Fact]
        public void Newtonsoft__Maybe_Some_serializes_properly()
        {
            var maybe = Maybe<string>.From("ala ma kota");
            var serialized = Newtonsoft.Json.JsonConvert.SerializeObject(maybe, new MaybeConverters.NewtonsoftJsonConverter());
            Assert.Equal("\"ala ma kota\"", serialized);
        }

        [Fact]
        public void Newtonsoft__Maybe_None_deserializes_properly()
        {
            var serialized = "null";
            var maybe = Newtonsoft.Json.JsonConvert.DeserializeObject<Maybe<string>>(serialized, new MaybeConverters.NewtonsoftJsonConverter());

            Assert.True(maybe.HasNoValue);
        }

        [Fact]
        public void Newtonsoft__Maybe_Some_deserializes_properly()
        {
            var serialized = "\"ala ma kota\"";
            var maybe = Newtonsoft.Json.JsonConvert.DeserializeObject<Maybe<string>>(serialized, new MaybeConverters.NewtonsoftJsonConverter());

            Assert.True(maybe.HasValue);
            Assert.Equal("ala ma kota", maybe.Value);
        }


        [Fact]
        public void SystemTextJson__Maybe_None_serializes_properly()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new MaybeConverters.SystemTextJsonConverterFactory());

            var maybe = Maybe<string>.None;
            var serialized = System.Text.Json.JsonSerializer.Serialize(maybe, options);
            Assert.Equal("null", serialized);
        }

        [Fact]
        public void SystemTextJson__Maybe_Some_serializes_properly()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new MaybeConverters.SystemTextJsonConverterFactory());

            var maybe = Maybe<string>.From("ala ma kota");
            var serialized = System.Text.Json.JsonSerializer.Serialize(maybe, options);
            Assert.Equal("\"ala ma kota\"", serialized);
        }

        [Fact]
        public void SystemTextJson__Maybe_None_deserializes_properly()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new MaybeConverters.SystemTextJsonConverterFactory());

            var serialized = "null";
            var maybe = System.Text.Json.JsonSerializer.Deserialize<Maybe<string>>(serialized, options);

            Assert.True(maybe.HasNoValue);
        }

        [Fact]
        public void SystemTextJson__Maybe_Some_deserializes_properly()
        {
            var options = new System.Text.Json.JsonSerializerOptions();
            options.Converters.Add(new MaybeConverters.SystemTextJsonConverterFactory());

            var serialized = "\"ala ma kota\"";
            var maybe = System.Text.Json.JsonSerializer.Deserialize<Maybe<string>>(serialized, options);

            Assert.True(maybe.HasValue);
            Assert.Equal("ala ma kota", maybe.Value);
        }
    }
}

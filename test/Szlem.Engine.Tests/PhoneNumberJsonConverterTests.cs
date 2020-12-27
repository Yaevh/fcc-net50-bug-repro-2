using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Xunit;

using NewtonsoftSerializer = Newtonsoft.Json.JsonConvert;
using MicrosoftSerializer = System.Text.Json.JsonSerializer;

namespace Szlem.Engine.Tests
{
    public class PhoneNumberJsonConverterTests
    {
        [Theory(DisplayName = "Proper PhoneNumber serializes correctly (using Newtonsoft.Json)")]
        [InlineData("575-5520-98", "+48 575 552 098")]
        [InlineData("575 5520 98", "+48 575 552 098")]
        [InlineData("575 552 098", "+48 575 552 098")]
        [InlineData("001-541-754-3010", "+1 541-754-3010")]
        [InlineData("+49-89-636-48018", "+49 89 63648018")]
        public void ProperPhoneNumber_SerializesCorrectly_UsingNewtonsoftJson(string number, string expectedNumber)
        {
            var phoneNumber = new PhoneNumber(number);

            var serialized = NewtonsoftSerializer.SerializeObject(phoneNumber);

            Assert.Equal($"\"{expectedNumber}\"", serialized);
        }

        [Theory(DisplayName = "Proper PhoneNumber deserializes correctly (using Newtonsoft.Json)")]
        [InlineData("575-5520-98")]
        [InlineData("575 5520 98")]
        [InlineData("001-541-754-3010")]
        [InlineData("+49-89-636-48018")]
        public void ProperPhoneNumber_DeserializesCorrectly_UsingNewtonsoftJson(string number)
        {
            var phoneNumber = new PhoneNumber(number);
            var serialized = NewtonsoftSerializer.SerializeObject(phoneNumber);

            var deserialized = NewtonsoftSerializer.DeserializeObject<PhoneNumber>(serialized);

            Assert.Equal(phoneNumber, deserialized);
        }

        [Theory(DisplayName = "Proper PhoneNumber serializes correctly (using System.Text.Json)")]
        [InlineData("575-5520-98", "+48 575 552 098")]
        [InlineData("575 5520 98", "+48 575 552 098")]
        [InlineData("575 552 098", "+48 575 552 098")]
        [InlineData("001-541-754-3010", "+1 541-754-3010")]
        [InlineData("+49-89-636-48018", "+49 89 63648018")]
        public void ProperPhoneNumber_SerializesCorrectly_UsingSystemTextJson(string number, string expectedNumber)
        {
            var phoneNumber = new PhoneNumber(number);

            var serialized = MicrosoftSerializer.Serialize(phoneNumber);

            Assert.Equal($"\"{expectedNumber}\"", serialized);
        }

        [Theory(DisplayName = "Proper PhoneNumber deserializes correctly (using System.Text.Json)")]
        [InlineData("575-5520-98")]
        [InlineData("001-541-754-3010")]
        [InlineData("+49-89-636-48018")]
        public void ProperPhoneNumber_DeserializesCorrectly_UsingSystemTextJson(string number)
        {
            var phoneNumber = new PhoneNumber(number);
            var serialized = MicrosoftSerializer.Serialize(phoneNumber);

            var deserialized = MicrosoftSerializer.Deserialize<PhoneNumber>(serialized);

            Assert.Equal(phoneNumber, deserialized);
        }
    }
}

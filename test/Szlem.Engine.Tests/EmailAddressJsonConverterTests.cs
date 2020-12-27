using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Xunit;

using NewtonsoftSerializer = Newtonsoft.Json.JsonConvert;
using MicrosoftSerializer = System.Text.Json.JsonSerializer;

namespace Szlem.Engine.Tests
{
    public class EmailAddressJsonConverterTests
    {
        [Theory(DisplayName = "Proper Email serializes correctly (using Newtonsoft.Json)")]
        [InlineData("andrzej@strzelba.pl")]
        public void ProperEmail_SerializesCorrectly_UsingNewtonsoftJson(string address)
        {
            var emailAddress = new EmailAddress(address);

            var serialized = NewtonsoftSerializer.SerializeObject(emailAddress);

            Assert.Equal($"\"{address}\"", serialized);
        }

        [Theory(DisplayName = "Proper Email deserializes correctly (using Newtonsoft.Json)")]
        [InlineData("andrzej@strzelba.pl")]
        public void ProperEmail_DeserializesCorrectly_UsingNewtonsoftJson(string address)
        {
            var email = new EmailAddress(address);
            var serialized = NewtonsoftSerializer.SerializeObject(email);

            var deserialized = NewtonsoftSerializer.DeserializeObject<EmailAddress>(serialized);

            Assert.Equal(email, deserialized);
        }

        [Theory(DisplayName = "Proper Email serializes correctly (using System.Text.Json)")]
        [InlineData("andrzej@strzelba.pl")]
        public void ProperEmail_SerializesCorrectly_UsingSystemTextJson(string address)
        {
            var emailAddress = new EmailAddress(address);

            var serialized = MicrosoftSerializer.Serialize(emailAddress);

            Assert.Equal($"\"{address}\"", serialized);
        }

        [Theory(DisplayName = "Proper Email deserializes correctly (using System.Text.Json)")]
        [InlineData("andrzej@strzelba.pl")]
        public void ProperEmail_DeserializesCorrectly_UsingSystemTextJson(string address)
        {
            var email = new EmailAddress(address);
            var serialized = MicrosoftSerializer.Serialize(email);

            var deserialized = MicrosoftSerializer.Deserialize<EmailAddress>(serialized);

            Assert.Equal(email, deserialized);
        }
    }
}

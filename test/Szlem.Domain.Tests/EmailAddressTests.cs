using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Domain.Tests
{
    public class EmailAddressTests
    {
        [Theory(DisplayName = "Creating EmailAddress with proper address succeeds")]
        [InlineData("andrzej@strzelba.pl")]
        public void CreatingEmailAddressWithProperInput_Succeeds(string address)
        {
            var result = EmailAddress.Create(address);

            Assert.True(result.IsSuccess);
        }

        [Theory(DisplayName = "Creating EmailAddress with invalid address fails")]
        [InlineData((string)null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData("alamakota")]
        [InlineData("andrzej strzelba@mises.pl", Skip = "poprawić parsowanie adresów ze spacjami")]
        public void CreatingEmailAddressWithInvalidInput_Fails(string address)
        {
            var result = EmailAddress.Create(address);

            Assert.True(result.IsFailure);
            Assert.Equal(EmailAddress.InvalidEmailAddress, result.Error);
        }
    }
}

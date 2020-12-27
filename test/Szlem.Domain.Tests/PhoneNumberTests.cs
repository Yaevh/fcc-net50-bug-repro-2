using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Domain.Tests
{
    public class PhoneNumberTests
    {
        [Theory(DisplayName = "Creating PhoneNumber with proper number succeeds")]
        [InlineData("575-5520-98")]
        public void CreatingPhoneNumberWithProperInput_Succeeds(string number)
        {
            var result = PhoneNumber.Create(number);

            Assert.True(result.IsSuccess);
        }

        [Theory(DisplayName = "Creating PhoneNumber with invalid number fails")]
        [InlineData((string)null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("\n")]
        [InlineData("alamakota")]
        [InlineData("123456789012345678901234567890")]
        public void CreatingPhoneNumberWithInvalidInput_Fails(string number)
        {
            var result = PhoneNumber.Create(number);

            Assert.True(result.IsFailure);
        }
    }
}

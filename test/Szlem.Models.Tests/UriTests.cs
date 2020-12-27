using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Models.Tests
{
    public class UriTests
    {
        [Theory]
        [InlineData("http://ilo.gda.pl")]
        public void ProperUri_ShouldParse(string uriString)
        {
            var success = Uri.TryCreate(uriString, UriKind.Absolute, out Uri uri);

            Assert.True(success);
        }

        [Theory]
        [InlineData("ilo.gda.pl")]
        [InlineData("ala ma kota")]
        [InlineData("szkola@ilo.gda.pl")]
        public void InvalidUri_ShouldNotParse(string uriString)
        {
            var success = Uri.TryCreate(uriString, UriKind.Absolute, out Uri uri);

            Assert.False(success);
        }
    }
}

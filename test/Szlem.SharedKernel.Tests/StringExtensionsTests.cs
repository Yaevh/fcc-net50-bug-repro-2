using System;
using Xunit;

namespace Szlem.SharedKernel.Tests
{
    public class StringExtensionsTests
    {
        #region TruncateWithEllipsis() tests

        [Theory]
        [InlineData("abcdef", 4, "…", "abc…")]
        [InlineData("abcdef", 3, "…", "ab…")]
        [InlineData("abcdef", 2, "…", "a…")]
        [InlineData("abcdef", 1, "…", "…")]
        [InlineData("abcdef", 5, "...", "ab...")]
        [InlineData("abcdef", 4, "...", "a...")]
        [InlineData("abcdef", 3, "...", "...")]
        public void TruncateWithEllipsis_truncates_string_properly(string s, int maxLength, string ellipsis, string expected)
        {
            var result = s.TruncateWithEllipsis(maxLength, ellipsis);
            Assert.Equal(expected, result);
            Assert.True(result.Length == maxLength);
        }

        [Theory]
        [InlineData("abcdef", 6, "…")]
        [InlineData("abcdef", 7, "…")]
        [InlineData("abcdef", 65538, "…")]
        [InlineData("abcdef", 6, "...")]
        public void TruncateWithEllipsis_returns_the_same_string_when_the_string_is_shorter_than_maxLength(string s, int maxLength, string ellipsis)
        {
            var result = s.TruncateWithEllipsis(maxLength, ellipsis);
            Assert.Equal(s, result);
            Assert.True(result.Length <= maxLength);
        }

        [Theory]
        [InlineData("abcdef", 0, "…")]
        [InlineData("abcdef", 0, "...")]
        [InlineData("abcdef", 2, "...")]
        public void TruncateWithEllipsis_throws_InvalidOperationException_when_ellipsis_is_longer_than_maxLength(string s, int maxLength, string ellipsis)
        {
            Assert.Throws<InvalidOperationException>(() => s.TruncateWithEllipsis(maxLength, ellipsis));
        }

        #endregion
    }
}

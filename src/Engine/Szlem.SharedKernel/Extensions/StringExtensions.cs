using Ardalis.GuardClauses;
using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static string TrimEnd(this string s, string substring)
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            if (s.EndsWith(substring) == false)
                throw new ArgumentException($"{s} does not end with {substring}");

            var index = s.LastIndexOf(substring);
            return s.Substring(0, index);
        }


        public static string TruncateWithEllipsis(this string s, int maxLength, string ellipsis = "…")
        {
            Guard.Against.Null(s, nameof(s));
            Guard.Against.Null(ellipsis, nameof(ellipsis));
            if (maxLength < ellipsis.Length)
                throw new InvalidOperationException($"{nameof(maxLength)} cannot be lower than {nameof(ellipsis)}.{nameof(ellipsis.Length)}");
            if (s.Length <= maxLength)
                return s;
            return $"{s.Substring(0, maxLength - ellipsis.Length)}{ellipsis}";
        }

        public static bool ContainsCaseInsensitive(this string s, string substring)
        {
            return s.IndexOf(substring, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }
    }
}

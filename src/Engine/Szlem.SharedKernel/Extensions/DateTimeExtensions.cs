using System;
using System.Collections.Generic;
using System.Text;

namespace System
{
    public static class DateTimeExtensions
    {
        public static string ToHtmlDateValue(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }
    }
}

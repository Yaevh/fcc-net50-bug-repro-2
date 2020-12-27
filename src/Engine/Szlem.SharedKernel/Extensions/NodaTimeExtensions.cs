using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace NodaTime
{
    public static class NodaTimeExtensions
    {
        public static bool Overlaps(this DateInterval first, DateInterval second)
        {
            if (first is null)
                throw new ArgumentNullException(nameof(first));
            if (second is null)
                throw new ArgumentNullException(nameof(second));

            return first.Contains(second.Start) || first.Contains(second.End)
                || second.Contains(first.Start) || second.Contains(first.End);
        }

        public static bool Overlaps(this Interval first, Interval second)
        {
            return first.Contains(second.Start) || first.Contains(second.End)
                || second.Contains(first.Start) || second.Contains(first.End);
        }
    }
}

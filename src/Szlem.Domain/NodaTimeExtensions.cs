using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Domain
{
    public static class NodaTimeExtensions
    {
        public static ZonedDateTime GetZonedDateTime(this IClock clock) => clock.GetCurrentInstant().InMainTimezone();
        public static OffsetDateTime GetOffsetDateTime(this IClock clock) => clock.GetZonedDateTime().ToOffsetDateTime();
        public static LocalDateTime GetLocalDateTime(this IClock clock) => clock.GetZonedDateTime().LocalDateTime;
        public static LocalDate GetTodayDate(this IClock clock) => clock.GetZonedDateTime().Date;

        public static ZonedDateTime InMainTimezone(this LocalDateTime localDateTime) => localDateTime.InZoneStrictly(Consts.MainTimezone);
        public static ZonedDateTime InMainTimezone(this Instant instant) => instant.InZone(Consts.MainTimezone);
    }
}

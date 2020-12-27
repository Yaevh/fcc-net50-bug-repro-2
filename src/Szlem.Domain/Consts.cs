using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Domain
{
    public static class Consts
    {
        public static readonly NodaTime.DateTimeZone MainTimezone = NodaTime.DateTimeZoneProviders.Tzdb["Europe/Warsaw"];

        public static readonly PhoneNumber FakePhoneNumber = PhoneNumber.Parse("+48 575-5520-98");
    }
}

using Newtonsoft.Json;
using NodaTime.Serialization.JsonNet;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Engine.Tests
{
    public class NodaTimeTests
    {
        [Fact]
        public void OssfetDateTime_CanBeSerializedAndDeserialized()
        {
            var clock = new NodaTime.ZonedClock(NodaTime.SystemClock.Instance, Szlem.Domain.Consts.MainTimezone, NodaTime.CalendarSystem.Iso);
            var dateTime = clock.GetCurrentOffsetDateTime();
            var serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);
            var serialized = JsonConvert.SerializeObject(dateTime, serializerSettings);

            var deserialized = JsonConvert.DeserializeObject<NodaTime.OffsetDateTime>(serialized, serializerSettings);

            Assert.Equal(dateTime, deserialized);
        }

        [Fact]
        public void PeriodCanBeSerializedAndDeserialized()
        {
            var serializerSettings = new JsonSerializerSettings().ConfigureForNodaTime(NodaTime.DateTimeZoneProviders.Tzdb);

            var builder = new NodaTime.PeriodBuilder();
            builder.Days = 1;
            builder.Weeks = 1;
            builder.Months = 1;
            var period = builder.Build();

            var serialized = JsonConvert.SerializeObject(period, serializerSettings);

            var deserialized = JsonConvert.DeserializeObject<NodaTime.Period>(serialized);

            Assert.Equal(period, deserialized);
        }
    }
}

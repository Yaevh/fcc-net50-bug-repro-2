using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;

namespace Szlem.Engine.Infrastructure
{
    public class MockableClock : IClock
    {
        private readonly IClock _realClock;

        public MockableClock(IClock realClock)
        {
            _realClock = realClock ?? throw new ArgumentNullException(nameof(realClock));
        }


        public Instant? MockNow { get; set; }
        public ZonedDateTime DateTime => this.GetZonedDateTime();
        public bool IsMocked => MockNow.HasValue;
        public void RestoreRealTime() => MockNow = null;

        public Instant GetCurrentInstant() => MockNow ?? _realClock.GetCurrentInstant();

    }
}

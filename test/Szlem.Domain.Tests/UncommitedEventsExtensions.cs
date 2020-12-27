using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Domain.Tests
{
    public static class UncommitedEventsExtensions
    {
        public static void AssertHasSingleEvent<TEvent>(this IEnumerable<IUncommittedEvent> events, Action<TEvent> assert)
        {
            Assert.Single(events);
            Assert.Collection(events, single =>
            {
                var @event = Assert.IsType<TEvent>(single.AggregateEvent);
                assert.Invoke(@event);
            });
        }
    }
}

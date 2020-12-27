using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace Szlem.SchoolManagement.Impl.Events
{
    [EventVersion("Szlem.SchoolManagement.School.SchoolRegistered", 1)]
    internal class SchoolRegistered : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Instant Timestamp { get; }
        public Guid RegisteringUserId { get; }
        public string Name { get; }
        public string City { get; }
        public string Address { get; }
        public IReadOnlyCollection<ContactData> ContactData { get; }

        public SchoolRegistered(Instant timestamp, Guid registeringUserId, string name, string city, string address, IReadOnlyCollection<ContactData> contactData)
        {
            Guard.Against.Default(timestamp, nameof(timestamp));
            Guard.Against.Default(registeringUserId, nameof(registeringUserId));
            Guard.Against.NullOrWhiteSpace(name, nameof(name));
            Guard.Against.NullOrWhiteSpace(city, nameof(city));
            Guard.Against.NullOrWhiteSpace(address, nameof(address));
            Guard.Against.NullOrEmpty(contactData, nameof(contactData));

            Timestamp = timestamp;
            RegisteringUserId = registeringUserId;
            Name = name;
            City = city;
            Address = address;
            ContactData = contactData ?? throw new ArgumentNullException(nameof(contactData));
        }
    }
}
#nullable restore

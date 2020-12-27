using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using static Szlem.SchoolManagement.RecordContact;

#nullable enable
namespace Szlem.SchoolManagement.Impl.Events
{
    [EventVersion("Szlem.SchoolManagement.School.ContactOccured", 1)]
    internal class ContactOccured : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Guid RecordingUserId { get; }
        public Instant ContactTimestamp { get; }
        public CommunicationChannelType CommunicationChannel { get; }
        public EmailAddress? EmailAddress { get; }
        public PhoneNumber? PhoneNumber { get; }

        /// <summary>
        /// Imię i nazwisko lub stanowisko osoby z którą doszło do kontaktu
        /// </summary>
        public string ContactPersonName { get; } = string.Empty;

        public string Content { get; } = string.Empty;
        public string? AdditionalNotes { get; }
        

        public ContactOccured(
            Guid recordingUserId,
            Instant contactTimestamp,
            CommunicationChannelType communicationChannel,
            EmailAddress? emailAddress,
            PhoneNumber? phoneNumber,
            string contactPersonName,
            string content,
            string? additionalNotes)
        {
            Guard.Against.Default(recordingUserId, nameof(recordingUserId));
            Guard.Against.Default(contactTimestamp, nameof(contactTimestamp));
            Guard.Against.Default(communicationChannel, nameof(communicationChannel));
            Guard.Against.NullOrWhiteSpace(contactPersonName, nameof(contactPersonName));
            Guard.Against.NullOrWhiteSpace(content, nameof(content));

            RecordingUserId = recordingUserId;
            ContactTimestamp = contactTimestamp;
            CommunicationChannel = communicationChannel;
            EmailAddress = emailAddress;
            PhoneNumber = phoneNumber;
            ContactPersonName = contactPersonName;
            Content = content;
            AdditionalNotes = additionalNotes;
        }
    }
}

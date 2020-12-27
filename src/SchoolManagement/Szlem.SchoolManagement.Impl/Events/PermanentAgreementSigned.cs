using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace Szlem.SchoolManagement.Impl.Events
{
    [EventVersion("Szlem.SchoolManagement.School.PermanentAgreementSigned", 1)]
    internal class PermanentAgreementSigned : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Guid Id { get; }
        public byte[] ScannedDocument { get; }
        public string ScannedDocumentExtension { get; }
        public string ScannedDocumentContentType { get; }
        public Guid RecordingUserId { get; }
        public string? AdditionalNotes { get; }

        public PermanentAgreementSigned(
            Guid id,
            byte[] scannedDocument,
            string scannedDocumentExtension,
            string scannedDocumentContentType,
            Guid recordingUserId,
            string? additionalNotes)
        {
            Guard.Against.Default(id, nameof(id));
            Guard.Against.Empty(scannedDocument, nameof(scannedDocument));
            Guard.Against.NullOrWhiteSpace(scannedDocumentExtension, nameof(scannedDocumentExtension));
            Guard.Against.NullOrWhiteSpace(scannedDocumentContentType, nameof(scannedDocumentContentType));
            Guard.Against.Default(recordingUserId, nameof(recordingUserId));

            Id = id;
            ScannedDocument = scannedDocument;
            ScannedDocumentExtension = scannedDocumentExtension;
            ScannedDocumentContentType = scannedDocumentContentType;
            RecordingUserId = recordingUserId;
            AdditionalNotes = additionalNotes;
        }
    }
}

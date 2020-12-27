using Ardalis.GuardClauses;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace Szlem.SchoolManagement.Impl.Events
{
    [EventVersion("Szlem.SchoolManagement.School.FixedTermAgreementSigned", 1)]
    internal class FixedTermAgreementSigned : AggregateEvent<SchoolAggregate, SchoolId>
    {
        public Guid Id { get; }
        public byte[] ScannedDocument { get; }
        public string ScannedDocumentExtension { get; }
        public string ScannedDocumentContentType { get; }
        public NodaTime.LocalDate AgreementEndDate { get; }
        public Guid RecordingUserId { get; }
        public string? AdditionalNotes { get; }

        public FixedTermAgreementSigned(
            Guid id,
            byte[] scannedDocument,
            string scannedDocumentExtension,
            string scannedDocumentContentType,
            NodaTime.LocalDate agreementEndDate,
            Guid recordingUserId,
            string? additionalNotes)
        {
            Guard.Against.Default(id, nameof(id));
            Guard.Against.Empty(scannedDocument, nameof(scannedDocument));
            Guard.Against.NullOrWhiteSpace(scannedDocumentExtension, nameof(scannedDocumentExtension));
            Guard.Against.NullOrWhiteSpace(scannedDocumentContentType, nameof(scannedDocumentContentType));
            Guard.Against.Default(agreementEndDate, nameof(agreementEndDate));
            Guard.Against.Default(recordingUserId, nameof(recordingUserId));

            Id = id;
            ScannedDocument = scannedDocument;
            ScannedDocumentExtension = scannedDocumentExtension;
            ScannedDocumentContentType = scannedDocumentContentType;
            AgreementEndDate = agreementEndDate;
            RecordingUserId = recordingUserId;
            AdditionalNotes = additionalNotes;
        }
    }
}

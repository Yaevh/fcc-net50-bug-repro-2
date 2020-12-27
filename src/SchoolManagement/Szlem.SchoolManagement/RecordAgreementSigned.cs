using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using NodaTime;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

#nullable enable
namespace Szlem.SchoolManagement
{
    public static class RecordAgreementSigned
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Guid, Error>>
        {
            public Guid SchoolId { get; set; }
            public byte[] ScannedDocument { get; set; } = Array.Empty<byte>();
            public string ScannedDocumentExtension { get; set; } = string.Empty;
            public string ScannedDocumentContentType { get; set; } = string.Empty;
            [Display(Name = "Okres umowy")] public AgreementDuration? Duration { get; set; }
            [Display(Name = "Data zakończenia umowy (w przypadku umowy na czas określony)")] public LocalDate? AgreementEndDate { get; set; }
            [Display(Name = "Dodatkowe uwagi i notatki")] public string? AdditionalNotes { get; set; }
        }

        public enum AgreementDuration
        {
            [Display(Name = "Umowa terminowa (na czas określony)")] FixedTerm = 1,
            [Display(Name = "Umowa na czas nieokreślony")] Permanent = 2
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(RecordAgreementSigned_Messages.SchoolId_cannot_be_empty);
                RuleFor(x => x.ScannedDocument).NotEmpty().WithMessage(RecordAgreementSigned_Messages.ScannedDocument_cannot_be_empty);
                RuleFor(x => x.ScannedDocument.Length).LessThanOrEqualTo(10 * 1024 * 1024).When(x => x.ScannedDocument != null)
                    .WithMessage(RecordAgreementSigned_Messages.ScannedDocument_cannot_be_bigger_than_10MB);
                RuleFor(x => x.ScannedDocumentExtension).NotEmpty()
                    .WithMessage(RecordAgreementSigned_Messages.ScannedDocumentExtension_cannot_be_empty);
                RuleFor(x => x.ScannedDocumentExtension)
                    .Must(x => x.StartsWith(".")).When(x => x.ScannedDocumentExtension != null)
                    .WithMessage(RecordAgreementSigned_Messages.ScannedDocumentExtension_must_start_with_a_dot);
                RuleFor(x => x.ScannedDocumentContentType).NotEmpty()
                    .WithMessage(RecordAgreementSigned_Messages.ScannedDocumentExtension_cannot_be_empty);
                RuleFor(x => x.Duration).NotEmpty().WithMessage(RecordAgreementSigned_Messages.Duration_cannot_be_empty);
                RuleFor(x => x.AgreementEndDate).NotEmpty().When(x => x.Duration == AgreementDuration.FixedTerm)
                    .WithMessage(RecordAgreementSigned_Messages.AgreementEndDate_cannot_be_empty);
                RuleFor(x => x.AgreementEndDate).Empty().When(x => x.Duration == AgreementDuration.Permanent)
                    .WithMessage(RecordAgreementSigned_Messages.PermanentAgreement_cannot_have_AgreementEndDate);
            }
        }
    }
}

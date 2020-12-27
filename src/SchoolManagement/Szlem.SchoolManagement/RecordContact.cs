using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

#nullable enable
namespace Szlem.SchoolManagement
{
    public static class RecordContact
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid SchoolId { get; set; }

            [Display(Name = "Data i czas kontaktu")]
            public NodaTime.Instant ContactTimestamp { get; set; }

            [Display(Name = "Kanał komunikacji")]
            public CommunicationChannelType CommunicationChannel { get; set; } = CommunicationChannelType.Unknown;

            [Display(Name = "Adres e-mail kontaktu (jeśli kontakt odbył się mailowo)")]
            public EmailAddress? EmailAddress { get; set; }

            [Display(Name = "Numer telefoniczny kontaktu (jeśli kontakt odbył się telefonicznie)")]
            public PhoneNumber? PhoneNumber { get; set; }
            
            [Display(Name = "Imię i nazwisko lub stanowisko osoby z którą doszło do kontaktu")]
            public string ContactPersonName { get; set; } = string.Empty;

            [Display(Name = "Treść/opis kontaktu")]
            public string Content { get; set; } = string.Empty;

            [Display(Name = "Dodatkowe informacje i notatki")]
            public string? AdditionalNotes { get; set; }
        }


        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(RecordContact_Messages.SchoolId_cannot_be_empty);
                RuleFor(x => x.ContactTimestamp).NotEmpty().WithMessage(RecordContact_Messages.Timestamp_cannot_be_empty);
                RuleFor(x => x.CommunicationChannel).NotEmpty().WithMessage(RecordContact_Messages.CommunicationChannel_cannot_be_empty);
                RuleFor(x => x.CommunicationChannel).NotEqual(CommunicationChannelType.Unknown)
                    .WithMessage(RecordContact_Messages.CommunicationChannel_cannot_be_empty);
                RuleFor(x => x.ContactPersonName).NotEmpty().WithMessage(RecordContact_Messages.ContactPersonName_cannot_be_empty);
                RuleFor(x => x.Content).NotEmpty().WithMessage(RecordContact_Messages.Content_cannot_be_empty);

                RuleFor(x => x.EmailAddress).NotEmpty().When(x => x.CommunicationChannel.IsEmail)
                    .WithMessage(RecordContact_Messages.EmailAddress_cannot_be_empty_when_CommunicationChannel_is_IncomingEmail_or_OutgoingEmail);
                RuleFor(x => x.EmailAddress).EmailAddress().When(x => x.CommunicationChannel.IsEmail)
                    .WithMessage(Messages.Invalid_email_address);
                RuleFor(x => x.PhoneNumber).NotEmpty().When(x => x.CommunicationChannel.IsPhone)
                    .WithMessage(RecordContact_Messages.PhoneNumber_cannot_be_empty_when_CommunicationChannelType_is_IncomingPhone_or_OutgoingPhone);
            }
        }
    }
}

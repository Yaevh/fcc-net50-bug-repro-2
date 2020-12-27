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
    public static class EditNote
    {
        [Authorize(AuthorizationPolicies.OwningCoordinatorOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid SchoolId { get; set; }
            public Guid NoteId { get; set; }
            [Display(Name = "Treść")] public string Content { get; set; } = string.Empty;
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(EditNote_Messages.SchoolId_cannot_be_empty);
                RuleFor(x => x.NoteId).NotEmpty().WithMessage(EditNote_Messages.NoteId_cannot_be_empty);
                RuleFor(x => x.Content).NotNullOrWhitespace().WithMessage(EditNote_Messages.Content_cannot_be_empty);
            }
        }
    }
}

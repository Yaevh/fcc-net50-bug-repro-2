using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

namespace Szlem.SchoolManagement
{
    public static class DeleteNote
    {
        [Authorize(AuthorizationPolicies.OwningCoordinatorOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public Guid SchoolId { get; set; }
            public Guid NoteId { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(DeleteNote_Messages.SchoolId_cannot_be_empty);
                RuleFor(x => x.NoteId).NotEmpty().WithMessage(DeleteNote_Messages.NoteId_cannot_be_empty);
            }
        }
    }
}

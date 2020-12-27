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
    public static class AddNote
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Guid, Error>>
        {
            public Guid SchoolId { get; set; }
            [Display(Name = "Treść")] public string? Content { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.SchoolId).NotEmpty().WithMessage(AddNote_Messages.SchoolId_cannot_be_empty);
                RuleFor(x => x.Content).NotNullOrWhitespace().WithMessage(AddNote_Messages.Content_cannot_be_empty);
            }
        }
    }
}

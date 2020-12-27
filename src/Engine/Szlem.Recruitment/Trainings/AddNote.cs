using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Domain;
using Szlem.SharedKernel;

#nullable enable
namespace Szlem.Recruitment.Trainings
{
    public static class AddNote
    {
        [Authorize(AuthorizationPolicies.CoordinatorsOnly)]
        public class Command : IRequest<Result<Nothing, Error>>
        {
            public int TrainingId { get; set; }
            public string Content { get; set; } = "brak treści";
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.TrainingId).NotEmpty();
                RuleFor(x => x.Content).NotEmpty();
            }
        }
    }
}

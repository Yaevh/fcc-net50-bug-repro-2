using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.UserManagement
{
    public static class RevokeRoleUseCase
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Command : IRequest
        {
            public Guid UserID { get; set; }

            public string RoleName { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.UserID).NotEmpty();
                RuleFor(x => x.RoleName).NotEmpty();
            }
        }
    }
}

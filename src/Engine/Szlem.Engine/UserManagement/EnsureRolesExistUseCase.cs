using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Users;
using Szlem.SharedKernel;

namespace Szlem.Engine.UserManagement
{
    public static class EnsureRolesExistUseCase
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Command : IRequest<Result>
        {
            public IReadOnlyCollection<ApplicationIdentityRole> RoleNames { get; set; } = new ApplicationIdentityRole[0];
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.RoleNames).NotNull();
                RuleForEach(x => x.RoleNames).NotNull();
            }
        }

        public class Result
        {
            public IReadOnlyCollection<ApplicationIdentityRole> ExistingRoles { get; set; } = new ApplicationIdentityRole[0];
            public IReadOnlyCollection<ApplicationIdentityRole> CreatedRoles { get; set; } = new ApplicationIdentityRole[0];
        }
    }
}

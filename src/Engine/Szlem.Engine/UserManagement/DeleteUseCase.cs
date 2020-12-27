using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;
using Szlem.SharedKernel;

namespace Szlem.Engine.UserManagement
{
    public static class DeleteUseCase
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Command : IRequest
        {
            public Guid UserID { get; set; }
        }

        public class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(x => x.UserID).NotEmpty();
            }
        }

        internal class CommandHandler : IRequestHandler<Command>
        {
            private readonly UserManager<ApplicationUser> _userManager;

            public CommandHandler(UserManager<ApplicationUser> userManager)
            {
                _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            }


            public async Task<Unit> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _userManager.FindByIdAsync(request.UserID.ToString());
                if (user == null)
                    throw new Exceptions.ResourceNotFoundException();

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded == false)
                    throw new SzlemException(string.Join(", ", result.Errors.Select(x => x.Description)));

                return Unit.Value;
            }
        }
    }
}

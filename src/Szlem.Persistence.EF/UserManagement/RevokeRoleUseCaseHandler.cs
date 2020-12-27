using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Exceptions;
using Szlem.Engine.UserManagement;
using Szlem.Models.Users;
using Szlem.Domain.Exceptions;

namespace Szlem.Persistence.EF.UserManagement
{
    internal class RevokeRoleUseCaseHandler : IRequestHandler<RevokeRoleUseCase.Command>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RevokeRoleUseCaseHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<Unit> Handle(RevokeRoleUseCase.Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserID.ToString());
            if (user == null)
                throw new InvalidRequestException();

            var result = await _userManager.RemoveFromRoleAsync(user, request.RoleName);
            if (result.Succeeded == false)
                throw new SzlemException(string.Join(", ", result.Errors.Select(x => x.Description)));
            
            return Unit.Value;
        }
    }
}

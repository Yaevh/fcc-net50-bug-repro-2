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
    internal class GrantRoleUseCaseHandler : IRequestHandler<GrantRoleUseCase.Command>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationIdentityRole> _roleManager;

        public GrantRoleUseCaseHandler(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationIdentityRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<Unit> Handle(GrantRoleUseCase.Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserID.ToString());
            if (user == null)
                throw new InvalidRequestException();

            request.RoleName = _roleManager.NormalizeKey(request.RoleName);

            var role = await _roleManager.FindByNameAsync(_roleManager.NormalizeKey(request.RoleName));
            if (role == null)
                throw new InvalidRequestException();

            var result = await _userManager.AddToRoleAsync(user, role.Name);
            if (result.Succeeded == false)
                throw new SzlemException(string.Join(", ", result.Errors.Select(x => x.Description)));

            return Unit.Value;
        }
    }
}

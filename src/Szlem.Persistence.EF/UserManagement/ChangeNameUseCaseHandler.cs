using MediatR;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Exceptions;
using Szlem.Engine.UserManagement;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF.UserManagement
{
    internal class ChangeNameUseCaseHandler : IRequestHandler<ChangeNameUseCase.Command>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public ChangeNameUseCaseHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<Unit> Handle(ChangeNameUseCase.Command request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserID.ToString());
            if (user == null)
                throw new InvalidRequestException();

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            var result = await _userManager.UpdateAsync(user);
            return Unit.Value;
        }
    }
}

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
using Szlem.Domain.Exceptions;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF.UserManagement
{
    internal class EnsureRolesExistUseCaseHandler : IRequestHandler<EnsureRolesExistUseCase.Command, EnsureRolesExistUseCase.Result>
    {
        private readonly AppDbContext _dbContext;
        private readonly RoleManager<ApplicationIdentityRole> _roleManager;

        public EnsureRolesExistUseCaseHandler(AppDbContext dbContext, RoleManager<ApplicationIdentityRole> roleManager)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<EnsureRolesExistUseCase.Result> Handle(EnsureRolesExistUseCase.Command request, CancellationToken cancellationToken)
        {
            var existingRoles = new List<ApplicationIdentityRole>();
            var createdRoles = new List<ApplicationIdentityRole>();

            foreach (var role in request.RoleNames)
            {
                if (await _roleManager.RoleExistsAsync(role.Name))
                {
                    existingRoles.Add(role);
                }
                else
                {
                    var result = await _roleManager.CreateAsync(role);
                    if (result.Succeeded)
                        createdRoles.Add(role);
                    else
                        throw new SzlemException(string.Join(", ", result.Errors.Select(x => x.Description)));
                }
            }

            return new EnsureRolesExistUseCase.Result() { CreatedRoles = createdRoles, ExistingRoles = existingRoles };
        }
    }
}

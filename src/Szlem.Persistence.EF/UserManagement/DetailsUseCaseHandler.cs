using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Exceptions;
using Szlem.Engine.Infrastructure;
using Szlem.Engine.UserManagement;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF.UserManagement
{
    internal class DetailsUseCaseHandler : IRequestHandler<DetailsUseCase.Query, DetailsUseCase.UserDetails>
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationIdentityRole> _roleManager;
        private readonly IRequestAuthorizationAnalyzer _authorizer;

        public DetailsUseCaseHandler(
            AppDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationIdentityRole> roleManager,
            IRequestAuthorizationAnalyzer authorizer)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
        }

        public async Task<DetailsUseCase.UserDetails> Handle(DetailsUseCase.Query request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new ResourceNotFoundException();

            var roleNames = await _userManager.GetRolesAsync(user);
            var roles = await _roleManager.Roles.Where(x => roleNames.Contains(x.Name)).ToListAsync();
            
            return new DetailsUseCase.UserDetails()
            {
                ID = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Email = user.Email,
                Roles = (await roles.SelectAsync(x => BuildRole(x, user))).ToArray(),
                CanChangeName = (await _authorizer.Authorize(new ChangeNameUseCase.Command() { UserID = user.Id })).Succeeded,
                CanGrantRoles = (await _authorizer.Authorize(new GrantRoleUseCase.Command() { UserID = user.Id })).Succeeded,
                CanDelete = (await _authorizer.Authorize(new DeleteUseCase.Command() { UserID = user.Id })).Succeeded,
            };
        }

        private async Task<DetailsUseCase.UserDetails.UserRole> BuildRole(ApplicationIdentityRole role, ApplicationUser user)
        {
            return new DetailsUseCase.UserDetails.UserRole()
            {
                Name = role.Name,
                Description = role.Description,
                CanRevoke = (await _authorizer.Authorize(new RevokeRoleUseCase.Command() { UserID = user.Id, RoleName = role.Name })).Succeeded
            };
        }
    }

    internal class GetAllRolesQueryHandler : IRequestHandler<DetailsUseCase.GetRolesThatCanBeGrantedQuery, IReadOnlyList<DetailsUseCase.RoleData>>
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationIdentityRole> _roleManager;

        public GetAllRolesQueryHandler(UserManager<ApplicationUser> userManager, RoleManager<ApplicationIdentityRole> roleManager)
        {
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        }

        public async Task<IReadOnlyList<DetailsUseCase.RoleData>> Handle(DetailsUseCase.GetRolesThatCanBeGrantedQuery request, CancellationToken cancellationToken)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
                throw new ResourceNotFoundException();

            var roleNames = await _userManager.GetRolesAsync(user);
            var roles = await _roleManager.Roles.Where(x => roleNames.Contains(x.Name) == false).ToListAsync();
            return roles.Select(x => new DetailsUseCase.RoleData() { Name = x.Name, Description = x.Description }).ToList();
        }
    }
}

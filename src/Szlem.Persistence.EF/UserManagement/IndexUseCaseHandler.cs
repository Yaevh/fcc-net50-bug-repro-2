using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Infrastructure;
using Szlem.Engine.UserManagement;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF.UserManagement
{
    public class IndexUseCaseHandler : IRequestHandler<IndexUseCase.Query, IReadOnlyList<IndexUseCase.UserSummary>>
    {
        private readonly AppDbContext _dbContext;
        private readonly IRequestAuthorizationAnalyzer _authorizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public IndexUseCaseHandler(AppDbContext dbContext, IRequestAuthorizationAnalyzer authorizer, UserManager<ApplicationUser> userManager)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _authorizer = authorizer ?? throw new ArgumentNullException(nameof(authorizer));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public async Task<IReadOnlyList<IndexUseCase.UserSummary>> Handle(IndexUseCase.Query request, CancellationToken cancellationToken)
        {
            var users = await _dbContext.Users.AsNoTracking().ToListAsync();
            return (await users.SelectAsync(x => ToUserData(x))).ToList();
        }

        private async Task<IndexUseCase.UserSummary> ToUserData(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            return new IndexUseCase.UserSummary()
            {
                ID = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                CanShowDetails = (await _authorizer.Authorize(new DetailsUseCase.Query() { UserId = user.Id })).Succeeded,
                CanDelete = (await _authorizer.Authorize(new DeleteUseCase.Command() { UserID = user.Id })).Succeeded,
                Roles = roles.ToList(),
                CanGrantRoles = (await _authorizer.Authorize(new GrantRoleUseCase.Command() { UserID = user.Id })).Succeeded,
                CanRevokeRoles = (await _authorizer.Authorize(new RevokeRoleUseCase.Command() { UserID = user.Id })).Succeeded
            };
        }
    }
}

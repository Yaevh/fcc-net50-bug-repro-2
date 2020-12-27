using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.Models.Users;

namespace Szlem.Persistence.EF
{
    internal class ApplicationUserClaimPrincipalFactory : IUserClaimsPrincipalFactory<ApplicationUser>
    {
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _baseImpl;
        public ApplicationUserClaimPrincipalFactory(IUserClaimsPrincipalFactory<ApplicationUser> baseImpl)
        {
            _baseImpl = baseImpl ?? throw new ArgumentNullException(nameof(baseImpl));
        }

        public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            var principal = await _baseImpl.CreateAsync(user);
            principal.AddIdentity(new ClaimsIdentity(new[] { new Claim(nameof(user.FullName), user.FullName) }));
            return principal;
        }
    }
}

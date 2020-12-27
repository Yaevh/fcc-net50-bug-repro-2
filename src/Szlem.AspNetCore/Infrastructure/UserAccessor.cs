using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Szlem.Engine.Interfaces;
using Szlem.Models.Users;

namespace Szlem.AspNetCore.Infrastructure
{
    public class UserAccessor : IUserAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserAccessor(IHttpContextAccessor httpContextAccessor, UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        }

        public Task<ClaimsPrincipal> GetClaimsPrincipal()
        {
            return Task.FromResult(_httpContextAccessor.HttpContext?.User);
        }

        public async Task<ApplicationUser> GetUser()
        {
            var claimsPrincipal = await GetClaimsPrincipal();
            if (claimsPrincipal == null)
                return null;

            return await _userManager.GetUserAsync(claimsPrincipal);
        }
    }
}

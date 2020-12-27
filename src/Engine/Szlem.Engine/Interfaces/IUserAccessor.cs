using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.Models.Users;

namespace Szlem.Engine.Interfaces
{
    public interface IUserAccessor
    {
        Task<ApplicationUser> GetUser();
        Task<ClaimsPrincipal> GetClaimsPrincipal();
    }
}

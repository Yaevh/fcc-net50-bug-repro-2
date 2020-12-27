using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Szlem.Models.Users;
using Szlem.SharedKernel;
using Szlem.AspNetCore.Infrastructure;

namespace Szlem.AspNetCore.Areas.Admin
{
    public class AreaConfig : IRazorPagesConventionBuilder
    {        
        public void Configure(PageConventionCollection conventions)
        {
            conventions.AuthorizeAreaFolder(Consts.AreaName, "/", AuthorizationPolicies.AdminOnly);
        }
    }
}

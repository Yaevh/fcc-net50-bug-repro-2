using Hangfire.Annotations;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using Szlem.Engine.Interfaces;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Common.Infrastructure
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IAuthorizationService _authService;
        public HangfireDashboardAuthorizationFilter(IAuthorizationService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        public bool Authorize([NotNull] DashboardContext context)
        {
            var user = context.GetHttpContext().User;
            return _authService.AuthorizeAsync(user, AuthorizationPolicies.AdminOnly).ConfigureAwait(false).GetAwaiter().GetResult().Succeeded;
        }
    }
}

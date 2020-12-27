using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Middleware
{
    public class SwaggerAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAuthorizationService _authorizationService;

        public SwaggerAuthorizationMiddleware(RequestDelegate next, IAuthorizationService authorizationService)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments("/swagger") == false)
            {
                await _next.Invoke(context);
                return;
            }

            var authorizationResult = await _authorizationService.AuthorizeAsync(context.User, AuthorizationPolicies.CanAccessSwagger);
            if (authorizationResult.Succeeded)
                await _next.Invoke(context);
            else if (context.User.Identity.IsAuthenticated)
                await context.ForbidAsync();
            else
                await context.ChallengeAsync();
        }
    }
}

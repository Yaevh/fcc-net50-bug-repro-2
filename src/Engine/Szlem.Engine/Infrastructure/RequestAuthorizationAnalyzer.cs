using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.Domain;
using Szlem.Engine.Interfaces;
using Szlem.SharedKernel;

namespace Szlem.Engine.Infrastructure
{
    public static class AuthorizationServiceExtensions
    {
        public static async Task<Result<Nothing, Error>> AuthorizeAsResult(
            this IAuthorizationService authService,
            ClaimsPrincipal claimsPrincipal, object resource, AuthorizationPolicy policy)
        {
            var result = await authService.AuthorizeAsync(claimsPrincipal, resource, policy);
            if (result.Succeeded)
                return Result.Success<Nothing, Error>(Nothing.Value);
            else
                return Result.Failure<Nothing, Error>(new Error.AuthorizationFailed());
        }

        public static async Task<AuthorizationResult> Authorize<TRequest>(
            this IAuthorizationService authService,
            ClaimsPrincipal claimsPrincipal, TRequest request, object resource)
        {
            var attributes = request.GetType().GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>()
                .ToList();
            var policies = attributes.Where(x => x.Policy != null).Select(x => x.Policy).ToList();
            var roles = attributes
                .Where(x => x.Roles != null)
                .SelectMany(x => x.Roles.Split(','))
                .Select(x => x.Trim())
                .ToList();

            // no authorization needed, proceed
            if (policies.Any() == false && roles.Any() == false)
                return AuthorizationResult.Success();

            foreach (var policy in policies)
            {
                var result = await authService.AuthorizeAsync(claimsPrincipal, resource, policy);
                if (result.Succeeded == false)
                    return result;
            }

            if (roles.Any())
            {
                var requirement = new RolesAuthorizationRequirement(roles);
                var result = await authService.AuthorizeAsync(claimsPrincipal, request, requirement);
                if (result.Succeeded == false)
                    return result;
            }

            return AuthorizationResult.Success();
        }

        public static async Task<Result<Nothing, Error>> AuthorizeAsResult(
            this IAuthorizationService authService,
            ClaimsPrincipal claimsPrincipal, object resource, string policy)
        {
            var result = await authService.AuthorizeAsync(claimsPrincipal, resource, policy);
            if (result.Succeeded)
                return Result.Success<Nothing, Error>(Nothing.Value);
            else
                return Result.Failure<Nothing, Error>(new Error.AuthorizationFailed());
        }
    }

    public class RequestAuthorizationAnalyzer : IRequestAuthorizationAnalyzer
    {
        private readonly IUserAccessor _userAccessor;
        private readonly IAuthorizationService _authorizationService;

        public RequestAuthorizationAnalyzer(IUserAccessor userAccessor, IAuthorizationService authorizationService)
        {
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }


        public async Task<AuthorizationResult> Authorize<TRequest>(TRequest request)
        {
            return await _authorizationService.Authorize(await _userAccessor.GetClaimsPrincipal(), request, resource: null);
        }
    }
}

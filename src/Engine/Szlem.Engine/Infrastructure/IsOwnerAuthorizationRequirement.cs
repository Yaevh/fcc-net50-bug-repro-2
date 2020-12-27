using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.Engine.Infrastructure
{
    public interface IHaveOwner
    {
        bool IsOwner(Guid userId);
    }


    public class IsOwnerAuthorizationRequirement : IAuthorizationRequirement { }

    public class IsOwnerAuthorizationHandler : AuthorizationHandler<IsOwnerAuthorizationRequirement, IHaveOwner>
    {
        public override async Task HandleAsync(AuthorizationHandlerContext context)
        {
            if (context.User == null)
                return;
            
            if (context.Resource is null || context.Resource is IHaveOwner)
            {
                foreach (var req in context.Requirements.OfType<IsOwnerAuthorizationRequirement>())
                {
                    await HandleRequirementAsync(context, req, (IHaveOwner)context.Resource);
                }
            }
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, IsOwnerAuthorizationRequirement requirement, IHaveOwner resource)
        {
            var claims = context.User.Claims.Where(x => x.Type == ClaimTypes.NameIdentifier).ToList();
            if (claims.Count > 1)
                throw new InvalidOperationException(
                    $"expected single claim of type {ClaimTypes.NameIdentifier}, but found {claims.Count}.\n" +
                    $"maybe you use multiple authentication methods?\n" +
                    $"found claim values:" +
                    $"\n{string.Join("\n", claims.Select(x => x.Value))}"
                    );

            var id = claims.FirstOrDefault();
            if (id == null)
                return Task.CompletedTask;

            if (Guid.TryParse(id.Value, out var guid))
            {
                if (resource == null)
                    context.Succeed(requirement);
                else if (resource.IsOwner(guid))
                    context.Succeed(requirement);
                else
                    context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}

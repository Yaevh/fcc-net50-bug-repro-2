using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.Engine.Infrastructure;
using Xunit;

namespace Szlem.Engine.Tests
{
    public class IsOwnerAuthorizationRequirementTests
    {
        [Fact(DisplayName = "Jeśli nie podano użytkownika, handler nie przepuszcza")]
        public async Task When_no_user_is_provided__handler_does_not_authorize()
        {
            var context = new AuthorizationHandlerContext(new[] { new IsOwnerAuthorizationRequirement() }, null, null);
            await new IsOwnerAuthorizationHandler().HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact(DisplayName = "Jeśli resource.IsOwner() zwraca False, handler nie przepuszcza")]
        public async Task When_resource_decides_user_is_not_owner__handler_does_not_authorize()
        {
            var guid = Guid.NewGuid();
            var context = new AuthorizationHandlerContext(
                new[] { new IsOwnerAuthorizationRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, guid.ToString()) })),
                new OwnedResource(Guid.NewGuid()));
            await new IsOwnerAuthorizationHandler().HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact(DisplayName = "Jeśli resource.IsOwner() zwraca True, handler autoryzuje")]
        public async Task When_resource_decides_user_is_the_owner__handler_authorizes()
        {
            var guid = Guid.NewGuid();
            var context = new AuthorizationHandlerContext(
                new[] { new IsOwnerAuthorizationRequirement() },
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, guid.ToString()) })),
                new OwnedResource(guid));
            await new IsOwnerAuthorizationHandler().HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }


        public class OwnedResource : IHaveOwner
        {
            private readonly Guid _ownerId;
            public OwnedResource(Guid ownerId) => _ownerId = ownerId;

            public bool IsOwner(Guid guid) => _ownerId == guid;
        }
    }
}

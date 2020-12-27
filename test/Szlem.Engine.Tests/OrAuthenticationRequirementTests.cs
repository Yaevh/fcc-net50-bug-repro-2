using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Szlem.Engine.Tests
{
    public class OrAuthenticationRequirementTests
    {
        private IAuthorizationRequirement SucceedingRequirement => new AssertionRequirement(ctx => true);
        private IAuthorizationRequirement FailingRequirement => new AssertionRequirement(ctx => false);

        private ClaimsPrincipal BuildClaimsPrincipalWithClaims(params Claim[] claims) => new ClaimsPrincipal(new[] { new ClaimsIdentity(claims) });

        [Fact(DisplayName = "Jeśli wszystkie subrequirementy zawiodą, OrRequirement zawodzi")]
        public async Task When_all_subRequirements_fail__OrRequirement_fails()
        {
            var handler = new OrAuthorizationRequirementHandler(
                new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions() { InvokeHandlersAfterFailure = false }),
                new Lazy<IAuthorizationHandlerProvider>(new DefaultAuthorizationHandlerProvider(new[] { new PassThroughAuthorizationHandler() })),
                new DefaultAuthorizationHandlerContextFactory());
            var requirement = FailingRequirement.Or(FailingRequirement);
            var context = new AuthorizationHandlerContext(new[] { requirement }, BuildClaimsPrincipalWithClaims(), null);

            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli pierwszy subrequirement się uda, OrRequirement udaje się")]
        public async Task When_first_subRequirement_succeeds__OrRequirement_succeeds()
        {
            var handler = new OrAuthorizationRequirementHandler(
                new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions() { InvokeHandlersAfterFailure = false }),
                new Lazy<IAuthorizationHandlerProvider>(new DefaultAuthorizationHandlerProvider(new[] { new PassThroughAuthorizationHandler() })),
                new DefaultAuthorizationHandlerContextFactory());
            var requirement = SucceedingRequirement.Or(FailingRequirement);
            var context = new AuthorizationHandlerContext(new[] { requirement }, BuildClaimsPrincipalWithClaims(), null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli pierwszy subrequirement się uda, drugi subrequirement nie jest rozwiązywany")]
        public async Task When_first_subRequirement_succeeds__second_subRequirement_is_never_resolved()
        {
            bool secondRequirementEvaluated = false;
            var handler = new OrAuthorizationRequirementHandler(
                new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions() { InvokeHandlersAfterFailure = false }),
                new Lazy<IAuthorizationHandlerProvider>(new DefaultAuthorizationHandlerProvider(new[] { new PassThroughAuthorizationHandler() })),
                new DefaultAuthorizationHandlerContextFactory());
            var requirement = SucceedingRequirement.Or(
                new AssertionRequirement(ctx => {
                    secondRequirementEvaluated = true;
                    return true;
                }));
            var context = new AuthorizationHandlerContext(new[] { requirement }, BuildClaimsPrincipalWithClaims(), null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
            Assert.False(secondRequirementEvaluated);
        }

        [Fact(DisplayName = "Jeśli drugi subrequirement się uda, OrRequirement udaje się")]
        public async Task When_second_subRequirement_succeeds__OrRequirement_succeeds()
        {
            var handler = new OrAuthorizationRequirementHandler(
                new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions() { InvokeHandlersAfterFailure = false }),
                new Lazy<IAuthorizationHandlerProvider>(new DefaultAuthorizationHandlerProvider(new[] { new PassThroughAuthorizationHandler() })),
                new DefaultAuthorizationHandlerContextFactory());
            var requirement = FailingRequirement.Or(SucceedingRequirement);
            var context = new AuthorizationHandlerContext(new[] { requirement }, BuildClaimsPrincipalWithClaims(), null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli wszystkie subrequirementy się udadzą, OrRequirement udaje się")]
        public async Task When_all_subRequirements_succeed__OrRequirement_succeeds()
        {
            var handler = new OrAuthorizationRequirementHandler(
                new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions() { InvokeHandlersAfterFailure = false }),
                new Lazy<IAuthorizationHandlerProvider>(new DefaultAuthorizationHandlerProvider(new[] { new PassThroughAuthorizationHandler() })),
                new DefaultAuthorizationHandlerContextFactory());
            var requirement = SucceedingRequirement.Or(SucceedingRequirement);
            var context = new AuthorizationHandlerContext(new[] { requirement }, BuildClaimsPrincipalWithClaims(), null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli nie podano żadnego subrequirementu, OrRequirement zawodzi")]
        public void When_no_subRequirements_are_provided__OrRequirement_throws_ArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new OrAuthorizationRequirement(Array.Empty<IAuthorizationRequirement>()));
        }

        [Fact(DisplayName = "Jeśli jednym z subrequirementów jest OrRequirement, requirement jest rozwiązywany rekurencyjnie")]
        public async Task When_subRequirements_contain_another_OrRequirement__requirements_are_resolved_recursively()
        {
            var handlers = new List<IAuthorizationHandler>() { new PassThroughAuthorizationHandler() };
            var handler = new OrAuthorizationRequirementHandler(
                new OptionsWrapper<AuthorizationOptions>(new AuthorizationOptions() { InvokeHandlersAfterFailure = false }),
                new Lazy<IAuthorizationHandlerProvider>(new DefaultAuthorizationHandlerProvider(handlers)),
                new DefaultAuthorizationHandlerContextFactory());
            handlers.Add(handler);

            var requirement = new OrAuthorizationRequirement(new[] {
                new OrAuthorizationRequirement(new[] {
                    FailingRequirement,
                    FailingRequirement
                }),
                new OrAuthorizationRequirement(new[] {
                    FailingRequirement,
                    new OrAuthorizationRequirement(new[] {
                        FailingRequirement,
                        SucceedingRequirement
                    })
                })
            });
            var context = new AuthorizationHandlerContext(new[] { requirement }, BuildClaimsPrincipalWithClaims(), null);

            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }
    }
}

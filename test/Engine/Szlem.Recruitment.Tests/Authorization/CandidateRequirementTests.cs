using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.Recruitment.Authorization;
using Szlem.Recruitment.Impl.Authorization;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.Recruitment.Tests.Authorization
{
    public class CandidateRequirementTests
    {
        private ClaimsPrincipal BuildClaimsPrincipalWithClaims(params Claim[] claims) => new ClaimsPrincipal(new[] { new ClaimsIdentity(claims) });


        [Fact(DisplayName = "Jeśli nie podano użytkownika, handler nie autoryzuje")]
        public async Task When_no_user_is_provided__handler_does_not_authorize()
        {
            var handler = new PassThroughAuthorizationHandler();

            var context = new AuthorizationHandlerContext(new[] { new CandidateRequirement() }, null, null);
            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli użytkownik ma claim Candidate, handler autoryzuje")]
        public async Task When_user_has_Candidate_claim__handler_authorizes()
        {
            var handler = new PassThroughAuthorizationHandler();
            var user = BuildClaimsPrincipalWithClaims(new Claim(SharedKernel.ClaimTypes.Candidate, Guid.NewGuid().ToString()));

            var context = new AuthorizationHandlerContext(new[] { new CandidateRequirement() }, user, null);
            await handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }


        [Fact(DisplayName = "Jeśli użytkownik nie ma claima Candidate, handler nie autoryzuje")]
        public async Task When_has_no_Candidate_claim_and_no_roles__handler_does_not_authorize()
        {
            var handler = new PassThroughAuthorizationHandler();
            var user = BuildClaimsPrincipalWithClaims();

            var context = new AuthorizationHandlerContext(new[] { new CandidateRequirement() }, user, null);
            await handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }
    }
}

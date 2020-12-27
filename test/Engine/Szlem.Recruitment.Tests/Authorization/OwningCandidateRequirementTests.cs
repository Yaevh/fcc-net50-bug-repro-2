using Microsoft.AspNetCore.Authorization;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Szlem.Recruitment.Authorization;
using Szlem.Recruitment.Impl.Authorization;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.SharedKernel;
using Xunit;

namespace Szlem.Recruitment.Tests.Authorization
{
    public class OwningCandidateRequirementTests
    {
        private ClaimsPrincipal BuildClaimsPrincipalWithClaims(params Claim[] claims) => new ClaimsPrincipal(new[] { new ClaimsIdentity(claims) });

        private OwningCandidateAuthorizationHandler Handler => new OwningCandidateAuthorizationHandler();


        [Fact(DisplayName = "Jeśli nie podano użytkownika, handler nie przepuszcza")]
        public async Task When_no_user_is_provided__handler_does_not_authorize()
        {
            var context = new AuthorizationHandlerContext(new[] { new OwningCandidateRequirement() }, null, null);
            await Handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli użytkownik nie ma claima Candidate, handler nie przepuszcza")]
        public async Task When_user_does_not_have_Candidate_claim__handler_does_not_authorize()
        {
            var user = BuildClaimsPrincipalWithClaims();

            var context = new AuthorizationHandlerContext(new[] { new OwningCandidateRequirement() }, user, null);
            await Handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact(DisplayName = "Jeśli użytkownik ma claim Candidate, a resource jest pusty, handler autoryzuje")]
        public async Task When_user_has_Candidate_claim_and_resource_is_null__handler_authorizes()
        {
            var user = BuildClaimsPrincipalWithClaims(new Claim(SharedKernel.ClaimTypes.Candidate, Guid.NewGuid().ToString()));

            var context = new AuthorizationHandlerContext(new[] { new OwningCandidateRequirement() }, user, null);
            await Handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Theory(DisplayName = "Jeśli użytkownik ma claim Candidate, a resource jest zgodny z tym claimem, handler autoryzuje")]
        [MemberData(nameof(MatchingResources))]
        public async Task When_has_Candidate_claim_and_resource_is_compatible_with_that_claim__handler_authorizes(string claimValue, object resource)
        {
            var user = BuildClaimsPrincipalWithClaims(new Claim(SharedKernel.ClaimTypes.Candidate, claimValue));

            var context = new AuthorizationHandlerContext(new[] { new OwningCandidateRequirement() }, user, resource);
            await Handler.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        [Theory(DisplayName = "Jeśli użytkownik ma claim Candidate, ale resource jest niezgodny z tym claimem, handler nie przepuszcza")]
        [MemberData(nameof(NotMatchingResources))]
        public async Task When_has_Candidate_claim_but_resource_is_not_compatible_with_that_claim__handler_does_not_authorize(string claimValue, object resource)
        {
            var user = BuildClaimsPrincipalWithClaims(new Claim(SharedKernel.ClaimTypes.Candidate, claimValue));

            var context = new AuthorizationHandlerContext(new[] { new OwningCandidateRequirement() }, user, resource);
            await Handler.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }


        public static TheoryData<string, object> MatchingResources => new TheoryData<string, object>() {
            { "E47D5756-57E5-40CD-982A-278916755DE6", EnrollmentAggregate.EnrollmentId.With(Guid.Parse("E47D5756-57E5-40CD-982A-278916755DE6")) },
            { "3C49F11E-416B-42D0-A20D-B520DC319F18", new EnrollmentAggregate(EnrollmentAggregate.EnrollmentId.With(Guid.Parse("3C49F11E-416B-42D0-A20D-B520DC319F18"))) },
            { "A62A4101-BA13-4319-AC62-EE4983317714", new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.With(Guid.Parse("A62A4101-BA13-4319-AC62-EE4983317714"))} }
        };

        public static TheoryData<string, object> NotMatchingResources => new TheoryData<string, object>() {
            { "86C6538C-47BD-4BB2-9BF3-3E3D2D404258", EnrollmentAggregate.EnrollmentId.With(Guid.Parse("E47D5756-57E5-40CD-982A-278916755DE6")) },
            { "86C6538C-47BD-4BB2-9BF3-3E3D2D404258", new EnrollmentAggregate(EnrollmentAggregate.EnrollmentId.With(Guid.Parse("3C49F11E-416B-42D0-A20D-B520DC319F18"))) },
            { "86C6538C-47BD-4BB2-9BF3-3E3D2D404258", new EnrollmentReadModel() { Id = EnrollmentAggregate.EnrollmentId.With(Guid.Parse("A62A4101-BA13-4319-AC62-EE4983317714"))} }
        };
    }
}

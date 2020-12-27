using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Authorization
{
    public class CandidateRequirement : AuthorizationHandler<CandidateRequirement>, IAuthorizationRequirement
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CandidateRequirement requirement)
        {
            if (context.User == null)
                return Task.CompletedTask;

            var candidateClaim = context.User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Candidate);
            if (candidateClaim != null)
                context.Succeed(requirement);

            return Task.CompletedTask;
        }
    }
}

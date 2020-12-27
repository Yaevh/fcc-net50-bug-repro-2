using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Models.Users;
using Szlem.Recruitment.Authorization;
using Szlem.Recruitment.Impl.Enrollments;
using Szlem.SharedKernel;

namespace Szlem.Recruitment.Impl.Authorization
{
    internal class OwningCandidateAuthorizationHandler : AuthorizationHandler<OwningCandidateRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OwningCandidateRequirement requirement)
        {
            if (context.User == null)
                return Task.CompletedTask;

            var candidateClaim = context.User.Claims.SingleOrDefault(x => x.Type == ClaimTypes.Candidate);
            if (candidateClaim == null)
                return Task.CompletedTask;

            var enrollmentIdClaim = context.User.FindFirst(ClaimTypes.Candidate);
            if (enrollmentIdClaim == null || Guid.TryParse(enrollmentIdClaim.Value, out Guid enrollmentGuid) == false)
                return Task.CompletedTask;

            var enrollmentId = EnrollmentAggregate.EnrollmentId.With(enrollmentGuid);

            if (context.Resource == null) // the resource is unknown yet (preliminary authorization)
                context.Succeed(requirement);

            switch (context.Resource)
            {
                case EnrollmentAggregate aggregate:
                    if (aggregate.Id == enrollmentId)
                        context.Succeed(requirement);
                    break;
                case EnrollmentReadModel readModel:
                    if (readModel.Id == enrollmentId)
                        context.Succeed(requirement);
                    break;
                case EnrollmentAggregate.EnrollmentId id:
                    if (id == enrollmentId)
                        context.Succeed(requirement);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.SharedKernel
{
    public static class AuthorizationPolicies
    {
        public const string AdminOnly = "policy.adminonly";

        public const string CoordinatorsOnly = "policy.coordinators-only";
        public const string OwningCoordinatorOnly = "policy.owning-coordinator-only";

        public const string ApprovedUsers = "policy.approvedusers";

        public const string CanAccessSwagger = "policy.swagger.access";
        
        public const string CandidateOnly = "policy.candidate-only";
        public const string CandidateOrCoordinator = "policy.candidate-or-coordinator";
        public const string OwningCandidateOrCoordinator = "policy.owning-candidate-or-coordinator";
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Engine;
using Szlem.SharedKernel;

namespace Szlem.AspNetCore.Common
{
    public static class AuthorizationPolicyDefinitions
    {
        public static AuthorizationOptions ConfigureSzlemPolicies(this AuthorizationOptions options)
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .RequireRole(AuthorizationRoles.Admin);
            });

            options.AddPolicy(AuthorizationPolicies.ApprovedUsers, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .RequireRole(AuthorizationRoles.ApprovedUser, AuthorizationRoles.CurriculumCoordinator, AuthorizationRoles.OperationsCoordinator, AuthorizationRoles.RegionalCoordinator, AuthorizationRoles.Admin);
            });

            options.AddPolicy(AuthorizationPolicies.CoordinatorsOnly, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .RequireRole(AuthorizationRoles.CurriculumCoordinator, AuthorizationRoles.OperationsCoordinator, AuthorizationRoles.RegionalCoordinator, AuthorizationRoles.Admin);
            });

            options.AddPolicy(AuthorizationPolicies.OwningCoordinatorOnly, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .AddRequirements(
                        new Szlem.Engine.Infrastructure.IsOwnerAuthorizationRequirement()
                        .Or(new RolesAuthorizationRequirement(new[] {
                            AuthorizationRoles.Admin
                        })
                    ));
            });

            options.AddPolicy(AuthorizationPolicies.CanAccessSwagger, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .RequireRole(AuthorizationRoles.CurriculumCoordinator, AuthorizationRoles.OperationsCoordinator, AuthorizationRoles.RegionalCoordinator, AuthorizationRoles.Admin);
            });

            options.AddPolicy(AuthorizationPolicies.CandidateOnly, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .RequireClaim(ClaimTypes.Candidate);
            });

            options.AddPolicy(AuthorizationPolicies.CandidateOrCoordinator, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .AddRequirements(
                        new Recruitment.Authorization.CandidateRequirement()
                        .Or(new RolesAuthorizationRequirement(new[] {
                            AuthorizationRoles.CurriculumCoordinator, AuthorizationRoles.OperationsCoordinator, AuthorizationRoles.RegionalCoordinator, AuthorizationRoles.Admin
                        })
                    ));
            });

            options.AddPolicy(AuthorizationPolicies.OwningCandidateOrCoordinator, builder => {
                builder
                    .RequireAuthenticatedUser()
                    .AddRequirements(
                        new Recruitment.Authorization.OwningCandidateRequirement()
                        .Or(new RolesAuthorizationRequirement(new[] {
                            AuthorizationRoles.CurriculumCoordinator, AuthorizationRoles.OperationsCoordinator, AuthorizationRoles.RegionalCoordinator, AuthorizationRoles.Admin
                        })
                    ));
            });

            return options;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models.Users;
using Szlem.SharedKernel;

namespace Szlem.Persistence.EF
{
    public static class UserRoleDefinitions
    {
        public static readonly ApplicationIdentityRole[] Roles = {
            new ApplicationIdentityRole(AuthorizationRoles.Admin, "administrator główny"),
            new ApplicationIdentityRole(AuthorizationRoles.ApprovedUser, "użytkownik zatwierdzony"),
            new ApplicationIdentityRole(AuthorizationRoles.CurriculumCoordinator, "koordynator ds. programowych"),
            new ApplicationIdentityRole(AuthorizationRoles.OperationsCoordinator, "koordynator ds. organizacyjnych"),
            new ApplicationIdentityRole(AuthorizationRoles.RegionalCoordinator, "koordynator regionalny"),
            new ApplicationIdentityRole(AuthorizationRoles.Trainer, "prowadzący szkolenia")
        };
    }
}

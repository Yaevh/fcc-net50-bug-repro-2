using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.SharedKernel
{
    public static class AuthorizationRoles
    {
        public const string Admin = "admin";

        /// <summary>
        /// Koordynator ds. programowych
        /// </summary>
        public const string CurriculumCoordinator = "coordinator.curriculum";

        /// <summary>
        /// Koordynator ds. organizacyjnych
        /// </summary>
        public const string OperationsCoordinator = "coordinator.operations";

        /// <summary>
        /// Koordynator regionalny
        /// </summary>
        public const string RegionalCoordinator = "coordinator.regional";

        /// <summary>
        /// Szkoleniowiec
        /// </summary>
        public const string Trainer = "training.trainer";


        /// <summary>
        /// Użytkownik zatwierdzony
        /// </summary>
        public const string ApprovedUser = "basic.approved";
    }
}

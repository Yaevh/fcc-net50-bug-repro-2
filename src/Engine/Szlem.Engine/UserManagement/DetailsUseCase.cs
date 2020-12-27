using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.UserManagement
{
    public static class DetailsUseCase
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Query : IRequest<UserDetails>
        {
            public Guid UserId { get; set; }
        }

        public class UserDetails
        {
            public Guid ID { get; set; }
            [Display(Name = "Imię")] public string FirstName { get; set; }
            [Display(Name = "Nazwisko")] public string LastName { get; set; }
            [Display(Name = "Imię i nazwisko")] public string FullName { get; set; }
            public string Email { get; set; }
            [Display(Name = "Role użytkownika")] public IReadOnlyList<UserRole> Roles { get; set; }
            [Display(Name = "Czy można zmienić imię i nazwisko?")] public bool CanChangeName { get; set; }
            [Display(Name = "Czy można usunąć użytkownika?")] public bool CanDelete { get; set; }
            [Display(Name = "Czy użytkownikowi można przydzielić role?")] public bool CanGrantRoles { get; set; }


            public class UserRole
            {
                [Display(Name = "Nazwa")] public string Name { get; set; }

                [Display(Name = "Opis")] public string Description { get; set; }

                [Display(Name = "Czy można odebrać tę rolę?")] public bool CanRevoke { get; set; }
            }
        }

        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class GetRolesThatCanBeGrantedQuery : IRequest<IReadOnlyList<RoleData>>
        {
            public Guid UserId { get; set; }
        }

        public class RoleData
        {
            [Display(Name = "Nazwa")] public string Name { get; set; }

            [Display(Name = "Opis")] public string Description { get; set; }
        }
    }
}

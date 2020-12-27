using MediatR;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Szlem.SharedKernel;

namespace Szlem.Engine.UserManagement
{
    public static class IndexUseCase
    {
        [Authorize(AuthorizationPolicies.AdminOnly)]
        public class Query : IRequest<IReadOnlyList<UserSummary>> { }

        public class UserSummary
        {
            public Guid ID { get; set; }

            [Display(Name = "Imię i nazwisko")] public string FullName { get; set; }

            [Display(Name = "E-mail")] public string Email { get; set; }

            [Display(Name = "Czy można wyświetlić szczegóły?")] public bool CanShowDetails { get; set; }

            [Display(Name = "Czy można usunąć?")] public bool CanDelete { get; set; }

            [Display(Name = "Role użytkownika")] public IReadOnlyList<string> Roles { get; set; }

            [Display(Name = "Czy można nadać nową rolę?")] public bool CanGrantRoles { get; set; }

            [Display(Name = "Czy można usunąć?")] public bool CanRevokeRoles { get; set; }
        }
    }
}

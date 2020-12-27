using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Models.Users
{
    public class ApplicationIdentityRole : IdentityRole<Guid>
    {
        public ApplicationIdentityRole() { }

        public ApplicationIdentityRole(string roleName) : base(roleName) { }

        public ApplicationIdentityRole(string roleName, string description) : this(roleName)
        {
            Description = description;
        }


        public string Description { get; set; }
    }
}

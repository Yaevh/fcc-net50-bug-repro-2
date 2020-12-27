using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.Models.Users
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string FullName => string.IsNullOrWhiteSpace(RawFullName) ? UserName : RawFullName;
        protected string RawFullName => $"{FirstName} {LastName}".Trim();

        public string DisplayName {
            get {
                var displayName = $"{FullName} [{Email}]";
                if (displayName.EndsWith("[]"))
                    displayName = displayName.TrimEnd("[]");
                return displayName.Trim();
            }
        }

        public override string ToString() => DisplayName;
    }
}

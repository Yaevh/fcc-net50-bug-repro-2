using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.Models.Schools.Validators
{
    [Obsolete]
    public class SchoolValidator : AbstractValidator<School>
    {
        public SchoolValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.City).NotEmpty();
            RuleFor(x => x.ContactPhoneNumber).NotEmpty();
        }
    }
}

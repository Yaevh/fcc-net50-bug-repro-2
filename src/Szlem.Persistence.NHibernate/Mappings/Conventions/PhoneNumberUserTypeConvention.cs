using FluentNHibernate.Conventions;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models;
using Szlem.Persistence.NHibernate.CustomTypes;

namespace Szlem.Persistence.NHibernate.Mappings.Conventions
{
    public class PhoneNumberUserTypeConvention : UserTypeConvention<PhoneNumberUserType>
    {
    }
}

using FluentNHibernate.Conventions;
using System;
using System.Collections.Generic;
using System.Text;
using Szlem.Models;
using Szlem.Persistence.NHibernate.CustomTypes;
using Szlem.Persistence.NHibernate.CustomTypes.NodaTimeUserTypes;

namespace Szlem.Persistence.NHibernate.Mappings.Conventions.NodaTimeTypeConventions
{
    public class InstantTypeConvention : UserTypeConvention<InstantType> { }

    public class ZonedDateTimeTypeConvention : UserTypeConvention<ZonedDateTimeType> { }

    public class OffsetDateTimeTypeConvention : UserTypeConvention<OffsetDateTimeType> { }
}

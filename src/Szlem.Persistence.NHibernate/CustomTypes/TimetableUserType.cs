using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Szlem.Models;
using Szlem.Models.Schools;

namespace Szlem.Persistence.NHibernate.CustomTypes
{
    public class TimetableUserType : JsonValueUserType<Timetable>
    {
    }
}

using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using Szlem.Persistence.NHibernate.CustomTypes;

namespace Szlem.Recruitment.Impl.Entities.Mappings
{
    public class StringBackedGuidType : ValueUserType<Guid>
    {
        public override SqlType[] SqlTypes => new[] { SqlTypeFactory.GetString(36) };

        public override object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            return new Guid(NHibernateUtil.String.NullSafeGet(rs, names[0], session).ToString());
        }

        public override void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            NHibernateUtil.String.NullSafeSet(cmd, value.ToString(), index, session);
        }
    }
}

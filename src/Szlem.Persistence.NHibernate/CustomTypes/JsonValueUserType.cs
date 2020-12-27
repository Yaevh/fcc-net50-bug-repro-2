using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;

namespace Szlem.Persistence.NHibernate.CustomTypes
{
    public abstract class JsonValueUserType<T> : ValueUserType<T>
    {
        public override SqlType[] SqlTypes => new[] { new SqlType(System.Data.DbType.String) };

        public override object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            var value = NHibernateUtil.String.NullSafeGet(rs, names, session, owner) as string;
            if (value == null)
                return null;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(value);
        }

        public override void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            Debug.Assert(cmd != null);
            if (value == null)
            {
                cmd.Parameters[index].Value = DBNull.Value;
            }
            else
            {
                cmd.Parameters[index].Value = Newtonsoft.Json.JsonConvert.SerializeObject(value);
            }
        }
    }
}

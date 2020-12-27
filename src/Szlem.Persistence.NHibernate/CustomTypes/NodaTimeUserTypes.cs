using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Szlem.Domain;
using Szlem.Models;

namespace Szlem.Persistence.NHibernate.CustomTypes.NodaTimeUserTypes
{
    public class InstantType : ValueUserType<Instant>
    {
        public override SqlType[] SqlTypes => new[] { new SqlType(System.Data.DbType.String) };

        public override object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            var value = NHibernateUtil.String.NullSafeGet(rs, names, session, owner);
            if (value == null)
                return null;

            var dateTimeOffset = DateTimeOffset.Parse(value as string);
            return Instant.FromDateTimeOffset(dateTimeOffset);
        }

        public override void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            Debug.Assert(cmd != null);

            var instant = (Instant)value;
            cmd.Parameters[index].Value = instant.InUtc().ToDateTimeOffset();
        }
    }

    public class ZonedDateTimeType : ValueUserType<ZonedDateTime> 
    {
        public override SqlType[] SqlTypes => new[] { new SqlType(System.Data.DbType.String) };

        public override object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            var value = NHibernateUtil.String.NullSafeGet(rs, names, session, owner);
            if (value == null)
                return null;

            var dateTimeOffset = DateTimeOffset.Parse(value as string);
            return ZonedDateTime.FromDateTimeOffset(dateTimeOffset);
        }

        public override void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            Debug.Assert(cmd != null);

            var zonedDateTime = (ZonedDateTime)value;
            cmd.Parameters[index].Value = zonedDateTime.ToDateTimeOffset();
        }
    }

    public class OffsetDateTimeType : ValueUserType<OffsetDateTime>
    {
        public override SqlType[] SqlTypes => new[] { new SqlType(System.Data.DbType.String) };

        public override object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            var value = NHibernateUtil.String.NullSafeGet(rs, names, session, owner);
            if (value == null)
                return null;

            var dateTimeOffset = DateTimeOffset.Parse(value as string);
            return OffsetDateTime.FromDateTimeOffset(dateTimeOffset);
        }

        public override void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session)
        {
            Debug.Assert(cmd != null);

            var offsetDateTime = (OffsetDateTime)value;
            cmd.Parameters[index].Value = offsetDateTime.ToDateTimeOffset();
        }
    }
}

﻿using NHibernate;
using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Text;
using Szlem.Domain;
using Szlem.Models;

namespace Szlem.Persistence.NHibernate.CustomTypes
{
    public class EmailAddressUserType : ValueUserType<EmailAddress>
    {
        public override SqlType[] SqlTypes => new[] { new SqlType(System.Data.DbType.String) };
        
        public override object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner)
        {
            var value = NHibernateUtil.String.NullSafeGet(rs, names, session, owner) as string;
            if (value == null)
                return null;
            return EmailAddress.Parse(value);
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
                cmd.Parameters[index].Value = value.ToString();
            }
        }
    }
}

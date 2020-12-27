using NHibernate.Engine;
using NHibernate.SqlTypes;
using NHibernate.UserTypes;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Szlem.Persistence.NHibernate.CustomTypes
{
    public abstract class ValueUserType<T> : IUserType
    {
        public bool IsMutable => false;

#pragma warning disable CA1819 // Properties should not return arrays // required by NHibernate
        public abstract SqlType[] SqlTypes { get; }
#pragma warning restore CA1819 // Properties should not return arrays

        public Type ReturnedType => typeof(T);


        public object Assemble(object cached, object owner) => cached;

        public object DeepCopy(object value) => value;

        public object Disassemble(object value) => value;

        public object Replace(object original, object target, object owner) => original;


        bool IUserType.Equals(object x, object y) => Equals(x, y);

        public int GetHashCode(object x) => x == null ? default : x.GetHashCode();


        public abstract object NullSafeGet(DbDataReader rs, string[] names, ISessionImplementor session, object owner);

        public abstract void NullSafeSet(DbCommand cmd, object value, int index, ISessionImplementor session);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class TypeExtensions
    {
        public static bool Implements<TInterface>(this Type type)
        {
            if (typeof(TInterface).IsInterface == false)
                throw new InvalidOperationException($"{nameof(TInterface)} is not an interface");
            return typeof(TInterface).IsAssignableFrom(type);
        }
    }
}

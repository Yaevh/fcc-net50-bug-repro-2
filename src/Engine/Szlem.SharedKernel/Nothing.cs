using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.SharedKernel
{
    public struct Nothing : IEquatable<Nothing>, IComparable<Nothing>, IComparable
    {
        public static readonly Nothing Value = new Nothing();

        public int CompareTo(Nothing other) => 0;

        public int CompareTo(object obj) => 0;

        public override bool Equals(object obj) => obj is Nothing;

        public bool Equals(Nothing other) => true;

        public override int GetHashCode() => 0;

#pragma warning disable IDE0060 // Remove unused parameter
        public static bool operator ==(Nothing left, Nothing right) => true;

        public static bool operator !=(Nothing left, Nothing right) => false;
#pragma warning restore IDE0060 // Remove unused parameter
    }
}

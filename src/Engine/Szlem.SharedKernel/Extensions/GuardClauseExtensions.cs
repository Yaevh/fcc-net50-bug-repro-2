using Ardalis.GuardClauses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Ardalis.GuardClauses
{
    public static class GuardClauseExtensions
    {
        public static void False(this IGuardClause guardClause, bool input, string parameterName)
        {
            if (input == false)
                throw new ArgumentException($"Parameter {parameterName} cannot be false.", parameterName);
        }

        public static void Empty<T>(this IGuardClause guardClause, IReadOnlyCollection<T> collection, string parameterName)
        {
            if (collection == null)
                return;
            if (collection.None())
                throw new ArgumentException($"Collection parameter {parameterName} cannot be empty", parameterName);
        }

        public static void NullOrEmpty<T>(this IGuardClause guardClause, IReadOnlyCollection<T> collection, string parameterName)
        {
            guardClause.Null(collection, parameterName);
            guardClause.Empty(collection, parameterName);
        }
    }
}

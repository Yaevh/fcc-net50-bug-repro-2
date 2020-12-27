using System;
using System.Collections.Generic;
using System.Text;
using CSharpFunctionalExtensions;

namespace CSharpFunctionalExtensions
{
    public static class ResultExtensions
    {
        /// <summary>
        ///     Returns a new failure result if the predicate is false. Otherwise returns the starting result.
        /// </summary>
        public static Result<T, E> Ensure<T, E>(this Result<T, E> result, Func<T, bool> predicate, Func<E> error)
        {
            if (result.IsFailure)
                return result;

            if (!predicate(result.Value))
                return Result.Failure<T, E>(error());

            return result;
        }

        /// <summary>
        ///     Returns a new failure result if the predicate is false. Otherwise returns the starting result.
        /// </summary>
        public static Result<T, E> Ensure<T, E>(this Result<T, E> result, Func<T, bool> predicate, Func<T, E> error)
        {
            if (result.IsFailure)
                return result;

            if (!predicate(result.Value))
                return Result.Failure<T, E>(error(result.Value));

            return result;
        }

        public static Result<T, E> Ensure<T, E>(this Result<T, E> result, Func<T, Result<T, E>> predicate, Func<Result<T, E>, E> error)
        {
            if (result.IsFailure)
                return result;

            var predicateResult = predicate(result.Value);
            if (predicateResult.IsFailure)
                return Result.Failure<T, E>(error(predicateResult));

            return result;
        }

        public static Result<T, E> Ensure<T, E>(this Result<T, E> result, Func<T, Result<T, E>> predicate)
        {
            if (result.IsFailure)
                return result;

            var predicateResult = predicate(result.Value);
            if (predicateResult.IsFailure)
                return predicateResult;

            return result;
        }

        /// <summary>
        ///     Returns a new failure result if the predicate is true. Otherwise returns the starting result.
        /// </summary>
        public static Result<T, E> EnsureNot<T, E>(this Result<T, E> result, Func<T, bool> predicate, E error)
        {
            if (result.IsFailure)
                return result;

            if (predicate(result.Value))
                return Result.Failure<T, E>(error);

            return result;
        }
    }
}

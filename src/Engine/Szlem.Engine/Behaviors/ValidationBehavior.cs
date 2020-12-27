using CSharpFunctionalExtensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Interfaces;

namespace Szlem.Engine.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var context = new ValidationContext<TRequest>(request);

            var failures = (await Task.WhenAll(_validators.Select(async v => await v.ValidateAsync(context))))
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .Select(x => new Domain.Exceptions.ValidationFailure(x.PropertyName, x.ErrorMessage))
                .ToList();

            if (failures.Any())
            {
                var specialResponse = TryBuildSpecialResponse(failures);
                if (specialResponse.HasValue)
                    return specialResponse.Value;

                throw new Domain.Exceptions.ValidationException(failures);
            }

            try
            {
                return await next();
            }
            catch (FluentValidation.ValidationException ex)
            {
                var specialResponse = TryBuildSpecialResponse(ex.Errors);
                if (specialResponse.HasValue)
                    return specialResponse.Value;
                else
                    throw;
            }
            catch (Szlem.Domain.Exceptions.ValidationException ex)
            {
                var specialResponse = TryBuildSpecialResponse(ex.Failures);
                if (specialResponse.HasValue)
                    return specialResponse.Value;
                else
                    throw;
            }
        }


        private Maybe<TResponse> TryBuildSpecialResponse(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        {
            var collection = new Domain.Exceptions.ValidationFailuresCollection(failures
                .GroupBy(x => x.PropertyName)
                .Select(x => new Domain.Exceptions.ValidationFailure(x.Key, x.Select(y => y.ErrorMessage)))
            );
            return TryBuildSpecialResponse(collection);
        }

        private Maybe<TResponse> TryBuildSpecialResponse(IEnumerable<Szlem.Domain.Exceptions.ValidationFailure> failures)
        {
            var responseType = typeof(TResponse);

            // special case: if TResponse is Maybe<Szlem.Domain.Error>
            // then return Maybe.From(Szlem.Domain.Error.BadRequest)
            // instead of throwing ValidationException
            if (responseType == typeof(Maybe<Szlem.Domain.Error>))
            {
                var error = new Domain.Error.ValidationFailed(failures);
                return Maybe<TResponse>.From((TResponse)(object)Maybe<Domain.Error>.From(error));
            }

            // special case: if TResponse is Result<TDetailedResponse, Szlem.Domain.Error>
            // then return Result.Failure(Szlem.Domain.Error.BadRequest)
            // instead of throwing ValidationException
            if (responseType.IsGenericType
                && responseType.GetGenericTypeDefinition() == typeof(Result<,>)
                && responseType.GenericTypeArguments.Length == 2
                && typeof(Domain.Error).IsAssignableFrom(responseType.GenericTypeArguments[1]))
            {
                var error = new Domain.Error.ValidationFailed(failures);

                var baseMethods = typeof(Result)
                        .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var baseMethod = baseMethods
                    .Where(x => x.Name == nameof(Result.Failure) && x.GetGenericArguments().Length == 2)
                    .Single();

                var genericMethod = baseMethod.MakeGenericMethod(responseType.GenericTypeArguments);
                return (TResponse)genericMethod.Invoke(null, new[] { error });
            }

            return Maybe<TResponse>.None;
        }
    }
}

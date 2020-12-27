using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Domain.Exceptions;

namespace Szlem.Domain
{
    public class Error
    {
        public string Message { get; }

        public Error(string message)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
        }

        public override string ToString() => Message;

        public class ResourceNotFound : Error
        {
            public ResourceNotFound(string message) : base(message) { }
            public ResourceNotFound() : this("Resource not found") { }
        }

        public class BadRequest : Error
        {
            public BadRequest(string message) : base(message) { }
            public BadRequest() : this("Bad request") { }
        }

        public class ValidationFailed : BadRequest
        {
            public ValidationFailuresCollection Failures { get; } = new ValidationFailuresCollection();

            public ValidationFailed(string propertyName, string error) : this(new[] { new ValidationFailure(propertyName, error) }) { }

            public ValidationFailed(params ValidationFailure[] failures) : this(failures.AsEnumerable()) { }

            public ValidationFailed(IEnumerable<ValidationFailure> failures) : base(ErrorMessages.OneOrMoreValidationErrorsOccured)
            {
                Failures = new ValidationFailuresCollection();
                foreach (var failure in failures
                    .GroupBy(x => x.PropertyName)
                    .Select(x => new ValidationFailure(x.Key, x.SelectMany(y => y.Errors))))
                {
                    Failures.Add(failure);
                }
            }

            public ValidationFailed(FluentValidation.Results.ValidationResult result) : this(result.Errors) { }

            public ValidationFailed(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
                : this(failures.Select(x => new ValidationFailure(x.PropertyName, x.ErrorMessage))) { }
        }

        public class AuthorizationFailed : BadRequest
        {
            public AuthorizationFailed(string message) : base(message) { }
            public AuthorizationFailed() : this("Authorization failed") { }
        }

        public class DomainError : Error
        {
            public DomainError(string message) : base(message) { }
        }
    }
}

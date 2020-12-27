using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Szlem.Domain.Exceptions
{
    public class ValidationException : SzlemException
    {
        public ValidationFailuresCollection Failures { get; } = new ValidationFailuresCollection();


        public ValidationException()
            : base("One or more validation failures have occured.") { }

        public ValidationException(string message) : this(string.Empty, message) { }

        public ValidationException(string propertyName, string message) : this(new ValidationFailure(propertyName, message)) { }

        public ValidationException(params ValidationFailure[] failures) : this(failures.AsEnumerable()) { }

        public ValidationException(FluentValidation.Results.ValidationResult validationResult)
            : this((validationResult ?? throw new ArgumentNullException(nameof(validationResult))).Errors.Select(x => new ValidationFailure(x.PropertyName, x.ErrorMessage)))
        {
            if (validationResult.IsValid)
                throw new ArgumentException($"Cannot create {nameof(ValidationException)} from successful {nameof(FluentValidation.Results.ValidationResult)}");
        }

        public ValidationException(IEnumerable<ValidationFailure> failures) : this()
        {
            foreach (var failure in failures
                .GroupBy(x => x.PropertyName)
                .Select(x => new ValidationFailure(x.Key, x.SelectMany(y => y.Errors))))
            {
                Failures.Add(failure);
            }
        }
    }
}

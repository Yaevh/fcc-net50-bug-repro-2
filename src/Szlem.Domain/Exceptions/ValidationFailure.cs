using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ardalis.GuardClauses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Szlem.Domain.Exceptions
{
    public class ValidationFailure
    {
        public ValidationFailure(string error) : this(string.Empty, error) { }

        public ValidationFailure(string propertyName, string error) : this(propertyName, new[] { error }) { }

        public ValidationFailure(string propertyName, IEnumerable<string> errors)
        {
            Guard.Against.Null(propertyName, nameof(propertyName));
            Guard.Against.Null(errors, nameof(errors));

            PropertyName = propertyName;
            Errors = errors?.ToArray() ?? throw new ArgumentNullException(nameof(errors));
        }

        public string PropertyName { get; set; }

        public IReadOnlyCollection<string> Errors { get; set; }
    }
}

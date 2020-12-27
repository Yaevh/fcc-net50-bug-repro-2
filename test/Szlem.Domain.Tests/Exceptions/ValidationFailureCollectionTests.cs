using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Szlem.Domain.Exceptions;
using Xunit;

namespace Szlem.Domain.Tests.Exceptions
{
    public class ValidationFailureCollectionTests
    {
        [Fact]
        public void CanBeEnumerated_asValidationFailures()
        {
            var collection = new ValidationFailuresCollection()
            {
                new ValidationFailure("A", "A"),
                new ValidationFailure("B", "B"),
                new ValidationFailure("C", "C")
            };

            var list = collection.ToList();

            Assert.Collection(list,
                first => { Assert.Equal("A", first.PropertyName); Assert.Equal("A", first.Errors.Single()); },
                second => { Assert.Equal("B", second.PropertyName); Assert.Equal("B", second.Errors.Single()); },
                third => { Assert.Equal("C", third.PropertyName); Assert.Equal("C", third.Errors.Single()); }
            );
        }
    }
}

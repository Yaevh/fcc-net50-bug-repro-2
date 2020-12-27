using System;
using Szlem.Domain.Exceptions;
using Xunit;

namespace Szlem.Test.Helpers
{
    public class AssertHelpers
    {
        public static void SingleError(string expectedProperty, string expectedError, ValidationFailuresCollection validationFailures)
        {
            Assert.Collection(
                validationFailures,
                failure =>
                {
                    Assert.Equal(expectedProperty, failure.PropertyName);
                    Assert.Collection(failure.Errors,
                        single => Assert.Equal(expectedError, single));
                }
            );
        }

        public static void SingleError(string expectedProperty, string expectedError, ValidationFailure validationFailure)
        {
            Assert.Equal(expectedProperty, validationFailure.PropertyName);
            Assert.Single(validationFailure.Errors, x => x.Equals(expectedError));
        }
    }
}

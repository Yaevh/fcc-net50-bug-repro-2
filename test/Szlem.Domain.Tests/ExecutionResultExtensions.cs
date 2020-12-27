using EventFlow.Aggregates.ExecutionResults;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Szlem.Domain.Tests
{
    public static class ExecutionResultExtensions
    {
        public static void AssertHasError(this IExecutionResult result, string error)
        {
            var failure = Assert.IsType<FailedExecutionResult>(result);
            Assert.Contains(error, failure.Errors);
        }
    }
}

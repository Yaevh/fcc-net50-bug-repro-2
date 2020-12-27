using Microsoft.Extensions.DependencyInjection;
using System;
using Szlem.Recruitment.Impl;
using Xunit;

namespace Szlem.Recruitment.Tests
{
    public class DbSessionTests
    {
        [Fact]
        public void DbSession_CanBeBuilt()
        {
            var sp = new ServiceProviderBuilder().BuildServiceProvider();
            using (var dbConfig = sp.GetRequiredService<DbSessionProvider>())
            {
                using (var session = dbConfig.CreateSession())
                {
                    // shouldn't throw
                }
            }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Szlem.Engine.Interfaces;

namespace Szlem.Engine.Infrastructure
{
    public class DummyRequestAuthorizationAnalyzer : IRequestAuthorizationAnalyzer
    {
        public Task<AuthorizationResult> Authorize<TRequest>(TRequest request)
        {
            return Task.FromResult(AuthorizationResult.Success());
        }
    }
}

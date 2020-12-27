using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.Engine.Infrastructure
{
    public interface IRequestAuthorizationAnalyzer
    {
        Task<AuthorizationResult> Authorize<TRequest>(TRequest request);
    }
}

using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.Engine.Infrastructure
{
    public interface IAuthorizationPolicyConfigurator
    {
        void Configure(AuthorizationOptions options);
    }
}

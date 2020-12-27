using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Interfaces;

namespace Szlem.Engine.Behaviors
{
    public class AuthorizationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IUserAccessor _userAccessor;

        public AuthorizationBehavior(IAuthorizationService authorizationService, IUserAccessor userAccessor)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var result = await new Infrastructure.RequestAuthorizationAnalyzer(_userAccessor, _authorizationService).Authorize(request);
            if (result.Succeeded == false)
                throw new Exceptions.AuthorizationFailureException(result.Failure);
            
            return await next.Invoke();
        }
    }
}

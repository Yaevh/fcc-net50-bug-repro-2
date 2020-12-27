using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Szlem.Engine
{
    public class OrAuthorizationRequirement : IAuthorizationRequirement
    {
        public IReadOnlyCollection<IAuthorizationRequirement> SubRequirements { get; }

        public OrAuthorizationRequirement(params IAuthorizationRequirement[] subRequirements) :
            this(subRequirements as IReadOnlyCollection<IAuthorizationRequirement>) { }

        public OrAuthorizationRequirement(IReadOnlyCollection<IAuthorizationRequirement> subRequirements)
        {
            if (subRequirements is null || subRequirements.None())
                throw new ArgumentNullException(nameof(subRequirements));

            SubRequirements = subRequirements;
        }
    }

    public static class AuthorizationRequirementExtensions
    {
        public static IAuthorizationRequirement Or(this IAuthorizationRequirement thisRequirement, IAuthorizationRequirement otherRequirement)
        {
            return new OrAuthorizationRequirement(thisRequirement, otherRequirement);
        }
    }

    public class OrAuthorizationRequirementHandler : IAuthorizationHandler
    {
        private readonly IOptions<AuthorizationOptions> _options;
        private readonly Lazy<IAuthorizationHandlerProvider> _handlers;
        private readonly IAuthorizationHandlerContextFactory _contextFactory;

        public OrAuthorizationRequirementHandler(IOptions<AuthorizationOptions> options, Lazy<IAuthorizationHandlerProvider> handlers, IAuthorizationHandlerContextFactory contextFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _handlers = handlers ?? throw new ArgumentNullException(nameof(handlers));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task HandleAsync(AuthorizationHandlerContext context)
        {
            Guard.Against.Null(context, nameof(context));
            foreach (var req in context.Requirements.OfType<OrAuthorizationRequirement>())
            {
                await HandleRequirementAsync(context, req);
            }
        }

        private async Task HandleRequirementAsync(AuthorizationHandlerContext context, OrAuthorizationRequirement requirement)
        {
            Guard.Against.Null(context, nameof(context));
            Guard.Against.Null(requirement, nameof(requirement));

            if (requirement.SubRequirements.None()) // short-circuit if no requirements
            {
                context.Succeed(requirement);
                return;
            }

            var options = _options.Value;
            var handlers = _handlers.Value;

            foreach (var subRequirement in requirement.SubRequirements)
            {
                var subContext = _contextFactory.CreateContext(new[] { subRequirement }, context.User, context.Resource);

                foreach (var handler in await handlers.GetHandlersAsync(subContext))
                {
                    await handler.HandleAsync(subContext);
                    if (subContext.HasFailed && options.InvokeHandlersAfterFailure == false)
                        break;
                }

                if (subContext.HasSucceeded)
                {
                    context.Succeed(requirement);
                    return;
                }
            }
        }
    }
}

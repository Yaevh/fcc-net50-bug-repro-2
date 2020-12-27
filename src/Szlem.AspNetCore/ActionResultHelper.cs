using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Szlem.AspNetCore
{
    public class ActionResultHelper
    {
        public IActionResult GetActionResultFor(Szlem.Domain.Error error)
        {
            Guard.Against.Null(error, nameof(error));
            switch (error)
            {
                case Szlem.Domain.Error.AuthorizationFailed e:
                    return new ForbidResult(JwtBearerDefaults.AuthenticationScheme);
                case Szlem.Domain.Error.ValidationFailed e:
                    return new BadRequestObjectResult(new ValidationProblemDetails(e.Failures.ToDictionary(x => x.PropertyName, x => x.Errors.ToArray())) { Title = e.Message, Detail = e.Message });
                case Szlem.Domain.Error.ResourceNotFound e:
                    return new NotFoundObjectResult(new ProblemDetails() { Type = "https://httpstatuses.com/404", Title = e.Message, Detail = e.Message });
                case Szlem.Domain.Error.DomainError e:
                    return new BadRequestObjectResult(new ProblemDetails() { Type = "https://httpstatuses.com/400", Title = e.Message, Detail = e.Message });
                case Szlem.Domain.Error.BadRequest e:
                    return new BadRequestObjectResult(new ProblemDetails() { Type = "https://httpstatuses.com/400", Title = e.Message, Detail = e.Message });
                default:
                    throw new NotSupportedException($"Unknown error type: {error.GetType()}");
            }
        }
    }
}

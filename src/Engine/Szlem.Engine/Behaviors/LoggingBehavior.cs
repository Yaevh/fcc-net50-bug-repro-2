using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Szlem.Engine.Interfaces;
using Szlem.Models.Users;

namespace Szlem.Engine.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IUserAccessor _userAccessor;
        private readonly JsonSerializerSettings _serializerSettings;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IUserAccessor userAccessor, JsonSerializerSettings serializerSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userAccessor = userAccessor ?? throw new ArgumentNullException(nameof(userAccessor));
            _serializerSettings = serializerSettings ?? throw new ArgumentNullException(nameof(serializerSettings));
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
        {
            var user = await _userAccessor.GetUser() ?? new ApplicationUser() { UserName = "anonymous" };
            using (_logger.BeginScope(request))
            {
                try
                {
                    _logger.LogInformation($"user {user} executing request {request}");
                    try
                    {
                        _logger.LogTrace($"user {user} executing request {request}: {JsonConvert.SerializeObject(request, _serializerSettings)}");
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning($"user {user} executing request {request}: failed to serialize the request ({ex})");
                    }

                    var response = await next.Invoke();

                    _logger.LogInformation($"user {user} finished executing request {request}, response {response}");
                    try
                    {
                        _logger.LogTrace($"user {user} executed request {request}: {JsonConvert.SerializeObject(request, _serializerSettings)}; received response {response}: {JsonConvert.SerializeObject(response, _serializerSettings)}");
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning($"user {user} executing request {request}, response {response}: failed to serialize request/response pair ({ex})");
                    }

                    return response;
                }
                catch (Exception ex)
                {
                    try
                    {
                        _logger.LogError($"user {user} executing request {request}: {JsonConvert.SerializeObject(request, _serializerSettings)}; and encountered exception {ex}");
                    }
                    catch (JsonException jsonEx)
                    {
                        _logger.LogError(ex, $"user {user} executed request {request}: failed to serialize the request ({jsonEx}); and encountered exception {ex}");
                    }
                    throw;
                }
            }
        }
    }
}

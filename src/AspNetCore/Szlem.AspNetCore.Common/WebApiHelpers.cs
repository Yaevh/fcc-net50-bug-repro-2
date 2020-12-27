using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Text;

namespace Szlem.AspNetCore.Common
{
    public static class HttpContextExtensions
    {
        public static bool PrefersHtmlResponse(this HttpContext httpContext)
        {
            var accepts = httpContext.Request.Headers[HeaderNames.Accept].ToString();
            if (string.IsNullOrWhiteSpace(accepts))
                return false;
            var delimiter = accepts.IndexOf(';');
            if (delimiter == -1)
                delimiter = accepts.Length - 1;
            var prefers = accepts.Substring(0, delimiter);
            return prefers.Contains("text/html") || prefers.Contains("application/xhtml+xml") || prefers.Contains("application/xml");
        }

        public static bool IsRequestJwtAuthenticated(this HttpContext httpContext)
        {
            string authorization = httpContext.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization];
            if (string.IsNullOrWhiteSpace(authorization))
                return false;
            if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }
}

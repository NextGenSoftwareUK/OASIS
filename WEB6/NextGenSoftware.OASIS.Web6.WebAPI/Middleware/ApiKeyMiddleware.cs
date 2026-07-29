using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Middleware
{
    public class ApiKeyMiddleware
    {
        private const string ApiKeyHeader = "X-OASIS-API-Key";

        private static readonly string[] PublicPaths =
        {
            "/api/avatar/register",
            "/api/avatar/authenticate",
            "/api/avatar/verifyemail",
            "/api/avatar/verifyemail2factor",
            "/api/avatar/forgotpassword",
            "/api/avatar/resetpassword",
            "/health",
            "/swagger",
            "/ws/",
            "/mcp"
        };

        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var security = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Security;

            if (security == null || !security.RequireApiKey)
            {
                await _next(context);
                return;
            }

            var requiredKey = Environment.GetEnvironmentVariable("OASIS_API_KEY");
            if (string.IsNullOrWhiteSpace(requiredKey))
                requiredKey = security.ApiKey;

            if (string.IsNullOrWhiteSpace(requiredKey))
            {
                await _next(context);
                return;
            }

            foreach (var exempt in PublicPaths)
            {
                if (context.Request.Path.StartsWithSegments(exempt, StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }
            }

            if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var provided)
                || !string.Equals(provided, requiredKey, StringComparison.Ordinal))
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"IsError\":true,\"Message\":\"Unauthorized: missing or invalid API key. " +
                    "Include the X-OASIS-API-Key header with a valid key.\"}");
                return;
            }

            await _next(context);
        }
    }
}

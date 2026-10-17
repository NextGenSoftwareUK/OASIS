using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class ApiKeyMiddleware
    {
        private const string ApiKeyHeader = "X-OASIS-API-Key";

        // Endpoints that must remain publicly accessible regardless of API key setting
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
            "/ws/"
        };

        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var security = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Security;

            // Skip entirely if OASISDNA hasn't loaded yet or the feature is off
            if (security == null || !security.RequireApiKey)
            {
                await _next(context);
                return;
            }

            // Environment variable takes priority so the key can be injected as a Railway/Vercel secret
            var requiredKey = Environment.GetEnvironmentVariable("OASIS_API_KEY");
            if (string.IsNullOrWhiteSpace(requiredKey))
                requiredKey = security.ApiKey;

            // No key configured means the check is a no-op even with RequireApiKey = true
            if (string.IsNullOrWhiteSpace(requiredKey))
            {
                await _next(context);
                return;
            }

            // Always allow public endpoints
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

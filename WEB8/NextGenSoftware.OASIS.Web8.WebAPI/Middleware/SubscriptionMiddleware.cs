using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.Web8.WebAPI.Middleware
{
    /// <summary>Delegates subscription authorization and usage accounting to authoritative WEB4.</summary>
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };
        private static readonly string[] BypassPaths = { "/swagger", "/health", "/favicon", "/openapi" };
        public SubscriptionMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            string path = context.Request.Path.Value ?? string.Empty;
            foreach (var bypass in BypassPaths)
                if (path.StartsWith(bypass, StringComparison.OrdinalIgnoreCase)) { await _next(context); return; }

            string auth = context.Request.Headers.Authorization.ToString();
            if (!auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) { await _next(context); return; }
            string web4Base = Environment.GetEnvironmentVariable("WEB4_API_BASE_URL") ?? "https://api.web4.oasisomniverse.one";
            try
            {
                var client = new Web4SubscriptionAuthorizationClient(Http, web4Base);
                var decision = await client.AuthorizeRequestAsync(auth.Substring("Bearer ".Length).Trim(), "WEB8", context.RequestAborted);
                context.Response.Headers["X-OASIS-Subscription-Plan"] = decision.PlanId ?? string.Empty;
                context.Response.Headers["X-OASIS-Subscription-Limit"] = decision.Limit < 0 ? "unlimited" : decision.Limit.ToString();
                context.Response.Headers["X-OASIS-Subscription-Remaining"] = decision.Remaining < 0 ? "unlimited" : decision.Remaining.ToString();
                if (!decision.Allowed)
                {
                    context.Response.StatusCode = decision.StatusCode;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(decision));
                    return;
                }
                await _next(context);
            }
            catch (Exception)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { isError = true, code = "SUBSCRIPTION_AUTHORITY_UNAVAILABLE", message = "WEB4 subscription authorization is unavailable." }));
            }
        }
    }
}

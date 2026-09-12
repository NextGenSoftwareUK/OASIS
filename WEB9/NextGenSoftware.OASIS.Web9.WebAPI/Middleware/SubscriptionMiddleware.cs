using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace NextGenSoftware.OASIS.Web9.WebAPI.Middleware
{
    /// <summary>
    /// Validates that the caller holds an active OASIS subscription (via WEB4 /api/subscription/subscriptions/me).
    /// Free-plan callers are allowed through; inactive or expired subscriptions return 402.
    /// Must run after JwtMiddleware so the bearer token is already available.
    /// </summary>
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        private static readonly string[] _bypassPaths =
        {
            "/swagger", "/health", "/favicon", "/openapi"
        };

        public SubscriptionMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            string path = context.Request.Path.Value ?? "";
            foreach (var bypass in _bypassPaths)
                if (path.StartsWith(bypass, StringComparison.OrdinalIgnoreCase))
                {
                    await _next(context);
                    return;
                }

            string auth = context.Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                await _next(context);
                return;
            }

            string bearer = auth.Substring("Bearer ".Length).Trim();

            try
            {
                string web4Base = Environment.GetEnvironmentVariable("WEB4_API_BASE_URL")
                    ?? "https://api.oasisomniverse.one";

                using var req = new HttpRequestMessage(HttpMethod.Get, $"{web4Base}/api/subscription/subscriptions/me");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                using var resp = await _http.SendAsync(req);

                if (resp.IsSuccessStatusCode)
                {
                    string json = await resp.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("isActive", out var isActive) && !isActive.GetBoolean())
                    {
                        context.Response.StatusCode = 402;
                        await context.Response.WriteAsync("{\"error\":\"Subscription required. Please upgrade at https://portal.oasisomniverse.one\"}");
                        return;
                    }
                }
            }
            catch
            {
                // If WEB4 is unreachable, allow through to avoid hard dependency
            }

            await _next(context);
        }
    }
}

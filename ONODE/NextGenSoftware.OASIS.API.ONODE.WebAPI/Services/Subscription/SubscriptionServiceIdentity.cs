using System;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>Every mutating protocol call authenticates the service independently of the avatar JWT.</summary>
    public static class SubscriptionServiceIdentity
    {
        public static bool IsService(string service) => service is "WEB5" or "WEB6" or "WEB7" or "WEB8" or "WEB9" or "WEB10";

        public static string RequireService(HttpContext context, IConfiguration configuration, string expectedService = null)
        {
            var serviceHeaders = context.Request.Headers["X-OASIS-Service"];
            var keyHeaders = context.Request.Headers["X-OASIS-Service-Key"];
            if (serviceHeaders.Count != 1 || keyHeaders.Count != 1 || !IsService(serviceHeaders[0]))
                throw new UnauthorizedAccessException("A valid consuming-service identity is required.");
            string service = serviceHeaders[0];
            if (expectedService != null && service != expectedService)
                throw new UnauthorizedAccessException("The request service does not match the authenticated service.");
            string configured = configuration["SUBSCRIPTION_SERVICE_KEY_" + service];
            if (string.IsNullOrWhiteSpace(configured) || Encoding.UTF8.GetByteCount(configured) < 32)
                throw new InvalidOperationException("The consuming-service credential is not configured with at least 32 bytes.");
            // Hash first: fixed-time comparison never reveals key length or matching prefixes.
            byte[] actual = SHA256.HashData(Encoding.UTF8.GetBytes(keyHeaders[0] ?? ""));
            byte[] expected = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
            if (!CryptographicOperations.FixedTimeEquals(actual, expected))
                throw new UnauthorizedAccessException("The consuming-service credential is invalid.");
            return service;
        }

        public static string RequireAdministrator(HttpContext context, IConfiguration configuration)
        {
            var principal = context.User;
            var id = principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal?.FindFirstValue("sub");
            if (principal?.Identity?.IsAuthenticated != true || !Guid.TryParseExact(id, "D", out var avatarId) ||
                !principal.HasClaim("oasis.subscription.admin", "true"))
                throw new UnauthorizedAccessException("A subscription administrator JWT is required.");
            var allowed = (configuration["SUBSCRIPTION_ADMIN_AVATAR_IDS"] ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!allowed.Any(value => Guid.TryParseExact(value, "D", out var candidate) && candidate == avatarId))
                throw new UnauthorizedAccessException("The avatar is not an approved subscription administrator.");
            return avatarId.ToString("D");
        }
    }
}

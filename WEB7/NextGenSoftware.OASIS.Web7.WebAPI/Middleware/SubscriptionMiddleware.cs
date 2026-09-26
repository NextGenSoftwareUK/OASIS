using Microsoft.AspNetCore.Http;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.Web7.WebAPI.Middleware
{
    /// <summary>Service-owned request measurement policy. WEB4 owns all quota and billing decisions.</summary>
    public static class SubscriptionPolicy
    {
        public static UsageEndpointPolicy Resolve(HttpContext context)
        {
            var path = context.Request.Path;
            if (context.GetEndpoint()?.Metadata.GetMetadata<Microsoft.AspNetCore.Authorization.IAllowAnonymous>() != null || context.GetEndpoint() == null || path == "/" ||
                path.StartsWithSegments("/swagger") || path.StartsWithSegments("/health") ||
                path.StartsWithSegments("/api/health") || path.StartsWithSegments("/openapi") ||
                path == "/favicon.ico") return null;
            return new UsageEndpointPolicy
            {
                UsesGraphQlResponseEnvelope = path.StartsWithSegments("/graphql"),
                MeterCategory = "api.request",
                ReservedUnits = 1,
                EstimatedCostUsd = 0m,
                PricingCatalogueVersion = "web7-included-request-v1",
                RequiresProviderMeasurement = false
            };
        }
    }
}

using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;

namespace NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Middleware;

/// <summary>
/// Middleware to enforce rate limiting per merchant tier
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimitService _rateLimitService;
    private readonly ILogger<RateLimitMiddleware> _logger;

    public RateLimitMiddleware(
        RequestDelegate next,
        RateLimitService rateLimitService,
        ILogger<RateLimitMiddleware> logger)
    {
        _next = next;
        _rateLimitService = rateLimitService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IShipexProRepository repository)
    {
        // Skip rate limiting for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Get merchant ID from context (set by auth middleware)
        if (!context.Items.ContainsKey("MerchantId") || 
            context.Items["MerchantId"] is not Guid merchantId)
        {
            await _next(context);
            return;
        }

        try
        {
            // Get merchant to determine tier
            var merchantResult = await repository.GetMerchantAsync(merchantId);
            if (merchantResult.IsError || merchantResult.Result == null)
            {
                await _next(context);
                return;
            }

            var tier = merchantResult.Result.RateLimitTier;
            var rateLimitStatus = _rateLimitService.CheckRateLimit(merchantId, tier.ToString());

            // Add rate limit headers
            context.Response.Headers["X-RateLimit-Limit"] = rateLimitStatus.Limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = rateLimitStatus.Remaining.ToString();
            var resetOffset = new DateTimeOffset(rateLimitStatus.ResetAt);
            context.Response.Headers["X-RateLimit-Reset"] = resetOffset.ToUnixTimeSeconds().ToString();

            // Check if limit exceeded
            if (rateLimitStatus.Remaining < 0)
            {
                _logger.LogWarning("Rate limit exceeded for merchant {MerchantId} (Tier: {Tier})", merchantId, tier);
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new
                {
                    error = "Rate limit exceeded",
                    limit = rateLimitStatus.Limit,
                    resetAt = rateLimitStatus.ResetAt
                });
                await context.Response.WriteAsync(json);
                return;
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in rate limit middleware");
            await _next(context);
        }
    }

    private bool IsPublicEndpoint(PathString path)
    {
        var publicPaths = new[]
        {
            "/api/shipexpro/merchant/register",
            "/api/shipexpro/merchant/login"
        };

        return publicPaths.Any(p => path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Extension method to register the middleware
/// </summary>
public static class RateLimitMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimit(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RateLimitMiddleware>();
    }
}


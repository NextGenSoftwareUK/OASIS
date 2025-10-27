using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubscriptionMiddleware> _logger;
        private readonly IConfiguration _configuration;

        // Pay-as-you-go pricing (based on industry standards)
        private static readonly Dictionary<string, decimal> PayAsYouGoPricing = new()
        {
            { "bronze", 0.001m },    // $0.001 per request over limit
            { "silver", 0.0005m },   // $0.0005 per request over limit  
            { "gold", 0.0002m },     // $0.0002 per request over limit
            { "enterprise", 0.0001m } // $0.0001 per request over limit
        };

        // Plan limits (requests per month)
        private static readonly Dictionary<string, int> PlanLimits = new()
        {
            { "bronze", 10000 },
            { "silver", 100000 },
            { "gold", 1000000 },
            { "enterprise", int.MaxValue } // Unlimited
        };

        public SubscriptionMiddleware(RequestDelegate next, ILogger<SubscriptionMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip subscription checks for certain endpoints
            if (ShouldSkipSubscriptionCheck(context))
            {
                await _next(context);
                return;
            }

            try
            {
                // Get avatar from session (set by JWT middleware)
                var avatar = context.Items["Avatar"] as IAvatar;
                if (avatar == null)
                {
                    await _next(context);
                    return;
                }

                // Get subscription info
                var subscriptionInfo = await GetSubscriptionInfo(avatar.Id);
                if (subscriptionInfo == null)
                {
                    // No subscription - check if this is a free tier request
                    if (IsFreeTierEndpoint(context))
                    {
                        await _next(context);
                        return;
                    }

                    await ReturnSubscriptionRequiredError(context);
                    return;
                }

                // Check if subscription is active
                if (!IsSubscriptionActive(subscriptionInfo))
                {
                    await ReturnInactiveSubscriptionError(context, subscriptionInfo);
                    return;
                }

                // Get current month's usage
                var currentUsage = await GetCurrentMonthUsage(avatar.Id);
                var planLimit = PlanLimits[subscriptionInfo.PlanId];

                // Check if over limit
                if (currentUsage >= planLimit)
                {
                    // Check if pay-as-you-go is enabled
                    if (subscriptionInfo.PayAsYouGoEnabled)
                    {
                        // Log the overage for billing
                        await LogOverageUsage(avatar.Id, subscriptionInfo.PlanId);
                        
                        // Continue with request
                        await _next(context);
                        return;
                    }
                    else
                    {
                        await ReturnLimitExceededError(context, subscriptionInfo, currentUsage, planLimit);
                        return;
                    }
                }

                // Increment usage counter
                await IncrementUsageCounter(avatar.Id);

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubscriptionMiddleware");
                await _next(context);
            }
        }

        private bool ShouldSkipSubscriptionCheck(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // Skip for health checks, auth endpoints, and subscription management
            var skipPaths = new[]
            {
                "/health",
                "/api/health",
                "/api/auth",
                "/api/avatar/signin",
                "/api/avatar/signup",
                "/api/subscription",
                "/swagger",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path?.StartsWith(skipPath) == true);
        }

        private bool IsFreeTierEndpoint(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // Define free tier endpoints
            var freeTierPaths = new[]
            {
                "/api/avatar/profile",
                "/api/avatar/basic-info",
                "/api/health"
            };

            return freeTierPaths.Any(freePath => path?.StartsWith(freePath) == true);
        }

        private async Task<SubscriptionInfo> GetSubscriptionInfo(Guid avatarId)
        {
            try
            {
                // TODO: Implement actual database query
                // For now, return mock data
                return new SubscriptionInfo
                {
                    AvatarId = avatarId,
                    PlanId = "silver", // Mock plan
                    Status = "active",
                    PayAsYouGoEnabled = false,
                    CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
                    CurrentPeriodEnd = DateTime.UtcNow.AddDays(15)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription info for avatar {AvatarId}", avatarId);
                return null;
            }
        }

        private bool IsSubscriptionActive(SubscriptionInfo subscription)
        {
            return subscription.Status == "active" && 
                   DateTime.UtcNow >= subscription.CurrentPeriodStart && 
                   DateTime.UtcNow <= subscription.CurrentPeriodEnd;
        }

        private async Task<int> GetCurrentMonthUsage(Guid avatarId)
        {
            try
            {
                // TODO: Implement actual usage tracking
                // For now, return mock usage
                return 50000; // Mock usage
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting usage for avatar {AvatarId}", avatarId);
                return 0;
            }
        }

        private async Task LogOverageUsage(Guid avatarId, string planId)
        {
            try
            {
                // TODO: Implement overage logging for billing
                var overagePrice = PayAsYouGoPricing[planId];
                _logger.LogInformation("Overage usage logged for avatar {AvatarId}, plan {PlanId}, price per request: {Price}", 
                    avatarId, planId, overagePrice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging overage usage for avatar {AvatarId}", avatarId);
            }
        }

        private async Task IncrementUsageCounter(Guid avatarId)
        {
            try
            {
                // TODO: Implement actual usage counter increment
                _logger.LogDebug("Usage incremented for avatar {AvatarId}", avatarId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing usage for avatar {AvatarId}", avatarId);
            }
        }

        private async Task ReturnSubscriptionRequiredError(HttpContext context)
        {
            context.Response.StatusCode = 402; // Payment Required
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "Subscription Required",
                message = "A valid OASIS subscription is required to access this endpoint.",
                code = "SUBSCRIPTION_REQUIRED",
                upgradeUrl = "/subscription/plans",
                freeTierInfo = new
                {
                    available = true,
                    endpoints = new[] { "/api/avatar/profile", "/api/health" }
                }
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }

        private async Task ReturnInactiveSubscriptionError(HttpContext context, SubscriptionInfo subscription)
        {
            context.Response.StatusCode = 402; // Payment Required
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "Inactive Subscription",
                message = "Your OASIS subscription is not active. Please update your payment method or contact support.",
                code = "INACTIVE_SUBSCRIPTION",
                subscription = new
                {
                    planId = subscription.PlanId,
                    status = subscription.Status,
                    currentPeriodEnd = subscription.CurrentPeriodEnd
                },
                manageUrl = "/subscription/manage"
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }

        private async Task ReturnLimitExceededError(HttpContext context, SubscriptionInfo subscription, int currentUsage, int planLimit)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";

            var errorResponse = new
            {
                error = "Plan Limit Exceeded",
                message = $"You have exceeded your {subscription.PlanId} plan limit of {planLimit:N0} requests this month. You have used {currentUsage:N0} requests.",
                code = "PLAN_LIMIT_EXCEEDED",
                usage = new
                {
                    current = currentUsage,
                    limit = planLimit,
                    remaining = Math.Max(0, planLimit - currentUsage)
                },
                options = new
                {
                    upgradeUrl = "/subscription/plans",
                    enablePayAsYouGoUrl = "/subscription/manage",
                    payAsYouGoPricing = PayAsYouGoPricing[subscription.PlanId],
                    payAsYouGoEnabled = subscription.PayAsYouGoEnabled
                },
                nextBillingDate = subscription.CurrentPeriodEnd
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }

    public class SubscriptionInfo
    {
        public Guid AvatarId { get; set; }
        public string PlanId { get; set; }
        public string Status { get; set; }
        public bool PayAsYouGoEnabled { get; set; }
        public DateTime CurrentPeriodStart { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
    }
}

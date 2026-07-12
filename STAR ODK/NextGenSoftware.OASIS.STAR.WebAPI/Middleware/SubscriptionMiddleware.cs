using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.STAR.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Middleware
{
    public class SubscriptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SubscriptionMiddleware> _logger;

        private static readonly Dictionary<string, int> PlanLimits = new()
        {
            { "free",       1_000 },
            { "bronze",     10_000 },
            { "silver",     100_000 },
            { "gold",       1_000_000 },
            { "enterprise", int.MaxValue }
        };

        private static readonly Dictionary<string, decimal> OveragePricing = new()
        {
            { "bronze",     0.001m },
            { "silver",     0.0005m },
            { "gold",       0.0002m },
            { "enterprise", 0.0001m }
        };

        private static readonly string[] SkipPaths =
        {
            "/health", "/api/health", "/api/auth",
            "/api/avatar/signin", "/api/avatar/signup",
            "/api/subscription", "/swagger", "/favicon.ico"
        };

        private static readonly string[] FreeTierPaths =
        {
            "/api/avatar/profile", "/api/avatar/basic-info"
        };

        public SubscriptionMiddleware(RequestDelegate next, ILogger<SubscriptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            if (SkipPaths.Any(p => path?.StartsWith(p) == true))
            {
                await _next(context);
                return;
            }

            try
            {
                var avatar = context.Items["Avatar"] as IAvatar;
                if (avatar == null)
                {
                    await _next(context);
                    return;
                }

                var svc = context.RequestServices.GetRequiredService<ISubscriptionService>();
                var userId = avatar.Id.ToString();
                var subscription = await svc.GetSubscriptionAsync(userId);

                if (subscription == null || !IsActive(subscription))
                {
                    if (FreeTierPaths.Any(p => path?.StartsWith(p) == true))
                    {
                        await _next(context);
                        return;
                    }

                    if (subscription == null)
                        await WriteJson(context, 402, new { error = "Subscription Required", code = "SUBSCRIPTION_REQUIRED", message = "A valid OASIS subscription is required.", upgradeUrl = "/subscription/plans" });
                    else
                        await WriteJson(context, 402, new { error = "Inactive Subscription", code = "INACTIVE_SUBSCRIPTION", message = "Your subscription is not active.", status = subscription.Status });
                    return;
                }

                var planId = subscription.PlanId ?? "free";
                var limit = PlanLimits.GetValueOrDefault(planId, 1_000);
                var usage = await svc.GetUsageAsync(userId, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

                if (limit >= 0 && usage.RequestCount >= limit)
                {
                    if (subscription.PayAsYouGoEnabled)
                        await svc.IncrementOverageAsync(userId);
                    else
                    {
                        await WriteJson(context, 429, new
                        {
                            error = "Plan Limit Exceeded",
                            code = "PLAN_LIMIT_EXCEEDED",
                            message = $"You have used {usage.RequestCount:N0} of your {limit:N0} monthly requests.",
                            usage = new { current = usage.RequestCount, limit },
                            options = new { upgradeUrl = "/subscription/plans", pricePerRequest = OveragePricing.GetValueOrDefault(planId, 0m) }
                        });
                        return;
                    }
                }
                else
                {
                    await svc.IncrementUsageAsync(userId);
                }

                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubscriptionMiddleware error");
                if (!context.Response.HasStarted)
                    await WriteJson(context, 500, new { error = "Subscription check failed", message = ex.Message });
            }
        }

        private static bool IsActive(SubscriptionRecord sub)
        {
            if (sub.Status is not ("active" or "trialing" or "free")) return false;
            if (sub.CurrentPeriodEnd.HasValue && DateTime.UtcNow > sub.CurrentPeriodEnd.Value) return false;
            return true;
        }

        private static async Task WriteJson(HttpContext context, int status, object body)
        {
            if (context.Response.HasStarted) return;
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(body));
        }
    }
}

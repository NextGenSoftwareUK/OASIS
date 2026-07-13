using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;
using Stripe;
using OASISSub = NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly OASISSub.ISubscriptionService _subscriptionService;

        // Canonical plan definitions — single source of truth for the whole controller
        private static readonly List<PlanDto> Plans = new()
        {
            new PlanDto
            {
                Id = "free",
                Name = "Free",
                PriceMonthly = 0m,
                Currency = "USD",
                Features = new[] { "1,000 API requests/month", "100 MB storage", "Community support" },
                MaxRequestsPerMonth = 1000,
                MaxStorageGB = 0,
                SupportLevel = "Community"
            },
            new PlanDto
            {
                Id = "bronze",
                Name = "Bronze",
                PriceMonthly = 9m,
                Currency = "USD",
                Features = new[] { "10,000 API requests/month", "1 GB storage", "Community support", "Basic analytics" },
                MaxRequestsPerMonth = 10_000,
                MaxStorageGB = 1,
                SupportLevel = "Community"
            },
            new PlanDto
            {
                Id = "silver",
                Name = "Silver",
                PriceMonthly = 29m,
                Currency = "USD",
                Features = new[] { "100,000 API requests/month", "10 GB storage", "Email support", "Priority processing", "Advanced analytics" },
                MaxRequestsPerMonth = 100_000,
                MaxStorageGB = 10,
                SupportLevel = "Email"
            },
            new PlanDto
            {
                Id = "gold",
                Name = "Gold",
                PriceMonthly = 99m,
                Currency = "USD",
                Features = new[] { "1,000,000 API requests/month", "100 GB storage", "Priority support", "Advanced analytics", "Custom integrations", "SLA guarantee" },
                MaxRequestsPerMonth = 1_000_000,
                MaxStorageGB = 100,
                SupportLevel = "Priority"
            },
            new PlanDto
            {
                Id = "enterprise",
                Name = "Enterprise",
                PriceMonthly = 0m,
                Currency = "USD",
                Features = new[] { "Unlimited API requests", "Unlimited storage", "Dedicated support", "SLA & SSO", "On-premise deployment", "Custom contracts" },
                MaxRequestsPerMonth = -1,
                MaxStorageGB = -1,
                SupportLevel = "Dedicated",
                IsContactSales = true
            }
        };

        private static readonly Dictionary<string, int> PlanRequestLimits = Plans
            .Where(p => p.MaxRequestsPerMonth > 0)
            .ToDictionary(p => p.Id, p => p.MaxRequestsPerMonth);

        public SubscriptionController(IConfiguration configuration, OASISSub.ISubscriptionService subscriptionService)
        {
            _configuration = configuration;
            _subscriptionService = subscriptionService;
        }

        // ── Plans ────────────────────────────────────────────────────────────

        [HttpGet("plans")]
        public ActionResult GetPlans()
        {
            return Ok(new { Result = Plans, IsError = false, Message = "Plans loaded successfully" });
        }

        // ── Checkout ─────────────────────────────────────────────────────────

        [HttpPost("checkout/session")]
        public async Task<ActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            if (request == null)
                return BadRequest(new { IsError = true, Message = "Request body required. Provide PlanId and optional SuccessUrl, CancelUrl." });
            if (!ModelState.IsValid)
                return BadRequest(new { IsError = true, Message = "Invalid request." });

            var plan = Plans.FirstOrDefault(p => p.Id == request.PlanId);
            if (plan == null)
                return BadRequest(new { IsError = true, Message = $"Unknown plan '{request.PlanId}'." });

            if (plan.IsContactSales)
                return BadRequest(new { IsError = true, Message = "Enterprise plan requires contacting sales." });

            // Free plan: provision immediately without Stripe
            if (plan.PriceMonthly == 0m)
            {
                var userId = GetCurrentUserId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _subscriptionService.UpsertSubscriptionAsync(new OASISSub.SubscriptionRecord
                    {
                        UserId = userId,
                        PlanId = "free",
                        Status = "active",
                        CurrentPeriodStart = DateTime.UtcNow,
                        CurrentPeriodEnd = DateTime.UtcNow.AddYears(10)
                    });
                }
                var successUrl = request.SuccessUrl ?? "/";
                return Ok(new { IsError = false, Message = "Free plan activated.", SessionUrl = successUrl });
            }

            try
            {
                var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                    ?? _configuration["STRIPE_SECRET_KEY"]
                    ?? OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Stripe?.SecretKey;
                if (string.IsNullOrWhiteSpace(secretKey))
                    return StatusCode(500, new { IsError = true, Message = "Stripe not configured. Set STRIPE_SECRET_KEY environment variable." });

                StripeConfiguration.ApiKey = secretKey;

                var avatarId = GetCurrentUserId() ?? request.AvatarId ?? "anonymous";
                var priceId = await GetOrCreateStripePriceAsync(plan);

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                    {
                        new() { Price = priceId, Quantity = 1 }
                    },
                    Mode = "subscription",
                    SuccessUrl = (request.SuccessUrl ?? "https://oasisomniverse.one/checkout/success") + "?subscribed=1",
                    CancelUrl = request.CancelUrl ?? "https://oasisomniverse.one/checkout/cancel",
                    CustomerEmail = request.CustomerEmail,
                    Metadata = new Dictionary<string, string>
                    {
                        { "avatar_id", avatarId },
                        { "plan_id", plan.Id }
                    }
                };

                var service = new Stripe.Checkout.SessionService();
                var session = await service.CreateAsync(options);

                return Ok(new CreateCheckoutSessionResponse
                {
                    IsError = false,
                    Message = "Checkout session created.",
                    SessionId = session.Id,
                    SessionUrl = session.Url
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsError = true, Message = $"Error creating checkout session: {ex.Message}" });
            }
        }

        // ── Stripe Webhooks ──────────────────────────────────────────────────

        [HttpPost("webhooks/stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                ?? _configuration["STRIPE_WEBHOOK_SECRET"]
                ?? OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.Stripe?.WebhookSecret;
            if (string.IsNullOrWhiteSpace(webhookSecret))
                return BadRequest("Webhook secret not configured.");

            string body;
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync();

            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
                return BadRequest("Missing Stripe-Signature header.");

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(body, signature, webhookSecret);
                await HandleStripeEventAsync(stripeEvent);
                return Ok();
            }
            catch (StripeException ex)
            {
                return BadRequest($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        private async Task HandleStripeEventAsync(Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await OnCheckoutCompletedAsync(stripeEvent.Data.Object as Stripe.Checkout.Session);
                    break;
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await OnSubscriptionUpdatedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                    break;
                case "customer.subscription.deleted":
                    await OnSubscriptionDeletedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                    break;
                case "invoice.payment_succeeded":
                    await OnPaymentSucceededAsync(stripeEvent.Data.Object as Invoice);
                    break;
                case "invoice.payment_failed":
                    await OnPaymentFailedAsync(stripeEvent.Data.Object as Invoice);
                    break;
            }
        }

        private async Task OnCheckoutCompletedAsync(Stripe.Checkout.Session session)
        {
            if (session == null) return;
            var avatarId = session.Metadata?.GetValueOrDefault("avatar_id");
            var planId = session.Metadata?.GetValueOrDefault("plan_id");
            if (string.IsNullOrEmpty(avatarId) || string.IsNullOrEmpty(planId)) return;

            await _subscriptionService.UpsertSubscriptionAsync(new OASISSub.SubscriptionRecord
            {
                UserId = avatarId,
                PlanId = planId,
                Status = "active",
                StripeCustomerId = session.CustomerId,
                StripeSubscriptionId = session.SubscriptionId,
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            });

            var plan = Plans.FirstOrDefault(p => p.Id == planId);
            await _subscriptionService.AddOrderAsync(new OASISSub.OrderRecord
            {
                UserId = avatarId,
                PlanId = planId,
                Description = $"{plan?.Name ?? planId} — monthly subscription",
                Amount = plan?.PriceMonthly ?? 0m,
                Currency = "USD",
                Status = "paid",
                StripeInvoiceId = session.Id
            });
        }

        private async Task OnSubscriptionUpdatedAsync(Stripe.Subscription subscription)
        {
            if (subscription == null) return;
            var record = await _subscriptionService.GetSubscriptionByStripeSubscriptionIdAsync(subscription.Id)
                      ?? await _subscriptionService.GetSubscriptionByStripeCustomerIdAsync(subscription.CustomerId);
            if (record == null) return;

            var planId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Metadata?.GetValueOrDefault("plan_id")
                      ?? record.PlanId;

            record.Status = subscription.Status;
            record.PlanId = planId;
            record.StripeCustomerId = subscription.CustomerId;
            record.StripeSubscriptionId = subscription.Id;
            record.CurrentPeriodStart = subscription.CurrentPeriodStart;
            record.CurrentPeriodEnd = subscription.CurrentPeriodEnd;
            await _subscriptionService.UpsertSubscriptionAsync(record);
        }

        private async Task OnSubscriptionDeletedAsync(Stripe.Subscription subscription)
        {
            if (subscription == null) return;
            var record = await _subscriptionService.GetSubscriptionByStripeSubscriptionIdAsync(subscription.Id)
                      ?? await _subscriptionService.GetSubscriptionByStripeCustomerIdAsync(subscription.CustomerId);
            if (record == null) return;

            record.Status = "cancelled";
            await _subscriptionService.UpsertSubscriptionAsync(record);
        }

        private async Task OnPaymentSucceededAsync(Invoice invoice)
        {
            if (invoice?.SubscriptionId == null) return;
            var record = await _subscriptionService.GetSubscriptionByStripeSubscriptionIdAsync(invoice.SubscriptionId);
            if (record == null) return;

            record.Status = "active";
            record.CurrentPeriodStart = invoice.PeriodStart;
            record.CurrentPeriodEnd = invoice.PeriodEnd;
            await _subscriptionService.UpsertSubscriptionAsync(record);

            var plan = Plans.FirstOrDefault(p => p.Id == record.PlanId);
            await _subscriptionService.AddOrderAsync(new OASISSub.OrderRecord
            {
                UserId = record.UserId,
                PlanId = record.PlanId,
                Description = $"{plan?.Name ?? record.PlanId} — renewal",
                Amount = (invoice.AmountPaid / 100m),
                Currency = invoice.Currency?.ToUpperInvariant() ?? "USD",
                Status = "paid",
                StripeInvoiceId = invoice.Id
            });
        }

        private async Task OnPaymentFailedAsync(Invoice invoice)
        {
            if (invoice?.SubscriptionId == null) return;
            var record = await _subscriptionService.GetSubscriptionByStripeSubscriptionIdAsync(invoice.SubscriptionId);
            if (record == null) return;

            record.Status = "past_due";
            await _subscriptionService.UpsertSubscriptionAsync(record);
        }

        // ── My Subscriptions ─────────────────────────────────────────────────

        [HttpGet("subscriptions/me")]
        public async Task<ActionResult> GetMySubscriptions()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { IsError = true, Message = "User not authenticated." });

            var record = await _subscriptionService.GetSubscriptionAsync(userId);
            if (record == null)
                return Ok(new { Result = Array.Empty<object>(), IsError = false, Message = "No active subscription." });

            var plan = Plans.FirstOrDefault(p => p.Id == record.PlanId);
            var result = new[]
            {
                new
                {
                    Id = record.StripeSubscriptionId ?? $"sub_{record.UserId[..Math.Min(8, record.UserId.Length)]}",
                    record.PlanId,
                    PlanName = plan?.Name ?? record.PlanId,
                    record.Status,
                    RenewsOn = record.CurrentPeriodEnd,
                    record.StripeCustomerId,
                    record.StripeSubscriptionId,
                    record.CurrentPeriodStart,
                    record.CurrentPeriodEnd,
                    record.PayAsYouGoEnabled,
                    record.CreatedAt
                }
            };

            return Ok(new { Result = result, IsError = false, Message = "Subscription loaded." });
        }

        // ── Orders ───────────────────────────────────────────────────────────

        [HttpGet("orders/me")]
        public async Task<ActionResult> GetMyOrders()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { IsError = true, Message = "User not authenticated." });

            var orders = await _subscriptionService.GetOrdersAsync(userId);
            return Ok(new { Result = orders, IsError = false, Message = "Orders loaded." });
        }

        // ── Pay-as-you-go ────────────────────────────────────────────────────

        [HttpPost("toggle-pay-as-you-go")]
        public async Task<IActionResult> TogglePayAsYouGo([FromBody] TogglePayAsYouGoRequest request)
        {
            if (request == null)
                return BadRequest(new { IsError = true, Message = "Request body required." });

            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { IsError = true, Message = "User not authenticated." });

            await _subscriptionService.SetPayAsYouGoAsync(userId, request.Enabled);

            return Ok(new
            {
                IsError = false,
                Success = true,
                PayAsYouGoEnabled = request.Enabled,
                Message = request.Enabled
                    ? "Pay-as-you-go enabled. Requests over your plan limit will be billed per-request."
                    : "Pay-as-you-go disabled. Requests will stop at your plan limit."
            });
        }

        // ── Usage ─────────────────────────────────────────────────────────────

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { IsError = true, Message = "User not authenticated." });

            var now = DateTime.UtcNow;
            var usage = await _subscriptionService.GetUsageAsync(userId, now.Year, now.Month);
            var record = await _subscriptionService.GetSubscriptionAsync(userId);
            var planId = record?.PlanId ?? "free";
            var plan = Plans.FirstOrDefault(p => p.Id == planId);
            var limit = plan?.MaxRequestsPerMonth ?? 1000;

            return Ok(new
            {
                currentMonth = new
                {
                    requests = usage.RequestCount,
                    limit,
                    remaining = limit < 0 ? -1 : Math.Max(0, limit - usage.RequestCount),
                    overage = Math.Max(0, usage.RequestCount - limit)
                },
                payAsYouGoEnabled = record?.PayAsYouGoEnabled ?? false,
                overageCharges = new
                {
                    currentMonth = usage.OverageCount * OveragePriceFor(planId),
                    currency = "USD"
                },
                subscription = new
                {
                    planId,
                    planName = plan?.Name ?? planId,
                    status = record?.Status ?? "free",
                    currentPeriodStart = record?.CurrentPeriodStart,
                    currentPeriodEnd = record?.CurrentPeriodEnd
                }
            });
        }

        // ── HyperDrive ───────────────────────────────────────────────────────

        [HttpPost("update-hyperdrive-config")]
        public async Task<ActionResult<OASISResult<bool>>> UpdateHyperDriveConfig([FromBody] UpdateHyperDriveConfigRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<bool> { IsError = true, Message = "Request body required." });

            try
            {
                var dna = OASISDNAManager.OASISDNA.OASIS;
                if (dna != null)
                {
                    dna.SubscriptionConfig.PlanType = request.PlanType;
                    dna.SubscriptionConfig.PayAsYouGoEnabled = request.PayAsYouGoEnabled;

                    var limits = HyperDriveLimitsFor(request.PlanType);
                    dna.SubscriptionConfig.MaxReplicationsPerMonth = limits.MaxReplications;
                    dna.SubscriptionConfig.MaxFailoversPerMonth = limits.MaxFailovers;
                    dna.SubscriptionConfig.MaxStorageGB = limits.MaxStorageGB;
                    dna.SubscriptionConfig.CostPerReplication = limits.CostPerReplication;
                    dna.SubscriptionConfig.CostPerFailover = limits.CostPerFailover;
                    dna.SubscriptionConfig.CostPerGB = limits.CostPerGB;

                    bool freeOnly = request.PlanType == "free" || request.PlanType == "Free";
                    dna.ReplicationRules.FreeProvidersOnly = freeOnly;
                    dna.ReplicationRules.CostThreshold = freeOnly ? 0 : limits.CostThreshold;
                    dna.FailoverRules.FreeProvidersOnly = freeOnly;
                    dna.FailoverRules.CostThreshold = freeOnly ? 0 : limits.CostThreshold;

                    await OASISDNAManager.SaveDNAAsync();
                }

                return Ok(new OASISResult<bool> { Result = true, Message = "HyperDrive configuration updated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message, Exception = ex });
            }
        }

        [HttpGet("hyperdrive-usage")]
        public async Task<ActionResult<OASISResult<HyperDriveUsageDto>>> GetHyperDriveUsage()
        {
            var userId = GetCurrentUserId();
            var dna = OASISDNAManager.OASISDNA.OASIS;
            var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();

            var now = DateTime.UtcNow;
            var usage = string.IsNullOrEmpty(userId)
                ? new OASISSub.UsageRecord()
                : await _subscriptionService.GetUsageAsync(userId, now.Year, now.Month);

            var dto = new HyperDriveUsageDto
            {
                PlanType = config.PlanType,
                PayAsYouGoEnabled = config.PayAsYouGoEnabled,
                CurrentUsage = new System.Collections.Generic.Dictionary<string, long>
                {
                    { "Replications", 0 },
                    { "Failovers", 0 },
                    { "StorageGB", (long)usage.StorageUsedGB },
                    { "Requests", usage.RequestCount }
                },
                Limits = new System.Collections.Generic.Dictionary<string, int>
                {
                    { "Replications", config.MaxReplicationsPerMonth },
                    { "Failovers", config.MaxFailoversPerMonth },
                    { "StorageGB", config.MaxStorageGB },
                    { "Requests", Plans.FirstOrDefault(p => p.Id == config.PlanType.ToLower())?.MaxRequestsPerMonth ?? 1000 }
                },
                Costs = new System.Collections.Generic.Dictionary<string, decimal>
                {
                    { "PerReplication", config.CostPerReplication },
                    { "PerFailover", config.CostPerFailover },
                    { "PerGB", config.CostPerGB }
                }
            };

            return Ok(new OASISResult<HyperDriveUsageDto> { Result = dto, Message = "HyperDrive usage retrieved." });
        }

        [HttpPost("check-hyperdrive-quota")]
        public async Task<ActionResult<OASISResult<QuotaCheckResult>>> CheckHyperDriveQuota([FromBody] QuotaCheckRequest request)
        {
            if (request == null)
                return BadRequest(new OASISResult<QuotaCheckResult> { IsError = true, Message = "Request body required." });

            var dna = OASISDNAManager.OASISDNA.OASIS;
            var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();
            var limits = HyperDriveLimitsFor(config.PlanType);

            var userId = GetCurrentUserId();
            var now = DateTime.UtcNow;
            var usage = string.IsNullOrEmpty(userId)
                ? new OASISSub.UsageRecord()
                : await _subscriptionService.GetUsageAsync(userId, now.Year, now.Month);

            int currentUsage = request.OperationType switch
            {
                "Requests" => (int)Math.Min(usage.RequestCount, int.MaxValue),
                _ => 0
            };
            int limit = request.OperationType switch
            {
                "Replications" => limits.MaxReplications,
                "Failovers" => limits.MaxFailovers,
                "Storage" or "StorageGB" => limits.MaxStorageGB,
                "Requests" => Plans.FirstOrDefault(p => p.Id == config.PlanType.ToLower())?.MaxRequestsPerMonth ?? 1000,
                _ => 0
            };

            var result = new QuotaCheckResult
            {
                CanProceed = currentUsage < limit || limit < 0,
                CurrentUsage = currentUsage,
                Limit = limit,
                Remaining = limit < 0 ? int.MaxValue : Math.Max(0, limit - currentUsage),
                WouldExceedQuota = limit >= 0 && currentUsage >= limit,
                RequiresPayAsYouGo = limit >= 0 && currentUsage >= limit && config.PayAsYouGoEnabled,
                EstimatedCost = currentUsage >= limit ? GetEstimatedCost(request.OperationType, config) : 0
            };

            return Ok(new OASISResult<QuotaCheckResult> { Result = result, Message = "Quota check complete." });
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private string GetCurrentUserId()
        {
            if (HttpContext.Items.TryGetValue("Avatar", out var avatarObj) && avatarObj is IAvatar avatar)
                return avatar.Id.ToString();

            // Fallback: JWT sub claim
            return User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value;
        }

        private static decimal OveragePriceFor(string planId) => planId switch
        {
            "bronze" => 0.001m,
            "silver" => 0.0005m,
            "gold" => 0.0002m,
            "enterprise" => 0.0001m,
            _ => 0m
        };

        private static HyperDriveLimits HyperDriveLimitsFor(string planType) => planType?.ToLower() switch
        {
            "bronze" or "basic" => new HyperDriveLimits { MaxReplications = 1000, MaxFailovers = 100, MaxStorageGB = 1, CostThreshold = 10, CostPerReplication = 0.01m, CostPerFailover = 0.05m, CostPerGB = 0.10m },
            "silver" or "pro" => new HyperDriveLimits { MaxReplications = 10_000, MaxFailovers = 1000, MaxStorageGB = 10, CostThreshold = 50, CostPerReplication = 0.005m, CostPerFailover = 0.025m, CostPerGB = 0.05m },
            "gold" => new HyperDriveLimits { MaxReplications = 100_000, MaxFailovers = 10_000, MaxStorageGB = 100, CostThreshold = 100, CostPerReplication = 0.002m, CostPerFailover = 0.01m, CostPerGB = 0.02m },
            "enterprise" => new HyperDriveLimits { MaxReplications = int.MaxValue, MaxFailovers = int.MaxValue, MaxStorageGB = int.MaxValue, CostThreshold = 0, CostPerReplication = 0.001m, CostPerFailover = 0.005m, CostPerGB = 0.01m },
            _ => new HyperDriveLimits { MaxReplications = 100, MaxFailovers = 10, MaxStorageGB = 0, CostThreshold = 0 }
        };

        private static decimal GetEstimatedCost(string operationType, SubscriptionConfig config) => operationType switch
        {
            "Replications" => config.CostPerReplication,
            "Failovers" => config.CostPerFailover,
            "Storage" or "StorageGB" => config.CostPerGB,
            _ => 0m
        };

        private async Task<string> GetOrCreateStripePriceAsync(PlanDto plan)
        {
            var priceService = new PriceService();
            var prices = await priceService.ListAsync(new PriceListOptions { Active = true, Limit = 100 });
            var existing = prices.Data.FirstOrDefault(p =>
                p.UnitAmount == (long)(plan.PriceMonthly * 100) &&
                p.Currency == plan.Currency.ToLower() &&
                p.Recurring?.Interval == "month" &&
                p.Metadata.ContainsKey("plan_id") && p.Metadata["plan_id"] == plan.Id);
            if (existing != null) return existing.Id;

            var productService = new ProductService();
            var product = await productService.CreateAsync(new ProductCreateOptions
            {
                Name = $"OASIS {plan.Name} Plan",
                Description = string.Join(", ", plan.Features),
                Metadata = new Dictionary<string, string> { { "plan_id", plan.Id } }
            });

            var price = await priceService.CreateAsync(new PriceCreateOptions
            {
                Product = product.Id,
                UnitAmount = (long)(plan.PriceMonthly * 100),
                Currency = plan.Currency.ToLower(),
                Recurring = new PriceRecurringOptions { Interval = "month" },
                Metadata = new Dictionary<string, string>
                {
                    { "plan_id", plan.Id },
                    { "max_requests", plan.MaxRequestsPerMonth.ToString() },
                    { "max_storage_gb", plan.MaxStorageGB.ToString() }
                }
            });

            return price.Id;
        }

        // ── DTOs / inner types ───────────────────────────────────────────────

        public class PlanDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public decimal PriceMonthly { get; set; }
            public string Currency { get; set; }
            public string[] Features { get; set; }
            public bool IsContactSales { get; set; }
            public int MaxRequestsPerMonth { get; set; }
            public int MaxStorageGB { get; set; }
            public string SupportLevel { get; set; }
        }

        public class CreateCheckoutSessionRequest
        {
            [Required]
            public string PlanId { get; set; }
            public string SuccessUrl { get; set; }
            public string CancelUrl { get; set; }
            public string CouponCode { get; set; }
            public string CustomerEmail { get; set; }
            public string AvatarId { get; set; }
        }

        public class CreateCheckoutSessionResponse
        {
            public bool IsError { get; set; }
            public string Message { get; set; }
            public string SessionId { get; set; }
            public string SessionUrl { get; set; }
        }

        public class TogglePayAsYouGoRequest
        {
            public bool Enabled { get; set; }
        }

        public class UpdateHyperDriveConfigRequest
        {
            public string PlanType { get; set; }
            public bool PayAsYouGoEnabled { get; set; }
        }

        public class HyperDriveUsageDto
        {
            public string PlanType { get; set; }
            public bool PayAsYouGoEnabled { get; set; }
            public Dictionary<string, long> CurrentUsage { get; set; }
            public Dictionary<string, int> Limits { get; set; }
            public Dictionary<string, decimal> Costs { get; set; }
        }

        public class QuotaCheckRequest
        {
            public string OperationType { get; set; }
        }

        public class QuotaCheckResult
        {
            public bool CanProceed { get; set; }
            public int CurrentUsage { get; set; }
            public int Limit { get; set; }
            public int Remaining { get; set; }
            public bool WouldExceedQuota { get; set; }
            public bool RequiresPayAsYouGo { get; set; }
            public decimal EstimatedCost { get; set; }
        }

        private class HyperDriveLimits
        {
            public int MaxReplications { get; set; }
            public int MaxFailovers { get; set; }
            public int MaxStorageGB { get; set; }
            public decimal CostThreshold { get; set; }
            public decimal CostPerReplication { get; set; }
            public decimal CostPerFailover { get; set; }
            public decimal CostPerGB { get; set; }
            public string PlanType { get; set; }
        }
    }

}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.SubscriptionStore;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System.Linq;
using Stripe;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionStore _store;

        /// <summary>In-memory credit balance fallback when MongoDB is not configured.</summary>
        private static readonly ConcurrentDictionary<string, decimal> CreditsBalanceByAvatar = new();

        private static readonly decimal[] CreditsPresetAmounts = { 20m, 50m, 100m, 250m };

        public SubscriptionController(IConfiguration configuration, ISubscriptionStore store)
        {
            _configuration = configuration;
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        [HttpGet("plans")]
        public ActionResult<IEnumerable<PlanDto>> GetPlans()
        {
            try
            {
                // Load plans from configuration or database
                var plans = LoadSubscriptionPlans();
                return Ok(new { Result = plans, IsError = false, Message = "Plans loaded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsError = true, Message = $"Error loading plans: {ex.Message}" });
    }
}

        private List<PlanDto> LoadSubscriptionPlans()
        {
            // Load from configuration or database
            var plans = new List<PlanDto>
            {
                new PlanDto 
                { 
                    Id = "bronze", 
                    Name = "Bronze", 
                    PriceMonthly = 9.0m, 
                    Currency = "USD", 
                    Features = new[]{"Starter API limits", "Community support", "Basic analytics"},
                    MaxRequestsPerMonth = 10000,
                    MaxStorageGB = 1,
                    SupportLevel = "Community"
                },
                new PlanDto 
                { 
                    Id = "silver", 
                    Name = "Silver", 
                    PriceMonthly = 29.0m, 
                    Currency = "USD", 
                    Features = new[]{"Higher API limits", "Email support", "Basic analytics", "Priority processing"},
                    MaxRequestsPerMonth = 100000,
                    MaxStorageGB = 10,
                    SupportLevel = "Email"
                },
                new PlanDto 
                { 
                    Id = "gold", 
                    Name = "Gold", 
                    PriceMonthly = 99.0m, 
                    Currency = "USD", 
                    Features = new[]{"Premium API limits", "Priority support", "Advanced analytics", "Custom integrations"},
                    MaxRequestsPerMonth = 1000000,
                    MaxStorageGB = 100,
                    SupportLevel = "Priority"
                },
                new PlanDto 
                { 
                    Id = "enterprise", 
                    Name = "Enterprise", 
                    PriceMonthly = 0.0m, 
                    Currency = "USD", 
                    Features = new[]{"Custom limits", "SLA & SSO", "Dedicated support", "On-premise deployment"},
                    MaxRequestsPerMonth = -1, // Unlimited
                    MaxStorageGB = -1, // Unlimited
                    SupportLevel = "Dedicated",
                    IsContactSales = true 
                }
            };

            return plans;
        }

        [HttpPost("checkout/session")]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { IsError = true, Message = "Invalid request" });

            try
            {
                // Validate Stripe configuration
                var publishableKey = _configuration["STRIPE_PUBLISHABLE_KEY"];
                var secretKey = _configuration["STRIPE_SECRET_KEY"];
                var webhookSecret = _configuration["STRIPE_WEBHOOK_SECRET"];

                if (string.IsNullOrWhiteSpace(publishableKey) || string.IsNullOrWhiteSpace(secretKey))
                    return StatusCode(500, new { IsError = true, Message = "Stripe keys not configured. Set STRIPE_PUBLISHABLE_KEY and STRIPE_SECRET_KEY." });

                // Create Stripe checkout session
                var session = await CreateStripeCheckoutSessionAsync(request);
                
                return Ok(new CreateCheckoutSessionResponse
                {
                    IsError = false,
                    Message = "Checkout session created successfully",
                    SessionId = session.Id,
                    SessionUrl = session.Url
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsError = true, Message = $"Error creating checkout session: {ex.Message}" });
            }
        }

        /// <summary>Get credit balance for the current avatar (or by avatarId when provided).</summary>
        [HttpGet("balance")]
        public async Task<ActionResult<object>> GetCreditsBalance([FromQuery] string avatarId = null)
        {
            var id = avatarId ?? GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(id))
                return Ok(new { Result = new { CreditsBalanceUsd = 0m, Currency = "USD" }, IsError = false });
            var balance = _store.IsConfigured
                ? await _store.GetCreditsBalanceAsync(id).ConfigureAwait(false)
                : CreditsBalanceByAvatar.GetValueOrDefault(id, 0m);
            return Ok(new { Result = new { CreditsBalanceUsd = balance, Currency = "USD" }, IsError = false });
        }

        /// <summary>Create a one-time Stripe Checkout session to buy credits (Anthropic-style).</summary>
        [HttpPost("checkout/credits")]
        public async Task<ActionResult<CreateCheckoutSessionResponse>> CreateCreditsCheckoutSession([FromBody] BuyCreditsRequest request)
        {
            if (request == null)
                return BadRequest(new { IsError = true, Message = "Invalid request" });
            if (!CreditsPresetAmounts.Contains(request.AmountUsd))
                return BadRequest(new { IsError = true, Message = $"Amount must be one of: {string.Join(", ", CreditsPresetAmounts)} USD." });
            if (string.IsNullOrWhiteSpace(request.AvatarId))
                return BadRequest(new { IsError = true, Message = "Sign in to buy credits. AvatarId is required." });

            try
            {
                var publishableKey = _configuration["STRIPE_PUBLISHABLE_KEY"];
                var secretKey = _configuration["STRIPE_SECRET_KEY"];
                if (string.IsNullOrWhiteSpace(secretKey))
                    return StatusCode(500, new { IsError = true, Message = "Stripe keys not configured." });

                StripeConfiguration.ApiKey = secretKey;
                var session = await CreateCreditsStripeSessionAsync(request);
                return Ok(new CreateCheckoutSessionResponse
                {
                    IsError = false,
                    Message = "Checkout session created",
                    SessionId = session.Id,
                    SessionUrl = session.Url
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsError = true, Message = $"Error creating credits checkout: {ex.Message}" });
            }
        }

        private async Task<Stripe.Checkout.Session> CreateCreditsStripeSessionAsync(BuyCreditsRequest request)
        {
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        PriceData = new Stripe.Checkout.SessionLineItemPriceDataOptions
                        {
                            Currency = "usd",
                            UnitAmount = (long)(request.AmountUsd * 100),
                            ProductData = new Stripe.Checkout.SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "OASIS API Credits",
                                Description = $"${request.AmountUsd:N0} in prepaid API credits. Use until depleted.",
                                Metadata = new Dictionary<string, string> { { "type", "credits" } }
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = request.SuccessUrl ?? "https://oasisweb4.com/checkout-success.html?type=credits",
                CancelUrl = request.CancelUrl ?? "https://oasisweb4.com/pricing.html",
                CustomerEmail = request.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "type", "credits" },
                    { "avatar_id", request.AvatarId },
                    { "amount_usd", request.AmountUsd.ToString("F2") }
                }
            };
            var service = new Stripe.Checkout.SessionService();
            return await service.CreateAsync(options);
        }

        private async Task<Stripe.Checkout.Session> CreateStripeCheckoutSessionAsync(CreateCheckoutSessionRequest request)
        {
            // Set Stripe API key
            Stripe.StripeConfiguration.ApiKey = _configuration["STRIPE_SECRET_KEY"];

            // Get plan details
            var plan = LoadSubscriptionPlans().FirstOrDefault(p => p.Id == request.PlanId);
            if (plan == null)
                throw new ArgumentException($"Plan {request.PlanId} not found");

            // Create price if it doesn't exist
            var priceId = await GetOrCreateStripePriceAsync(plan);

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<Stripe.Checkout.SessionLineItemOptions>
                {
                    new Stripe.Checkout.SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1,
                    },
                },
                Mode = "subscription",
                SuccessUrl = request.SuccessUrl ?? "https://oasisweb4.com/checkout/success",
                CancelUrl = request.CancelUrl ?? "https://oasisweb4.com/checkout/cancel",
                CustomerEmail = request.CustomerEmail,
                Metadata = new Dictionary<string, string>
                {
                    { "plan_id", plan.Id },
                    { "avatar_id", request.AvatarId ?? "anonymous" }
                }
            };

            var service = new Stripe.Checkout.SessionService();
            return await service.CreateAsync(options);
        }

        private async Task<string> GetOrCreateStripePriceAsync(PlanDto plan)
        {
            if (plan.IsContactSales)
                throw new InvalidOperationException("Enterprise plan requires contact sales");

            // Check if price already exists
            var priceService = new PriceService();
            var existingPrices = await priceService.ListAsync(new PriceListOptions
            {
                Active = true,
                Limit = 100
            });

            var existingPrice = existingPrices.Data.FirstOrDefault(p => 
                p.UnitAmount == (long)(plan.PriceMonthly * 100) && 
                p.Currency == plan.Currency.ToLower() &&
                p.Recurring?.Interval == "month");

            if (existingPrice != null)
                return existingPrice.Id;

            // Create new price
            var productService = new ProductService();
            var product = await productService.CreateAsync(new ProductCreateOptions
            {
                Name = plan.Name,
                Description = string.Join(", ", plan.Features),
                Metadata = new Dictionary<string, string>
                {
                    { "plan_id", plan.Id },
                    { "max_requests", plan.MaxRequestsPerMonth.ToString() },
                    { "max_storage_gb", plan.MaxStorageGB.ToString() },
                    { "support_level", plan.SupportLevel }
                }
            });

            var price = await priceService.CreateAsync(new PriceCreateOptions
            {
                Product = product.Id,
                UnitAmount = (long)(plan.PriceMonthly * 100),
                Currency = plan.Currency.ToLower(),
                Recurring = new PriceRecurringOptions
                {
                    Interval = "month"
                }
            });

            return price.Id;
        }

        [HttpPost("webhooks/stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var webhookSecret = _configuration["STRIPE_WEBHOOK_SECRET"];
                if (string.IsNullOrWhiteSpace(webhookSecret))
                    return BadRequest("Webhook secret not configured");

                // Read the request body
                using var reader = new System.IO.StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                // Get the signature header
                var signatureHeader = Request.Headers["Stripe-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(signatureHeader))
                    return BadRequest("Missing Stripe signature");

                // Verify the webhook signature
                var stripeEvent = EventUtility.ConstructEvent(body, signatureHeader, webhookSecret);

                // Handle the event
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
                    await HandleCheckoutSessionCompletedAsync(stripeEvent.Data.Object as Stripe.Checkout.Session);
                    break;

                case "customer.subscription.created":
                    await HandleSubscriptionCreatedAsync(stripeEvent.Data.Object as Subscription);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as Subscription);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Subscription);
                    break;

                case "invoice.payment_succeeded":
                    await HandlePaymentSucceededAsync(stripeEvent.Data.Object as Invoice);
                    break;

                case "invoice.payment_failed":
                    await HandlePaymentFailedAsync(stripeEvent.Data.Object as Invoice);
                    break;

                default:
                    // Log unhandled event types
                    Console.WriteLine($"Unhandled event type: {stripeEvent.Type}");
                    break;
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(Stripe.Checkout.Session session)
        {
            if (session.Mode == "payment")
            {
                var type = session.Metadata?.GetValueOrDefault("type");
                if (type == "credits")
                {
                    var avatarId = session.Metadata?.GetValueOrDefault("avatar_id");
                    var amountStr = session.Metadata?.GetValueOrDefault("amount_usd");
                    if (!string.IsNullOrEmpty(avatarId) && decimal.TryParse(amountStr, out var amountUsd) && amountUsd > 0)
                    {
                        if (_store.IsConfigured)
                            await _store.AddCreditsAsync(avatarId, amountUsd).ConfigureAwait(false);
                        else
                            CreditsBalanceByAvatar.AddOrUpdate(avatarId, amountUsd, (_, existing) => existing + amountUsd);
                    }
                }
                return;
            }

            // Create or update user subscription
            var userId = session.Metadata?.GetValueOrDefault("user_id") ?? session.Metadata?.GetValueOrDefault("avatar_id");
            var planId = session.Metadata?.GetValueOrDefault("plan_id");
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(planId))
            {
                await UpdateUserSubscriptionAsync(userId, planId, session.CustomerId, session.SubscriptionId);
            }
        }

        private async Task HandleSubscriptionCreatedAsync(Subscription subscription)
        {
            await UpdateSubscriptionStatusAsync(
                subscription.Id, "active", subscription.CustomerId,
                subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd);
        }

        private async Task HandleSubscriptionUpdatedAsync(Subscription subscription)
        {
            await UpdateSubscriptionStatusAsync(
                subscription.Id, subscription.Status, subscription.CustomerId,
                subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd);
        }

        private async Task HandleSubscriptionDeletedAsync(Subscription subscription)
        {
            await UpdateSubscriptionStatusAsync(subscription.Id, "cancelled", subscription.CustomerId);
        }

        private async Task HandlePaymentSucceededAsync(Invoice invoice)
        {
            if (invoice.SubscriptionId != null)
                await UpdateSubscriptionStatusAsync(invoice.SubscriptionId, "active", invoice.CustomerId);
        }

        private async Task HandlePaymentFailedAsync(Invoice invoice)
        {
            if (invoice.SubscriptionId != null)
                await UpdateSubscriptionStatusAsync(invoice.SubscriptionId, "past_due", invoice.CustomerId);
        }

        private async Task UpdateUserSubscriptionAsync(string userId, string planId, string customerId, string subscriptionId)
        {
            if (!_store.IsConfigured)
            {
                Console.WriteLine($"Updated subscription for user {userId}: plan={planId}, customer={customerId}, subscription={subscriptionId}");
                return;
            }
            var periodStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var periodEnd = periodStart.AddMonths(1);
            await _store.UpsertSubscriptionAsync(new SubscriptionRecord
            {
                AvatarId = userId,
                PlanId = planId,
                Status = "active",
                StripeCustomerId = customerId,
                StripeSubscriptionId = subscriptionId,
                CurrentPeriodStart = periodStart,
                CurrentPeriodEnd = periodEnd,
                PayAsYouGoEnabled = false,
                UpdatedAt = DateTime.UtcNow
            }).ConfigureAwait(false);
        }

        private async Task UpdateSubscriptionStatusAsync(string subscriptionId, string status, string customerId, DateTime? periodStart = null, DateTime? periodEnd = null)
        {
            if (!_store.IsConfigured)
            {
                Console.WriteLine($"Updated subscription {subscriptionId} status to {status} for customer {customerId}");
                return;
            }
            var record = await _store.GetSubscriptionByStripeSubscriptionIdAsync(subscriptionId).ConfigureAwait(false);
            if (record == null) return;
            record.Status = status;
            record.UpdatedAt = DateTime.UtcNow;
            if (periodStart.HasValue) record.CurrentPeriodStart = periodStart.Value;
            if (periodEnd.HasValue) record.CurrentPeriodEnd = periodEnd.Value;
            await _store.UpsertSubscriptionAsync(record).ConfigureAwait(false);
        }

        [HttpGet("subscriptions/me")]
        public async Task<ActionResult<object>> GetMySubscriptions()
        {
            try
            {
                // Get user ID from authentication token
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { IsError = true, Message = "User not authenticated" });

                // Query user subscriptions from database
                var subscriptions = await GetUserSubscriptionsAsync(userId);
                
                return Ok(new { Result = subscriptions, IsError = false, Message = "Subscriptions loaded successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsError = true, Message = $"Error loading subscriptions: {ex.Message}" });
            }
        }

        private async Task<List<object>> GetUserSubscriptionsAsync(string userId)
        {
            // This would typically query a database
            // For now, return mock data
            return new List<object>
            {
                new 
                { 
                    Id = "sub_test_123", 
                    PlanId = "silver", 
                    Status = "active", 
                    RenewsOn = DateTime.UtcNow.AddMonths(1),
                    CustomerId = "cus_test_123",
                    CreatedAt = DateTime.UtcNow.AddMonths(-1),
                    CurrentPeriodStart = DateTime.UtcNow,
                    CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
                }
            };
        }

        [HttpGet("orders/me")]
        public ActionResult<object> GetMyOrders()
        {
            var orders = new []
            {
                new { Id = "ord_test_123", PlanId = "silver", Amount = 29.0m, Currency = "USD", CreatedOn = DateTime.UtcNow }
            };
            return Ok(new { Result = orders, IsError = false, Message = "Orders loaded" });
        }

        [HttpPost("toggle-pay-as-you-go")]
        public async Task<IActionResult> TogglePayAsYouGo([FromBody] TogglePayAsYouGoRequest request)
        {
            try
            {
                // Get user ID from authentication token
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { IsError = true, Message = "User not authenticated" });

                // Update pay-as-you-go setting in database
                await UpdatePayAsYouGoSettingAsync(userId, request.Enabled);
                
                return Ok(new { 
                    IsError = false,
                    Success = true, 
                    PayAsYouGoEnabled = request.Enabled,
                    Message = request.Enabled 
                        ? "Pay-as-you-go billing enabled. You will be charged for requests over your plan limit."
                        : "Pay-as-you-go billing disabled. You will hit plan limits."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { IsError = true, Message = $"Failed to update pay-as-you-go settings: {ex.Message}" });
            }
        }

        private async Task UpdatePayAsYouGoSettingAsync(string userId, bool enabled)
        {
            // Update pay-as-you-go setting in database
            Console.WriteLine($"Updated pay-as-you-go setting for user {userId}: {enabled}");
        }

        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            try
            {
                var userId = GetCurrentUserId();
                var userUsage = await GetUserUsageAsync(userId);
                
                // Get current subscription details
                var subscription = await GetUserSubscriptionAsync(userId);
                var planLimits = GetPlanLimits(subscription?.PlanId ?? "free");
                
                var usage = new
                {
                    currentMonth = new
                    {
                        requests = userUsage.RequestsThisMonth,
                        limit = planLimits.RequestLimit,
                        remaining = Math.Max(0, planLimits.RequestLimit - userUsage.RequestsThisMonth),
                        overage = Math.Max(0, userUsage.RequestsThisMonth - planLimits.RequestLimit)
                    },
                    payAsYouGoEnabled = subscription?.PayAsYouGoEnabled ?? false,
                    overageCharges = new
                    {
                        currentMonth = userUsage.OverageCharges,
                        currency = "USD"
                    },
                    subscription = new
                    {
                        planId = subscription?.PlanId ?? "free",
                        status = subscription?.Status ?? "active",
                        currentPeriodStart = subscription?.CurrentPeriodStart,
                        currentPeriodEnd = subscription?.CurrentPeriodEnd
                    }
                };

                return Ok(usage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve usage data", details = ex.Message });
            }
        }

        private string GetCurrentUserId()
        {
            // This would typically extract user ID from JWT token or session
            // For now, return a mock user ID
            return "user_test_123";
        }

        private async Task<dynamic> GetUserUsageAsync(string userId)
        {
            // TODO: Implement real usage tracking from database
            // For now, return mock data
            return new
            {
                RequestsThisMonth = 45000,
                OverageCharges = 0.00m,
                LastUpdated = DateTime.UtcNow
            };
        }

        private async Task<dynamic> GetUserSubscriptionAsync(string userId)
        {
            // TODO: Implement real subscription lookup from database
            // For now, return mock data
            return new
            {
                PlanId = "pro",
                Status = "active",
                PayAsYouGoEnabled = false,
                CurrentPeriodStart = DateTime.UtcNow.AddDays(-15),
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(15)
            };
        }

    

    // HyperDrive Integration Methods

    /// <summary>
    /// Updates HyperDrive configuration based on subscription plan
    /// </summary>
    [HttpPost("update-hyperdrive-config")]
    public async Task<ActionResult<OASISResult<bool>>> UpdateHyperDriveConfig([FromBody] UpdateHyperDriveConfigRequest request)
    {
        try
        {
            var dna = OASISDNAManager.OASISDNA.OASIS;
            if (dna != null)
            {
                // Update subscription configuration
                dna.SubscriptionConfig.PlanType = request.PlanType;
                dna.SubscriptionConfig.PayAsYouGoEnabled = request.PayAsYouGoEnabled;
                
                // Update HyperDrive limits based on plan
                var limits = GetPlanLimits(request.PlanType);
                dna.SubscriptionConfig.MaxReplicationsPerMonth = limits.MaxReplications;
                dna.SubscriptionConfig.MaxFailoversPerMonth = limits.MaxFailovers;
                dna.SubscriptionConfig.MaxStorageGB = limits.MaxStorageGB;
                
                // Update cost settings
                dna.SubscriptionConfig.CostPerReplication = limits.CostPerReplication;
                dna.SubscriptionConfig.CostPerFailover = limits.CostPerFailover;
                dna.SubscriptionConfig.CostPerGB = limits.CostPerGB;
                
                // Update replication rules for free providers only on free plan
                if (request.PlanType == "Free")
                {
                    dna.ReplicationRules.FreeProvidersOnly = true;
                    dna.ReplicationRules.CostThreshold = 0;
                }
                else
                {
                    dna.ReplicationRules.FreeProvidersOnly = false;
                    dna.ReplicationRules.CostThreshold = limits.CostThreshold;
                }
                
                // Update failover rules
                if (request.PlanType == "Free")
                {
                    dna.FailoverRules.FreeProvidersOnly = true;
                    dna.FailoverRules.CostThreshold = 0;
                }
                else
                {
                    dna.FailoverRules.FreeProvidersOnly = false;
                    dna.FailoverRules.CostThreshold = limits.CostThreshold;
                }
                
                await OASISDNAManager.SaveDNAAsync();
            }
            
            return Ok(new OASISResult<bool>
            {
                Result = true,
                Message = "HyperDrive configuration updated successfully for subscription plan."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new OASISResult<bool>
            {
                IsError = true,
                Message = $"Failed to update HyperDrive configuration: {ex.Message}",
                Exception = ex
            });
        }
    }

    /// <summary>
    /// Gets HyperDrive usage statistics for current subscription
    /// </summary>
    [HttpGet("hyperdrive-usage")]
    public ActionResult<OASISResult<HyperDriveUsageDto>> GetHyperDriveUsage()
    {
        try
        {
            var dna = OASISDNAManager.OASISDNA.OASIS;
            var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();
            
            // This would typically come from a usage tracking service
            var usage = new HyperDriveUsageDto
            {
                PlanType = config.PlanType,
                PayAsYouGoEnabled = config.PayAsYouGoEnabled,
                CurrentUsage = new Dictionary<string, int>
                {
                    { "Replications", 45 },
                    { "Failovers", 3 },
                    { "Storage", 2 },
                    { "Requests", 1250 }
                },
                Limits = new Dictionary<string, int>
                {
                    { "Replications", config.MaxReplicationsPerMonth },
                    { "Failovers", config.MaxFailoversPerMonth },
                    { "Storage", config.MaxStorageGB },
                    { "Requests", GetRequestLimit(config.PlanType) }
                },
                Costs = new Dictionary<string, decimal>
                {
                    { "PerReplication", config.CostPerReplication },
                    { "PerFailover", config.CostPerFailover },
                    { "PerGB", config.CostPerGB }
                }
            };
            
            return Ok(new OASISResult<HyperDriveUsageDto>
            {
                Result = usage,
                Message = "HyperDrive usage retrieved successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new OASISResult<HyperDriveUsageDto>
            {
                IsError = true,
                Message = $"Failed to get HyperDrive usage: {ex.Message}",
                Exception = ex
            });
        }
    }

    /// <summary>
    /// Checks if user can perform HyperDrive operation based on quota
    /// </summary>
    [HttpPost("check-hyperdrive-quota")]
    public ActionResult<OASISResult<QuotaCheckResult>> CheckHyperDriveQuota([FromBody] QuotaCheckRequest request)
    {
        try
        {
            var dna = OASISDNAManager.OASISDNA.OASIS;
            var config = dna?.SubscriptionConfig ?? new SubscriptionConfig();
            
            var limits = GetPlanLimits(config.PlanType);
            var currentUsage = GetCurrentUsage(request.OperationType);
            var limit = GetLimitForOperation(request.OperationType, limits);
            
            var result = new QuotaCheckResult
            {
                CanProceed = currentUsage < limit,
                CurrentUsage = currentUsage,
                Limit = limit,
                Remaining = Math.Max(0, limit - currentUsage),
                WouldExceedQuota = currentUsage >= limit,
                RequiresPayAsYouGo = currentUsage >= limit && config.PayAsYouGoEnabled,
                EstimatedCost = currentUsage >= limit ? GetEstimatedCost(request.OperationType, config) : 0
            };
            
            return Ok(new OASISResult<QuotaCheckResult>
            {
                Result = result,
                Message = "Quota check completed successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new OASISResult<QuotaCheckResult>
            {
                IsError = true,
                Message = $"Failed to check quota: {ex.Message}",
                Exception = ex
            });
        }
    }

    private PlanLimits GetPlanLimits(string planType)
    {
        return planType switch
        {
            "Free" => new PlanLimits
            {
                MaxReplications = 100,
                MaxFailovers = 10,
                MaxStorageGB = 1,
                CostThreshold = 0,
                CostPerReplication = 0,
                CostPerFailover = 0,
                CostPerGB = 0
            },
            "Basic" => new PlanLimits
            {
                MaxReplications = 1000,
                MaxFailovers = 100,
                MaxStorageGB = 10,
                CostThreshold = 10,
                CostPerReplication = 0.01m,
                CostPerFailover = 0.05m,
                CostPerGB = 0.10m
            },
            "Pro" => new PlanLimits
            {
                MaxReplications = 10000,
                MaxFailovers = 1000,
                MaxStorageGB = 100,
                CostThreshold = 50,
                CostPerReplication = 0.005m,
                CostPerFailover = 0.025m,
                CostPerGB = 0.05m
            },
            "Enterprise" => new PlanLimits
            {
                MaxReplications = int.MaxValue,
                MaxFailovers = int.MaxValue,
                MaxStorageGB = int.MaxValue,
                CostThreshold = 100,
                CostPerReplication = 0.001m,
                CostPerFailover = 0.01m,
                CostPerGB = 0.01m
            },
            _ => new PlanLimits
            {
                MaxReplications = 100,
                MaxFailovers = 10,
                MaxStorageGB = 1,
                CostThreshold = 0,
                CostPerReplication = 0,
                CostPerFailover = 0,
                CostPerGB = 0
            }
        };
    }

        private int GetRequestLimit(string planType)
        {
            return planType switch
        {
            "Free" => 1000,
            "Basic" => 10000,
            "Pro" => 100000,
            "Enterprise" => int.MaxValue,
            _ => 1000
        };
        }

        private int GetCurrentUsage(string operationType)
        {
            // This would typically come from a usage tracking service
            return operationType switch
        {
            "Replications" => 45,
            "Failovers" => 3,
            "Storage" => 2,
            "Requests" => 1250,
            _ => 0
        };
        }

        private int GetLimitForOperation(string operationType, PlanLimits limits)
        {
            return operationType switch
        {
            "Replications" => limits.MaxReplications,
            "Failovers" => limits.MaxFailovers,
            "Storage" => limits.MaxStorageGB,
            "Requests" => GetRequestLimit(limits.PlanType),
            _ => 0
        };
        }

        private decimal GetEstimatedCost(string operationType, SubscriptionConfig config)
        {
            return operationType switch
        {
            "Replications" => config.CostPerReplication,
            "Failovers" => config.CostPerFailover,
            "Storage" => config.CostPerGB,
            _ => 0
        };
        }

        // Supporting classes for HyperDrive integration
        public class UpdateHyperDriveConfigRequest
        {
        public string PlanType { get; set; }
        public bool PayAsYouGoEnabled { get; set; }
        }

        public class HyperDriveUsageDto
        {
        public string PlanType { get; set; }
        public bool PayAsYouGoEnabled { get; set; }
        public Dictionary<string, int> CurrentUsage { get; set; }
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

        public class PlanLimits
        {
        public int MaxReplications { get; set; }
        public int MaxFailovers { get; set; }
        public int MaxStorageGB { get; set; }
        public decimal CostThreshold { get; set; }
        public decimal CostPerReplication { get; set; }
        public decimal CostPerFailover { get; set; }
        public decimal CostPerGB { get; set; }
        public string PlanType { get; set; }
        public int RequestLimit { get; set; }
        }


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

        /// <summary>Request to create a one-time checkout session to buy credits.</summary>
        public class BuyCreditsRequest
        {
            [Required]
            public decimal AmountUsd { get; set; }
            [Required]
            public string AvatarId { get; set; }
            public string SuccessUrl { get; set; }
            public string CancelUrl { get; set; }
            public string CustomerEmail { get; set; }
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
    }
}

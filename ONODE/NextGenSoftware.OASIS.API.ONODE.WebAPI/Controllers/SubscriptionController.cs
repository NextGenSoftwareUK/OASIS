using System;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
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
using Newtonsoft.Json;
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
        private readonly OASISSub.ISubscriptionUsageRepository _usageRepository;
        private readonly OASISSub.ISubscriptionBillingRepository _billingRepository;

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

        public SubscriptionController(IConfiguration configuration, OASISSub.ISubscriptionService subscriptionService, OASISSub.ISubscriptionUsageRepository usageRepository = null, OASISSub.ISubscriptionBillingRepository billingRepository = null)
        {
            _configuration = configuration;
            _subscriptionService = subscriptionService;
            _usageRepository = usageRepository;
            _billingRepository = billingRepository;
        }

        // ── Plans ────────────────────────────────────────────────────────────

        [HttpGet("plans")]
        public ActionResult GetPlans()
        {
            return Ok(new { Result = Plans, IsError = false, Message = "Plans loaded successfully" });
        }

        /// <summary>The retired counter route never authorizes or records usage.</summary>
        [HttpPost("authorize-request")]
        public ActionResult AuthorizeRequest([FromBody] AuthorizeSubscriptionRequest request) =>
            StatusCode(410, new { IsError = true, Code = "USAGE_PROTOCOL_REQUIRED", Message = "Use usage/authorize, usage/start and usage/settle with the shared WEB4 usage SDK." });

        /// <summary>Reserve against the authenticated avatar and independently authenticated consuming service.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("usage/authorize")]
        public async Task<ActionResult> AuthorizeUsage([FromBody] UsageAuthorizationRequest request)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { IsError = true, Message = "User not authenticated." });
            try
            {
                if (request == null) return BadRequest(new { IsError = true, Code = "INVALID_USAGE_AUTHORIZATION" });
                OASISSub.SubscriptionServiceIdentity.RequireService(HttpContext, _configuration, request.ConsumingService);
                var decision = await _subscriptionService.AuthorizeUsageAsync(userId, await GetCurrentKarmaAsync(userId), request, HttpContext.RequestAborted);
                return StatusCode(decision.StatusCode, decision);
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { IsError = true, Code = "SERVICE_IDENTITY_REQUIRED", ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { IsError = true, Code = "INVALID_USAGE_AUTHORIZATION", ex.Message }); }
        }

        /// <summary>Marks the durable execution boundary; a provider must never execute without this acknowledgement.</summary>
        [HttpPost("usage/start")]
        public async Task<ActionResult> StartUsage([FromBody] UsageStartRequest request)
        {
            try
            {
                if (request == null) return BadRequest(new { IsError = true, Code = "INVALID_USAGE_START" });
                OASISSub.SubscriptionServiceIdentity.RequireService(HttpContext, _configuration, request.ConsumingService);
                return Ok(await _usageRepository.StartAsync(request, HttpContext.RequestAborted));
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { IsError = true, Code = "SERVICE_IDENTITY_REQUIRED", ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { IsError = true, Code = "INVALID_USAGE_START", ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { IsError = true, Code = "USAGE_OPERATION_NOT_FOUND", ex.Message }); }
            catch (OASISSub.SubscriptionUsageConflictException ex) { return Conflict(new { IsError = true, Code = "OPERATION_ID_CONFLICT", ex.Message }); }
        }

        /// <summary>Service credentials authorize durable outbox delivery after the original avatar JWT expires.</summary>
        [HttpPost("usage/settle")]
        public async Task<ActionResult> SettleUsage([FromBody] UsageSettlementRequest request)
        {
            try
            {
                if (request == null) return BadRequest(new { IsError = true, Code = "INVALID_USAGE_SETTLEMENT" });
                OASISSub.SubscriptionServiceIdentity.RequireService(HttpContext, _configuration, request.ConsumingService);
                return Ok(await _subscriptionService.SettleUsageAsync(request.UserId, request, HttpContext.RequestAborted));
            }
            catch (UnauthorizedAccessException ex) { return Unauthorized(new { IsError = true, Code = "SERVICE_IDENTITY_REQUIRED", ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { IsError = true, Code = "INVALID_USAGE_SETTLEMENT", ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { IsError = true, Code = "USAGE_OPERATION_NOT_FOUND", ex.Message }); }
            catch (OASISSub.SubscriptionUsageConflictException ex) { return Conflict(new { IsError = true, Code = "OPERATION_ID_CONFLICT", ex.Message }); }
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("usage/admin/corrections")]
        public async Task<ActionResult> CorrectUsage([FromBody] OASISSub.UsageCorrectionRequest request)
        {
            try
            {
                string actor = OASISSub.SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
                return Ok(await _usageRepository.CorrectAsync(actor, request, HttpContext.RequestAborted));
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { IsError = true, Code = "ADMINISTRATOR_REQUIRED", ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { IsError = true, Code = "INVALID_CORRECTION", ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new { IsError = true, Code = "USAGE_OPERATION_NOT_FOUND", ex.Message }); }
            catch (OASISSub.SubscriptionUsageConflictException ex) { return Conflict(new { IsError = true, Code = "CORRECTION_CONFLICT", ex.Message }); }
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("usage/admin/migration/inventory")]
        public async Task<ActionResult> GetUsageMigrationInventory()
        {
            try
            {
                OASISSub.SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
                return Ok(await _usageRepository.GetMigrationInventoryAsync(HttpContext.RequestAborted));
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { IsError = true, Code = "ADMINISTRATOR_REQUIRED", ex.Message }); }
        }

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpPost("usage/admin/migration/opening-balance")]
        public async Task<ActionResult> ImportUsageOpeningBalance([FromBody] OASISSub.UsageOpeningBalanceManifest manifest)
        {
            try
            {
                string actor = OASISSub.SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration);
                OASISSub.UsageMigrationSignature.RequireApproval(manifest, actor, _configuration);
                return Ok(new { MigrationId = await _usageRepository.ImportOpeningBalanceAsync(actor, manifest, HttpContext.RequestAborted) });
            }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new { IsError = true, Code = "MIGRATION_APPROVAL_REQUIRED", ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new { IsError = true, Code = "INVALID_OPENING_BALANCE", ex.Message }); }
            catch (OASISSub.SubscriptionUsageConflictException ex) { return Conflict(new { IsError = true, Code = "MIGRATION_CONFLICT", ex.Message }); }
        }
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("usage/audit")]
        public async Task<ActionResult> GetUsageAudit([FromQuery] string beforeId = null, [FromQuery] int limit = 100)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { IsError = true, Message = "User not authenticated." });
            try { return Ok(await _usageRepository.GetAuditAsync(userId, beforeId, limit, HttpContext.RequestAborted)); }
            catch (ArgumentException ex) { return BadRequest(new { IsError = true, Code = "INVALID_AUDIT_CURSOR", ex.Message }); }
        }
        /// <summary>Returns authoritative subscription usage; plan and karma are never accepted from the caller.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("usage/current")]
        public async Task<ActionResult> GetAuthoritativeUsage()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { IsError = true, Message = "User not authenticated." });
            return Ok(await _subscriptionService.GetUsageSummaryAsync(userId, await GetCurrentKarmaAsync(userId), HttpContext.RequestAborted));
        }

        /// <summary>Returns the authenticated avatar's operation projections; usage/audit returns immutable history.</summary>
        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("usage/events")]
        public async Task<ActionResult> GetUsageEvents([FromQuery] int limit = 100)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized(new { IsError = true, Message = "User not authenticated." });
            return Ok(await _subscriptionService.GetUsageEventsAsync(userId, limit, HttpContext.RequestAborted));
        }

        // ── Checkout ─────────────────────────────────────────────────────────

        [Microsoft.AspNetCore.Authorization.Authorize]
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

            // Free/trial plan: provision immediately without Stripe
            if (plan.PriceMonthly == 0m)
            {
                var userId = GetCurrentUserId();
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { IsError = true, Message = "User not authenticated." });

                await _subscriptionService.UpsertSubscriptionAsync(new OASISSub.SubscriptionRecord
                {
                    UserId = userId,
                    PlanId = plan.Id,
                    Status = "active",
                    CurrentPeriodStart = DateTime.UtcNow,
                    CurrentPeriodEnd = DateTime.UtcNow.AddYears(10)
                });
                var successUrl = request.SuccessUrl ?? "/";
                return Ok(new { IsError = false, Message = plan.Name + " plan activated.", SessionUrl = successUrl });
            }

            try
            {
                var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                    ?? _configuration["STRIPE_SECRET_KEY"]
                    ?? OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.SubscriptionConfig?.Stripe?.SecretKey;
                if (string.IsNullOrWhiteSpace(secretKey))
                    return StatusCode(500, new { IsError = true, Message = "Stripe not configured. Set STRIPE_SECRET_KEY environment variable." });

                StripeConfiguration.ApiKey = secretKey;

                var avatarId = GetCurrentUserId();
                if (!Guid.TryParseExact(avatarId, "D", out _)) return Unauthorized(new { IsError = true, Message = "An authenticated avatar is required for checkout." });
                var priceId = GetStripePriceId(plan.Id);
                if (string.IsNullOrWhiteSpace(priceId))
                    return StatusCode(500, new { IsError = true, Message = $"No Stripe Price ID configured for plan '{plan.Id}'. Set STRIPE_PRICE_{plan.Id.ToUpper()} environment variable." });

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
                    SubscriptionData = new Stripe.Checkout.SessionSubscriptionDataOptions { Metadata = new Dictionary<string, string> { { "avatar_id", avatarId }, { "plan_id", plan.Id } } },
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
            string body;
            using (var reader = new System.IO.StreamReader(Request.Body))
                body = await reader.ReadToEndAsync();

            var webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
                ?? _configuration["STRIPE_WEBHOOK_SECRET"]
                ?? OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.SubscriptionConfig?.Stripe?.WebhookSecret;
            if (string.IsNullOrWhiteSpace(webhookSecret))
                return BadRequest("Webhook secret not configured.");

            var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
                return BadRequest("Missing Stripe-Signature header.");

            Event stripeEvent;
            try { stripeEvent = EventUtility.ConstructEvent(body, signature, webhookSecret, throwOnApiVersionMismatch: false); }
            catch (Exception ex) when (ex is StripeException || ex is JsonException || ex is ArgumentException)
            { return BadRequest(new { IsError = true, Code = "INVALID_STRIPE_WEBHOOK", ex.Message }); }
            try
            {
                var diag = await HandleStripeEventAsync(stripeEvent, body);
                return Ok(new { processed = true, diag });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        private async Task<string> HandleStripeEventAsync(Event stripeEvent, string rawBody)
        {
            if (stripeEvent == null || string.IsNullOrWhiteSpace(stripeEvent.Type) || string.IsNullOrWhiteSpace(stripeEvent.Id))
                throw new InvalidOperationException("A signed Stripe event with a type and immutable event id is required.");
            string subscriptionId;
            string userId;
            Invoice invoice = null;
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    var checkout = RequireStripeObject<Stripe.Checkout.Session>(stripeEvent);
                    subscriptionId = checkout.SubscriptionId;
                    userId = checkout.Metadata?.GetValueOrDefault("avatar_id");
                    break;
                case "customer.subscription.created":
                case "customer.subscription.updated":
                case "customer.subscription.deleted":
                    var subscription = RequireStripeObject<Stripe.Subscription>(stripeEvent);
                    subscriptionId = subscription.Id;
                    userId = subscription.Metadata?.GetValueOrDefault("avatar_id");
                    if (string.IsNullOrEmpty(userId))
                        userId = (await _subscriptionService.GetSubscriptionByStripeSubscriptionIdAsync(subscription.Id))?.UserId;
                    break;
                case "invoice.paid":
                case "invoice.payment_succeeded":
                case "invoice.payment_failed":
                    invoice = RequireStripeObject<Invoice>(stripeEvent);
                    if (string.IsNullOrWhiteSpace(invoice.SubscriptionId)) return "non_subscription_invoice";
                    subscriptionId = invoice.SubscriptionId;
                    userId = (await _subscriptionService.GetSubscriptionByStripeSubscriptionIdAsync(subscriptionId))?.UserId;
                    if (string.IsNullOrEmpty(userId))
                    {
                        var invoiceSubscription = await new Stripe.SubscriptionService(StripeClient()).GetAsync(subscriptionId, cancellationToken: HttpContext.RequestAborted);
                        userId = invoiceSubscription.Metadata?.GetValueOrDefault("avatar_id");
                    }
                    break;
                default: return "unhandled_" + stripeEvent.Type;
            }
            if (!Guid.TryParseExact(userId, "D", out _) || string.IsNullOrWhiteSpace(subscriptionId))
                throw new InvalidOperationException("Stripe subscription ownership has not been established; retry after its verified subscription metadata or legacy snapshot is available.");
            OASISSub.OrderRecord order = null;
            if (stripeEvent.Type is "invoice.paid" or "invoice.payment_succeeded")
            {
                if (invoice.Status != "paid" || !invoice.Paid || invoice.Currency != "usd")
                    throw new InvalidOperationException("This billing catalogue requires a paid USD invoice with an actual Stripe amount.");
                var invoicedPlans = Plans.Where(plan => invoice.Lines?.Data?.Any(line => line.Price?.Id == GetStripePriceId(plan.Id) && !string.IsNullOrEmpty(line.Price?.Id)) == true).ToList();
                order = new OASISSub.OrderRecord { UserId = userId, PlanId = invoicedPlans.Count == 1 ? invoicedPlans[0].Id : null, StripeInvoiceId = invoice.Id, Amount = invoice.AmountPaid / 100m,
                    Currency = "USD", Status = "paid", Description = "Stripe subscription invoice", CreatedAt = invoice.Created };
            }
            string fingerprint = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawBody)));
            bool applied = await _billingRepository.ApplyStripeEventAsync(stripeEvent.Id, fingerprint, userId, stripeEvent.Created, async ct =>
            {
                var canonical = await new Stripe.SubscriptionService(StripeClient()).GetAsync(subscriptionId, cancellationToken: ct);
                var matchingPlans = Plans.Where(plan => canonical.Items?.Data?.Any(item => item.Price?.Id == GetStripePriceId(plan.Id) && !string.IsNullOrEmpty(item.Price?.Id)) == true).ToList();
                if (matchingPlans.Count != 1) throw new InvalidOperationException("Canonical Stripe subscription must match exactly one configured OASIS plan price.");
                var canonicalOwner = canonical.Metadata?.GetValueOrDefault("avatar_id");
                if (!string.IsNullOrEmpty(canonicalOwner) && canonicalOwner != userId)
                    throw new InvalidOperationException("Canonical Stripe metadata does not match the established avatar owner.");

                return new OASISSub.SubscriptionRecord { UserId = userId, PlanId = matchingPlans[0].Id, Status = canonical.Status,
                    StripeCustomerId = canonical.CustomerId, StripeSubscriptionId = canonical.Id, StripeSubscriptionCreatedAtUtc = canonical.Created,
                    CurrentPeriodStart = canonical.CurrentPeriodStart, CurrentPeriodEnd = canonical.CurrentPeriodEnd, CreatedAt = canonical.Created };
            }, order, HttpContext.RequestAborted);
            return applied ? "applied" : "already_applied";
        }

        private static T RequireStripeObject<T>(Event stripeEvent) where T : class =>
            stripeEvent.Data?.Object as T ?? throw new InvalidOperationException("Signed Stripe event contains an unexpected or incomplete object schema.");

        private IStripeClient StripeClient()
        {
            string secret = _configuration["STRIPE_SECRET_KEY"] ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
                ?? OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.SubscriptionConfig?.Stripe?.SecretKey;
            if (string.IsNullOrWhiteSpace(secret)) throw new InvalidOperationException("STRIPE_SECRET_KEY is required to read canonical subscription state.");
            return new StripeClient(secret);
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
            if (User?.Identity?.IsAuthenticated != true) return null;
            string claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParseExact(claim, "D", out var authenticatedId)) return null;
            if (HttpContext.Items.TryGetValue("Avatar", out var value) && value is IAvatar avatar && avatar.Id != authenticatedId) return null;
            return authenticatedId.ToString("D");
        }
        private static async Task<int> GetCurrentKarmaAsync(string userId)
        {
            if (!Guid.TryParse(userId, out var avatarId)) return 0;
            var result = await Program.AvatarManager.LoadAvatarDetailAsync(avatarId);
            if (result.IsError || result.Result == null)
                throw new InvalidOperationException($"Unable to load authoritative karma for subscription usage: {result.Message}");
            return result.Result.Karma > int.MaxValue ? int.MaxValue : (int)Math.Max(0, result.Result.Karma);
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

        /// <summary>
        /// Looks up the Stripe Price ID for a plan from env vars or OASISDNA config.
        /// Env vars take priority: STRIPE_PRICE_BRONZE, STRIPE_PRICE_SILVER, STRIPE_PRICE_GOLD, STRIPE_PRICE_ENTERPRISE.
        /// Set these in Railway (production) or OASISDNA.json Stripe section (local dev).
        /// Get Price IDs from: Stripe Dashboard → Products → your plan → Pricing → copy price_xxx.
        /// </summary>
        private string GetStripePriceId(string planId)
        {
            var stripe = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.SubscriptionConfig?.Stripe;
            return planId.ToLower() switch
            {
                "bronze"     => Environment.GetEnvironmentVariable("STRIPE_PRICE_BRONZE")     ?? _configuration["STRIPE_PRICE_BRONZE"]     ?? stripe?.PriceBronze,
                "silver"     => Environment.GetEnvironmentVariable("STRIPE_PRICE_SILVER")     ?? _configuration["STRIPE_PRICE_SILVER"]     ?? stripe?.PriceSilver,
                "gold"       => Environment.GetEnvironmentVariable("STRIPE_PRICE_GOLD")       ?? _configuration["STRIPE_PRICE_GOLD"]       ?? stripe?.PriceGold,
                "enterprise" => Environment.GetEnvironmentVariable("STRIPE_PRICE_ENTERPRISE") ?? _configuration["STRIPE_PRICE_ENTERPRISE"] ?? stripe?.PriceEnterprise,
                _            => null
            };
        }

        // ── OLD: auto-create Stripe Products/Prices on first checkout ──────────
        // Replaced by GetStripePriceId() above. Kept for reference — the new approach
        // requires Price IDs to be set explicitly in config, giving full control over
        // the Stripe product catalogue (no accidental duplicates, matches NFT Founders pattern).
        //
        // private async Task<string> GetOrCreateStripePriceAsync(PlanDto plan)
        // {
        //     var priceService = new PriceService();
        //     var prices = await priceService.ListAsync(new PriceListOptions { Active = true, Limit = 100 });
        //     var existing = prices.Data.FirstOrDefault(p =>
        //         p.UnitAmount == (long)(plan.PriceMonthly * 100) &&
        //         p.Currency == plan.Currency.ToLower() &&
        //         p.Recurring?.Interval == "month" &&
        //         p.Metadata.ContainsKey("plan_id") && p.Metadata["plan_id"] == plan.Id);
        //     if (existing != null) return existing.Id;
        //
        //     var productService = new ProductService();
        //     var product = await productService.CreateAsync(new ProductCreateOptions
        //     {
        //         Name = $"OASIS {plan.Name} Plan",
        //         Description = string.Join(", ", plan.Features),
        //         Metadata = new Dictionary<string, string> { { "plan_id", plan.Id } }
        //     });
        //
        //     var price = await priceService.CreateAsync(new PriceCreateOptions
        //     {
        //         Product = product.Id,
        //         UnitAmount = (long)(plan.PriceMonthly * 100),
        //         Currency = plan.Currency.ToLower(),
        //         Recurring = new PriceRecurringOptions { Interval = "month" },
        //         Metadata = new Dictionary<string, string>
        //         {
        //             { "plan_id", plan.Id },
        //             { "max_requests", plan.MaxRequestsPerMonth.ToString() },
        //             { "max_storage_gb", plan.MaxStorageGB.ToString() }
        //         }
        //     });
        //
        //     return price.Id;
        // }

        // ── DTOs / inner types ───────────────────────────────────────────────

        public class PlanDto
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public decimal PriceMonthly { get; set; }
            public decimal Price => PriceMonthly;   // portal reads p.Price
            public decimal Amount => PriceMonthly;  // portal reads p.amount
            public string Interval { get; set; } = "month";
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

    public sealed class AuthorizeSubscriptionRequest
    {
        public string ConsumingService { get; set; }
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SubscriptionController(IConfiguration configuration)
        {
            _configuration = configuration;
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
                    { "user_id", request.UserId ?? "anonymous" }
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
            var priceService = new Stripe.PriceService();
            var existingPrices = await priceService.ListAsync(new Stripe.PriceListOptions
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
            var productService = new Stripe.ProductService();
            var product = await productService.CreateAsync(new Stripe.ProductCreateOptions
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

            var price = await priceService.CreateAsync(new Stripe.PriceCreateOptions
            {
                Product = product.Id,
                UnitAmount = (long)(plan.PriceMonthly * 100),
                Currency = plan.Currency.ToLower(),
                Recurring = new Stripe.PriceRecurringOptions
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
                var stripeEvent = Stripe.EventUtility.ConstructEvent(body, signatureHeader, webhookSecret);

                // Handle the event
                await HandleStripeEventAsync(stripeEvent);

                return Ok();
            }
            catch (Stripe.StripeException ex)
            {
                return BadRequest($"Stripe error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        private async Task HandleStripeEventAsync(Stripe.Event stripeEvent)
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent.Data.Object as Stripe.Checkout.Session);
                    break;

                case "customer.subscription.created":
                    await HandleSubscriptionCreatedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                    break;

                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                    break;

                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent.Data.Object as Stripe.Subscription);
                    break;

                case "invoice.payment_succeeded":
                    await HandlePaymentSucceededAsync(stripeEvent.Data.Object as Stripe.Invoice);
                    break;

                case "invoice.payment_failed":
                    await HandlePaymentFailedAsync(stripeEvent.Data.Object as Stripe.Invoice);
                    break;

                default:
                    // Log unhandled event types
                    Console.WriteLine($"Unhandled event type: {stripeEvent.Type}");
                    break;
            }
        }

        private async Task HandleCheckoutSessionCompletedAsync(Stripe.Checkout.Session session)
        {
            // Create or update user subscription
            var userId = session.Metadata.GetValueOrDefault("user_id");
            var planId = session.Metadata.GetValueOrDefault("plan_id");
            
            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(planId))
            {
                // Update user subscription in database
                await UpdateUserSubscriptionAsync(userId, planId, session.CustomerId, session.SubscriptionId);
            }
        }

        private async Task HandleSubscriptionCreatedAsync(Stripe.Subscription subscription)
        {
            // Handle new subscription creation
            await UpdateSubscriptionStatusAsync(subscription.Id, "active", subscription.CustomerId);
        }

        private async Task HandleSubscriptionUpdatedAsync(Stripe.Subscription subscription)
        {
            // Handle subscription updates
            var status = subscription.Status;
            await UpdateSubscriptionStatusAsync(subscription.Id, status, subscription.CustomerId);
        }

        private async Task HandleSubscriptionDeletedAsync(Stripe.Subscription subscription)
        {
            // Handle subscription cancellation
            await UpdateSubscriptionStatusAsync(subscription.Id, "cancelled", subscription.CustomerId);
        }

        private async Task HandlePaymentSucceededAsync(Stripe.Invoice invoice)
        {
            // Handle successful payment
            if (invoice.SubscriptionId != null)
            {
                await UpdateSubscriptionStatusAsync(invoice.SubscriptionId, "active", invoice.CustomerId);
            }
        }

        private async Task HandlePaymentFailedAsync(Stripe.Invoice invoice)
        {
            // Handle failed payment
            if (invoice.SubscriptionId != null)
            {
                await UpdateSubscriptionStatusAsync(invoice.SubscriptionId, "past_due", invoice.CustomerId);
            }
        }

        private async Task UpdateUserSubscriptionAsync(string userId, string planId, string customerId, string subscriptionId)
        {
            // Update user subscription in database
            // This would typically involve updating a database record
            Console.WriteLine($"Updated subscription for user {userId}: plan={planId}, customer={customerId}, subscription={subscriptionId}");
        }

        private async Task UpdateSubscriptionStatusAsync(string subscriptionId, string status, string customerId)
        {
            // Update subscription status in database
            Console.WriteLine($"Updated subscription {subscriptionId} status to {status} for customer {customerId}");
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
                // TODO: Implement usage retrieval
                var usage = new
                {
                    currentMonth = new
                    {
                        requests = 45000,
                        limit = 100000,
                        remaining = 55000,
                        overage = 0
                    },
                    payAsYouGoEnabled = false,
                    overageCharges = new
                    {
                        currentMonth = 0.00m,
                        currency = "USD"
                    }
                };

                return Ok(usage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve usage data" });
            }
        }

        private string GetCurrentUserId()
        {
            // This would typically extract user ID from JWT token or session
            // For now, return a mock user ID
            return "user_test_123";
        }

        private async Task<object> GetUserUsageAsync(string userId)
        {
            // This would typically query the database for actual usage data
            // For now, return mock data
            return new
            {
                currentMonth = new
                {
                    requests = 45000,
                    limit = 100000,
                    remaining = 55000,
                    overage = 0
                },
                payAsYouGoEnabled = false,
                overageCharges = new
                {
                    currentMonth = 0.00m,
                    currency = "USD"
                },
                lastUpdated = DateTime.UtcNow,
                periodStart = DateTime.UtcNow.AddDays(-15),
                periodEnd = DateTime.UtcNow.AddDays(15)
            };
        }
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



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
            // TODO: Load from config or persistent store. Placeholder values for now.
            var plans = new List<PlanDto>
            {
                new PlanDto { Id = "bronze", Name = "Bronze", PriceMonthly = 9.0m, Currency = "USD", Features = new []{"Starter API limits", "Community support"} },
                new PlanDto { Id = "silver", Name = "Silver", PriceMonthly = 29.0m, Currency = "USD", Features = new []{"Higher API limits", "Email support", "Basic analytics"} },
                new PlanDto { Id = "gold", Name = "Gold", PriceMonthly = 99.0m, Currency = "USD", Features = new []{"Premium API limits", "Priority support", "Advanced analytics"} },
                new PlanDto { Id = "enterprise", Name = "Enterprise", PriceMonthly = 0.0m, Currency = "USD", Features = new []{"Custom limits", "SLA & SSO", "Dedicated support"}, IsContactSales = true }
            };

            return Ok(new { Result = plans, IsError = false, Message = "Plans loaded" });
        }

        [HttpPost("checkout/session")]
        public ActionResult<CreateCheckoutSessionResponse> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { IsError = true, Message = "Invalid request" });

            // Placeholders for Stripe integration. We only validate envs exist and return a mock URL for now.
            var publishableKey = _configuration["STRIPE_PUBLISHABLE_KEY"];
            var secretKey = _configuration["STRIPE_SECRET_KEY"];
            var webhookSecret = _configuration["STRIPE_WEBHOOK_SECRET"];

            if (string.IsNullOrWhiteSpace(publishableKey) || string.IsNullOrWhiteSpace(secretKey))
                return StatusCode(500, new { IsError = true, Message = "Stripe keys not configured. Set STRIPE_PUBLISHABLE_KEY and STRIPE_SECRET_KEY." });

            // In a full implementation, we'd create a Stripe Checkout Session here and return its URL/id.
            var mockSessionId = $"cs_test_{Guid.NewGuid():N}";
            var mockSessionUrl = request.SuccessUrl ?? "https://oasisweb4.com/checkout/success";

            return Ok(new CreateCheckoutSessionResponse
            {
                IsError = false,
                Message = "Checkout session created (placeholder)",
                SessionId = mockSessionId,
                SessionUrl = mockSessionUrl
            });
        }

        [HttpPost("webhooks/stripe")]
        public async Task<IActionResult> StripeWebhook()
        {
            // Placeholder endpoint to be filled with Stripe event verification using STRIPE_WEBHOOK_SECRET
            // For now, just consume the body and return 200.
            using var reader = new System.IO.StreamReader(Request.Body);
            var payload = await reader.ReadToEndAsync();

            // TODO: verify signature header and process events (checkout.session.completed, invoice.paid, customer.subscription.*)

            return Ok();
        }

        [HttpGet("subscriptions/me")]
        public ActionResult<object> GetMySubscriptions()
        {
            // Placeholder - would query by authenticated user id (from token).
            var subs = new []
            {
                new { Id = "sub_test_123", PlanId = "silver", Status = "active", RenewsOn = DateTime.UtcNow.AddMonths(1) }
            };
            return Ok(new { Result = subs, IsError = false, Message = "Subscriptions loaded" });
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
    }

    public class PlanDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public decimal PriceMonthly { get; set; }
        public string Currency { get; set; }
        public string[] Features { get; set; }
        public bool IsContactSales { get; set; }
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
}



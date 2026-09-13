using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription
{
    public class SubscriptionControllerTests
    {
        private readonly Mock<ISubscriptionService> _svc;
        private readonly IConfiguration _cfg;
        private readonly SubscriptionController _ctrl;

        public SubscriptionControllerTests()
        {
            _svc = new Mock<ISubscriptionService>();
            _cfg = new ConfigurationBuilder().Build();
            _ctrl = new SubscriptionController(_cfg, _svc.Object);
            SetUnauthenticated();
        }

        // ── GET /api/subscription/plans ──────────────────────────────────────

        [Fact]
        public void GetPlans_Returns200WithAllPlanIds()
        {
            var result = _ctrl.GetPlans() as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            // Serialize to JSON to inspect plan IDs
            var json = System.Text.Json.JsonSerializer.Serialize(result.Value);
            json.Should().Contain("free");
            json.Should().Contain("bronze");
            json.Should().Contain("silver");
            json.Should().Contain("gold");
            json.Should().Contain("enterprise");
        }

        [Fact]
        public void GetPlans_IsErrorFalse()
        {
            var result = _ctrl.GetPlans() as OkObjectResult;
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            json.Should().Contain("false");
        }

        // ── POST /api/subscription/checkout/session ──────────────────────────

        [Fact]
        public async Task CreateCheckoutSession_NullBody_Returns400()
        {
            var result = await _ctrl.CreateCheckoutSession(null!) as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateCheckoutSession_UnknownPlan_Returns400()
        {
            var result = await _ctrl.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest
            {
                PlanId = "diamond"
            }) as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task CreateCheckoutSession_EnterprisePlan_Returns400WithSalesMessage()
        {
            var result = await _ctrl.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest
            {
                PlanId = "enterprise"
            }) as BadRequestObjectResult;

            result.Should().NotBeNull();
            System.Text.Json.JsonSerializer.Serialize(result!.Value).Should().Contain("sales");
        }

        [Fact]
        public async Task CreateCheckoutSession_FreePlan_Unauthenticated_Returns401()
        {
            var result = await _ctrl.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest
            {
                PlanId = "free",
                SuccessUrl = "/success"
            });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task CreateCheckoutSession_FreePlan_AuthenticatedUser_UpsertsCalled()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);

            _svc.Setup(s => s.UpsertSubscriptionAsync(It.IsAny<SubscriptionRecord>()))
                .Returns(Task.CompletedTask);

            var result = await _ctrl.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest
            {
                PlanId = "free",
                SuccessUrl = "/success"
            }) as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            _svc.Verify(s => s.UpsertSubscriptionAsync(It.Is<SubscriptionRecord>(r =>
                r.UserId == userId.ToString() &&
                r.PlanId == "free" &&
                r.Status == "active")), Times.Once);
        }

        [Fact]
        public async Task CreateCheckoutSession_BronzePlan_NoStripeKey_Returns500()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);

            // No STRIPE_SECRET_KEY in env or config — should return 500
            var result = await _ctrl.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest
            {
                PlanId = "bronze",
                SuccessUrl = "/success"
            }) as ObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(500);
            System.Text.Json.JsonSerializer.Serialize(result.Value).Should().Contain("STRIPE_SECRET_KEY");
        }

        // ── GET /api/subscription/subscriptions/me ───────────────────────────

        [Fact]
        public async Task GetMySubscriptions_Unauthenticated_Returns401()
        {
            var result = await _ctrl.GetMySubscriptions();
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMySubscriptions_NoRecord_Returns200EmptyArray()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString())).ReturnsAsync((SubscriptionRecord?)null);

            var result = await _ctrl.GetMySubscriptions() as OkObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetMySubscriptions_WithBronzeRecord_ReturnsBronzePlanId()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString())).ReturnsAsync(new SubscriptionRecord
            {
                UserId = userId.ToString(),
                PlanId = "bronze",
                Status = "active",
                StripeSubscriptionId = "sub_test123",
                CurrentPeriodStart = DateTime.UtcNow,
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            });

            var result = await _ctrl.GetMySubscriptions() as OkObjectResult;

            result.Should().NotBeNull();
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            json.Should().Contain("bronze");
            json.Should().Contain("active");
        }

        // ── GET /api/subscription/orders/me ─────────────────────────────────

        [Fact]
        public async Task GetMyOrders_Unauthenticated_Returns401()
        {
            var result = await _ctrl.GetMyOrders();
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMyOrders_WithOrders_ReturnsOk()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _svc.Setup(s => s.GetOrdersAsync(userId.ToString())).ReturnsAsync(new List<OrderRecord>
            {
                new() { UserId = userId.ToString(), PlanId = "bronze", Amount = 9m, Status = "paid" }
            });

            var result = await _ctrl.GetMyOrders() as OkObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }

        // ── POST /api/subscription/toggle-pay-as-you-go ──────────────────────

        [Fact]
        public async Task TogglePayAsYouGo_NullBody_Returns400()
        {
            var result = await _ctrl.TogglePayAsYouGo(null!) as BadRequestObjectResult;
            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }

        [Fact]
        public async Task TogglePayAsYouGo_Unauthenticated_Returns401()
        {
            var result = await _ctrl.TogglePayAsYouGo(new SubscriptionController.TogglePayAsYouGoRequest { Enabled = true });
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task TogglePayAsYouGo_Enable_CallsServiceWithTrue()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _svc.Setup(s => s.SetPayAsYouGoAsync(userId.ToString(), true)).Returns(Task.CompletedTask);

            var result = await _ctrl.TogglePayAsYouGo(new SubscriptionController.TogglePayAsYouGoRequest { Enabled = true }) as OkObjectResult;

            result.Should().NotBeNull();
            _svc.Verify(s => s.SetPayAsYouGoAsync(userId.ToString(), true), Times.Once);
        }

        [Fact]
        public async Task TogglePayAsYouGo_Disable_CallsServiceWithFalse()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            _svc.Setup(s => s.SetPayAsYouGoAsync(userId.ToString(), false)).Returns(Task.CompletedTask);

            await _ctrl.TogglePayAsYouGo(new SubscriptionController.TogglePayAsYouGoRequest { Enabled = false });

            _svc.Verify(s => s.SetPayAsYouGoAsync(userId.ToString(), false), Times.Once);
        }

        // ── GET /api/subscription/usage ──────────────────────────────────────

        [Fact]
        public async Task GetUsage_Unauthenticated_Returns401()
        {
            var result = await _ctrl.GetUsage();
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetUsage_ReturnsRequestCount()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            var now = DateTime.UtcNow;

            _svc.Setup(s => s.GetUsageAsync(userId.ToString(), now.Year, now.Month))
                .ReturnsAsync(new UsageRecord { UserId = userId.ToString(), Year = now.Year, Month = now.Month, RequestCount = 42 });
            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord { UserId = userId.ToString(), PlanId = "bronze", Status = "active" });

            var result = await _ctrl.GetUsage() as OkObjectResult;

            result.Should().NotBeNull();
            System.Text.Json.JsonSerializer.Serialize(result!.Value).Should().Contain("42");
        }

        [Fact]
        public async Task GetUsage_FreePlanLimit_Is1000()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            var now = DateTime.UtcNow;

            _svc.Setup(s => s.GetUsageAsync(userId.ToString(), now.Year, now.Month))
                .ReturnsAsync(new UsageRecord { UserId = userId.ToString(), RequestCount = 0 });
            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord { UserId = userId.ToString(), PlanId = "free", Status = "active" });

            var result = await _ctrl.GetUsage() as OkObjectResult;
            System.Text.Json.JsonSerializer.Serialize(result!.Value).Should().Contain("1000");
        }

        // ── Webhook — no Stripe secret configured ────────────────────────────

        [Fact]
        public async Task StripeWebhook_NoSecret_ReturnsBadRequest()
        {
            // No STRIPE_WEBHOOK_SECRET in env or config
            var httpCtx = new DefaultHttpContext();
            httpCtx.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
            _ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };

            var result = await _ctrl.StripeWebhook() as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void SetUnauthenticated()
        {
            _ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        }

        private void SetAuthenticatedUser(Guid userId)
        {
            var avatarMock = new Mock<IAvatar>();
            avatarMock.Setup(a => a.Id).Returns(userId);

            var httpCtx = new DefaultHttpContext();
            httpCtx.Items["Avatar"] = avatarMock.Object;
            _ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        }
    }
}

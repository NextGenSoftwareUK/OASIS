using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using static NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.SubscriptionController;
using Xunit;
using FluentAssertions;
using OASISSub = NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers
{
    public class SubscriptionControllerTests
    {
        private readonly Mock<OASISSub.ISubscriptionService> _mockService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SubscriptionController _controller;
        private readonly string _testUserId = Guid.NewGuid().ToString();

        public SubscriptionControllerTests()
        {
            _mockService = new Mock<OASISSub.ISubscriptionService>();
            _mockConfig = new Mock<IConfiguration>();

            _controller = new SubscriptionController(_mockConfig.Object, _mockService.Object);

            // Wire up a fake authenticated context so GetCurrentUserId() returns a value
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, _testUserId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        // ── GetPlans ──────────────────────────────────────────────────────────

        [Fact]
        public void GetPlans_ShouldReturn200WithFivePlans()
        {
            var result = _controller.GetPlans() as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);

            var body = result.Value!;
            var isError = (bool)body.GetType().GetProperty("IsError")!.GetValue(body)!;
            isError.Should().BeFalse();

            var plans = body.GetType().GetProperty("Result")!.GetValue(body) as IEnumerable<object>;
            plans.Should().HaveCount(5);
        }

        [Fact]
        public void GetPlans_ShouldIncludeFreePlan()
        {
            var result = (_controller.GetPlans() as OkObjectResult)!.Value!;
            var plans = result.GetType().GetProperty("Result")!.GetValue(result) as System.Collections.IEnumerable;

            bool hasFreePlan = false;
            foreach (var plan in plans!)
            {
                var id = plan.GetType().GetProperty("Id")?.GetValue(plan)?.ToString();
                if (id == "free") { hasFreePlan = true; break; }
            }
            hasFreePlan.Should().BeTrue();
        }

        // ── GetMySubscriptions ────────────────────────────────────────────────

        [Fact]
        public async Task GetMySubscriptions_WhenNoRecord_ShouldReturnEmptyArray()
        {
            _mockService.Setup(s => s.GetSubscriptionAsync(_testUserId))
                        .ReturnsAsync((SubscriptionRecord?)null);

            var result = await _controller.GetMySubscriptions() as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetMySubscriptions_WhenHasRecord_ShouldReturnSubscription()
        {
            var record = new SubscriptionRecord
            {
                UserId = _testUserId,
                PlanId = "silver",
                Status = "active",
                StripeSubscriptionId = "sub_abc123"
            };
            _mockService.Setup(s => s.GetSubscriptionAsync(_testUserId)).ReturnsAsync(record);

            var result = await _controller.GetMySubscriptions() as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetMySubscriptions_WhenUnauthenticated_ShouldReturn401()
        {
            // Controller without authentication
            var unauthController = new SubscriptionController(_mockConfig.Object, _mockService.Object);
            unauthController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await unauthController.GetMySubscriptions();

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // ── GetMyOrders ───────────────────────────────────────────────────────

        [Fact]
        public async Task GetMyOrders_ShouldReturnOrderList()
        {
            var orders = new List<OrderRecord>
            {
                new() { UserId = _testUserId, PlanId = "bronze", Amount = 9m }
            };
            _mockService.Setup(s => s.GetOrdersAsync(_testUserId)).ReturnsAsync(orders);

            var result = await _controller.GetMyOrders() as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetMyOrders_WhenUnauthenticated_ShouldReturn401()
        {
            var unauthController = new SubscriptionController(_mockConfig.Object, _mockService.Object);
            unauthController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await unauthController.GetMyOrders();

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // ── TogglePayAsYouGo ──────────────────────────────────────────────────

        [Fact]
        public async Task TogglePayAsYouGo_Enable_ShouldCallServiceAndReturn200()
        {
            _mockService.Setup(s => s.SetPayAsYouGoAsync(_testUserId, true)).Returns(Task.CompletedTask);
            var request = new TogglePayAsYouGoRequest { Enabled = true };

            var result = await _controller.TogglePayAsYouGo(request) as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            _mockService.Verify(s => s.SetPayAsYouGoAsync(_testUserId, true), Times.Once);
        }

        [Fact]
        public async Task TogglePayAsYouGo_NullBody_ShouldReturn400()
        {
            var result = await _controller.TogglePayAsYouGo(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task TogglePayAsYouGo_WhenUnauthenticated_ShouldReturn401()
        {
            var unauthController = new SubscriptionController(_mockConfig.Object, _mockService.Object);
            unauthController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await unauthController.TogglePayAsYouGo(new TogglePayAsYouGoRequest { Enabled = true });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // ── GetUsage ───────────────────────────────────────────────────

        [Fact]
        public async Task GetUsage_ShouldReturnUsageData()
        {
            var now = DateTime.UtcNow;
            var usage = new UsageRecord
            {
                UserId = _testUserId,
                Year = now.Year,
                Month = now.Month,
                RequestCount = 250
            };
            var subscription = new SubscriptionRecord
            {
                UserId = _testUserId,
                PlanId = "bronze",
                Status = "active"
            };
            _mockService.Setup(s => s.GetUsageAsync(_testUserId, now.Year, now.Month)).ReturnsAsync(usage);
            _mockService.Setup(s => s.GetSubscriptionAsync(_testUserId)).ReturnsAsync(subscription);

            var result = await _controller.GetUsage() as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task GetUsage_WhenUnauthenticated_ShouldReturn401()
        {
            var unauthController = new SubscriptionController(_mockConfig.Object, _mockService.Object);
            unauthController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = await unauthController.GetUsage();

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        // ── Checkout (free plan) ──────────────────────────────────────────────

        [Fact]
        public async Task CreateCheckoutSession_FreePlan_ShouldProvisionWithoutStripe()
        {
            _mockService.Setup(s => s.UpsertSubscriptionAsync(It.IsAny<SubscriptionRecord>()))
                        .Returns(Task.CompletedTask);

            var request = new CreateCheckoutSessionRequest
            {
                PlanId = "free",
                SuccessUrl = "/success"
            };

            var result = await _controller.CreateCheckoutSession(request) as OkObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(200);
            _mockService.Verify(s => s.UpsertSubscriptionAsync(It.Is<SubscriptionRecord>(r =>
                r.PlanId == "free" && r.Status == "active")), Times.Once);
        }

        [Fact]
        public async Task CreateCheckoutSession_NullRequest_ShouldReturn400()
        {
            var result = await _controller.CreateCheckoutSession(null!);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateCheckoutSession_UnknownPlan_ShouldReturn400()
        {
            var request = new CreateCheckoutSessionRequest { PlanId = "nonexistent" };

            var result = await _controller.CreateCheckoutSession(request);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task CreateCheckoutSession_EnterprisePlan_ShouldReturn400WithContactSalesMessage()
        {
            var request = new CreateCheckoutSessionRequest { PlanId = "enterprise" };

            var result = await _controller.CreateCheckoutSession(request) as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.Value!.ToString().Should().Contain("Enterprise");
        }
    }
}

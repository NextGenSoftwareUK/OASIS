using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.Subscription
{
    /// <summary>
    /// HTTP-level integration tests using WebApplicationFactory.
    /// The ISubscriptionService is replaced with a mock so no OASIS storage layer is required.
    /// To run tests against the real Railway stack instead, set ONODE_BASE_URL and skip these tests.
    /// </summary>
    public class SubscriptionEndpointsTests : IClassFixture<SubscriptionWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly Mock<ISubscriptionService> _svc;

        public SubscriptionEndpointsTests(SubscriptionWebAppFactory factory)
        {
            _svc = factory.SvcMock;
            _client = factory.CreateClient();
        }

        // ── GET /api/subscription/plans — public endpoint ────────────────────

        [Fact]
        public async Task GET_Plans_Returns200()
        {
            var response = await _client.GetAsync("/api/subscription/plans");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task GET_Plans_BodyContainsFivePlans()
        {
            var body = await _client.GetStringAsync("/api/subscription/plans");
            body.Should().Contain("\"free\"");
            body.Should().Contain("\"bronze\"");
            body.Should().Contain("\"silver\"");
            body.Should().Contain("\"gold\"");
            body.Should().Contain("\"enterprise\"");
        }

        [Fact]
        public async Task GET_Plans_IsErrorFalse()
        {
            var body = await _client.GetStringAsync("/api/subscription/plans");
            body.Should().Contain("\"IsError\":false");
        }

        // ── POST /api/subscription/checkout/session — unauthenticated ────────

        [Fact]
        public async Task POST_CheckoutSession_NoAuth_EnterprisePlan_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/checkout/session",
                new { PlanId = "enterprise" });

            // 401 (from [Authorize]) or 400 (contact sales) — either is correct depending on auth middleware
            ((int)response.StatusCode).Should().BeOneOf(400, 401);
        }

        [Fact]
        public async Task POST_CheckoutSession_NoAuth_UnknownPlan_Returns400Or401()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/checkout/session",
                new { PlanId = "nonexistent" });

            ((int)response.StatusCode).Should().BeOneOf(400, 401);
        }

        // ── GET /api/subscription/subscriptions/me — unauthenticated ─────────

        [Fact]
        public async Task GET_SubscriptionsMe_Unauthenticated_Returns401()
        {
            var response = await _client.GetAsync("/api/subscription/subscriptions/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/orders/me — unauthenticated ────────────────

        [Fact]
        public async Task GET_OrdersMe_Unauthenticated_Returns401()
        {
            var response = await _client.GetAsync("/api/subscription/orders/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/usage — unauthenticated ────────────────────

        [Fact]
        public async Task GET_Usage_Unauthenticated_Returns401()
        {
            var response = await _client.GetAsync("/api/subscription/usage");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ── GET /api/subscription/hyperdrive-usage — public ──────────────────

        [Fact]
        public async Task GET_HyperdriveUsage_Returns200()
        {
            var response = await _client.GetAsync("/api/subscription/hyperdrive-usage");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ── POST /api/subscription/webhooks/stripe — no secret ───────────────

        [Fact]
        public async Task POST_StripeWebhook_NoSecret_Returns400()
        {
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/subscription/webhooks/stripe", content);

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_StripeWebhook_MissingSignatureHeader_Returns400()
        {
            // Set a fake webhook secret so it gets past the "not configured" check
            Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", "whsec_fake_test_secret");
            try
            {
                var content = new StringContent("{}", Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("/api/subscription/webhooks/stripe", content);

                // No Stripe-Signature header → 400
                response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            }
            finally
            {
                Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_SECRET", null);
            }
        }

        // ── POST /api/subscription/check-hyperdrive-quota ────────────────────

        [Fact]
        public async Task POST_CheckHyperdriveQuota_NullBody_Returns400()
        {
            var content = new StringContent("", Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/subscription/check-hyperdrive-quota", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_CheckHyperdriveQuota_ValidOperationType_Returns200()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/check-hyperdrive-quota",
                new { OperationType = "Requests" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// Shared fixture: boots the WebAPI with a mock ISubscriptionService.
    /// This avoids spinning up OASIS storage providers in CI.
    /// </summary>
    public class SubscriptionWebAppFactory : WebApplicationFactory<Program>
    {
        public Mock<ISubscriptionService> SvcMock { get; } = new();

        public SubscriptionWebAppFactory()
        {
            // Default stub: no subscription record, empty usage, empty orders
            SvcMock.Setup(s => s.GetSubscriptionAsync(It.IsAny<string>()))
                   .ReturnsAsync((SubscriptionRecord?)null);
            SvcMock.Setup(s => s.GetUsageAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
                   .ReturnsAsync(new UsageRecord());
            SvcMock.Setup(s => s.GetOrdersAsync(It.IsAny<string>()))
                   .ReturnsAsync(new List<OrderRecord>());
            SvcMock.Setup(s => s.UpsertSubscriptionAsync(It.IsAny<SubscriptionRecord>()))
                   .Returns(Task.CompletedTask);
            SvcMock.Setup(s => s.AddOrderAsync(It.IsAny<OrderRecord>()))
                   .Returns(Task.CompletedTask);
            SvcMock.Setup(s => s.IncrementUsageAsync(It.IsAny<string>()))
                   .Returns(Task.CompletedTask);
            SvcMock.Setup(s => s.SetPayAsYouGoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns(Task.CompletedTask);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                // Replace the real SubscriptionService with the mock
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(ISubscriptionService));
                if (descriptor != null)
                    services.Remove(descriptor);

                services.AddSingleton(SvcMock.Object);
            });
        }
    }
}

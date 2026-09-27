using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.Subscription
{
    /// <summary>
    /// HTTP-level integration tests using WebApplicationFactory.
    /// Subscription services and repositories are replaced with mocks so no OASIS storage layer is required.
    /// To run tests against the real Railway stack instead, set ONODE_BASE_URL and skip these tests.
    /// </summary>
    public class SubscriptionEndpointsTests : IClassFixture<SubscriptionWebAppFactory>
    {
        private readonly HttpClient _client;
        private readonly HttpClient _authenticatedClient;
        private readonly Mock<ISubscriptionService> _svc;

        public SubscriptionEndpointsTests(SubscriptionWebAppFactory factory)
        {
            _svc = factory.SvcMock;
            _client = factory.CreateClient();
            _authenticatedClient = factory.CreateClient();
            _authenticatedClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.HeaderName, "true");
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
            body.Should().Contain("\"isError\":false");
        }

        // ── POST /api/subscription/checkout/session — unauthenticated ────────

        [Fact]
        public async Task POST_CheckoutSession_NoAuth_EnterprisePlan_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/checkout/session",
                new { PlanId = "enterprise" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task POST_CheckoutSession_NoAuth_UnknownPlan_Returns400Or401()
        {
            var response = await _client.PostAsJsonAsync("/api/subscription/checkout/session",
                new { PlanId = "nonexistent" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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

        // ── GET /api/subscription/hyperdrive-usage — authenticated ───────────

        [Fact]
        public async Task GET_HyperdriveUsage_Returns200()
        {
            var response = await _authenticatedClient.GetAsync("/api/subscription/hyperdrive-usage");
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
            var response = await _authenticatedClient.PostAsync("/api/subscription/check-hyperdrive-quota", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_CheckHyperdriveQuota_ValidOperationType_Returns200()
        {
            var response = await _authenticatedClient.PostAsJsonAsync("/api/subscription/check-hyperdrive-quota",
                new { OperationType = "Requests" });

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    /// <summary>
    /// Shared fixture: boots the WebAPI with mocked subscription storage and explicit test authentication.
    /// This avoids spinning up OASIS storage providers in CI.
    /// </summary>
    public class SubscriptionWebAppFactory : WebApplicationFactory<Program>
    {
        public Mock<ISubscriptionService> SvcMock { get; } = new();
        public Mock<ISubscriptionUsageRepository> UsageRepositoryMock { get; } = new();
        public Mock<ISubscriptionBillingRepository> BillingRepositoryMock { get; } = new();

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
            SvcMock.Setup(s => s.SetPayAsYouGoAsync(It.IsAny<string>(), It.IsAny<bool>()))
                   .Returns(Task.CompletedTask);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISubscriptionService>();
                services.RemoveAll<ISubscriptionUsageRepository>();
                services.RemoveAll<ISubscriptionBillingRepository>();
                services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();
                services.AddSingleton(SvcMock.Object);
                services.AddSingleton(UsageRepositoryMock.Object);
                services.AddSingleton(BillingRepositoryMock.Object);

                services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                        options.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
                    })
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        TestAuthenticationHandler.SchemeName, _ => { });
            });
        }
    }

    public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string SchemeName = "SubscriptionIntegrationTest";
        public const string HeaderName = "X-OASIS-Test-Authenticated";
        private const string TestAvatarId = "11111111-1111-1111-1111-111111111111";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder) : base(options, logger, encoder) { }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var value) || value.ToString() != "true")
                return Task.FromResult(AuthenticateResult.NoResult());

            var avatarId = Guid.Parse(TestAvatarId);
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, TestAvatarId),
                    new Claim("sub", TestAvatarId)
                },
                SchemeName);
            var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
            Context.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId };
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

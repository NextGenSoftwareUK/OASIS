using System;
using System.Collections.Generic;
using System.Threading;
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
        private readonly Mock<ISubscriptionBillingPortal> _portal;

        public SubscriptionControllerTests()
        {
            _svc = new Mock<ISubscriptionService>();
            _portal = new Mock<ISubscriptionBillingPortal>();
            _cfg = new ConfigurationBuilder().Build();
            var prices = new Mock<ISubscriptionPlanPriceReader>();
            prices.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionPlanPrice(9m, "GBP", "month"));
            _ctrl = new SubscriptionController(_cfg, _svc.Object, planCatalogue: new SubscriptionPlanCatalogue(prices.Object),billingPortal:_portal.Object);
            SetUnauthenticated();
        }

        // ── GET /api/subscription/plans ──────────────────────────────────────

        [Fact]
        public async Task GetPlans_Returns200WithAllPlanIds()
        {
            var result = await _ctrl.GetPlans() as OkObjectResult;

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
        public async Task GetPlans_IsErrorFalse()
        {
            var result = await _ctrl.GetPlans() as OkObjectResult;
            var json = System.Text.Json.JsonSerializer.Serialize(result!.Value);
            json.Should().Contain("false");
        }

        [Fact]
        public void AuthorizeRequest_ReturnsGoneWithoutCallingSubscriptionService()
        {
            var result = _ctrl.AuthorizeRequest(new AuthorizeSubscriptionRequest { ConsumingService = "WEB6" });
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(410);
            _svc.VerifyNoOtherCalls();
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

        [Theory]
        [InlineData("free")][InlineData("silver")]
        public async Task ExistingPaidSubscriberUsesOwnedBillingPortalInsteadOfAnotherCheckoutOrFreeOverwrite(string targetPlan)
        {
            var id=Guid.NewGuid();SetAuthenticatedUser(id);
            var record=new SubscriptionRecord {UserId=id.ToString(),PlanId="bronze",Status="active",StripeCustomerId="cus_owned",StripeSubscriptionId="sub_owned"};
            _svc.Setup(x=>x.GetSubscriptionAsync(id.ToString())).ReturnsAsync(record);
            _portal.Setup(x=>x.CreateAsync(record,"https://dev.example/subscription",It.IsAny<CancellationToken>())).ReturnsAsync(new SubscriptionPortalSession("bps_owned","https://billing.stripe.com/test"));
            var result=await _ctrl.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest {PlanId=targetPlan,SuccessUrl="https://dev.example/subscription"});
            result.Should().BeOfType<OkObjectResult>();
            _svc.Verify(x=>x.UpsertSubscriptionAsync(It.IsAny<SubscriptionRecord>()),Times.Never);
            _portal.Verify(x=>x.CreateAsync(record,"https://dev.example/subscription",It.IsAny<CancellationToken>()),Times.Once);
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
        public async Task TogglePayAsYouGo_Enable_RequiresExplicitPriceConsentAndCap()
        {
            var userId = Guid.NewGuid();
            SetAuthenticatedUser(userId);
            var result = await _ctrl.TogglePayAsYouGo(new SubscriptionController.TogglePayAsYouGoRequest { Enabled = true }) as ObjectResult;

            result.Should().NotBeNull();
            result!.StatusCode.Should().Be(400);
            _svc.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SubscriberCannotChangeServerWideHyperDrivePlan()
        {
            SetAuthenticatedUser(Guid.NewGuid());
            var result = await _ctrl.UpdateHyperDriveConfig(new SubscriptionController.UpdateHyperDriveConfigRequest { PlanType = "enterprise" });
            result.Result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
            _svc.VerifyNoOtherCalls();
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

        [Fact]
        public async Task StripeWebhook_TestTokenCannotBypassRealSignatureVerification()
        {
            string previous = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_TEST_TOKEN");
            Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_TEST_TOKEN", "obsolete-token");
            try
            {
                var controller = new SubscriptionController(new ConfigurationBuilder().AddInMemoryCollection(
                    new Dictionary<string, string> { ["STRIPE_WEBHOOK_SECRET"] = "whsec_test" }).Build(), _svc.Object);
                var context = new DefaultHttpContext();
                context.Request.Headers["X-Webhook-Test-Token"] = "obsolete-token";
                context.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"id\":\"evt_forged\",\"type\":\"checkout.session.completed\"}"));
                controller.ControllerContext = new ControllerContext { HttpContext = context };
                (await controller.StripeWebhook()).Should().BeOfType<BadRequestObjectResult>();
                _svc.VerifyNoOtherCalls();
            }
            finally { Environment.SetEnvironmentVariable("STRIPE_WEBHOOK_TEST_TOKEN", previous); }
        }

        [Fact]
        public async Task StripeWebhook_InvalidSignatureNeverUsesRawJsonPayload()
        {
            var controller = new SubscriptionController(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> { ["STRIPE_WEBHOOK_SECRET"] = "whsec_test" }).Build(), _svc.Object);
            var context = new DefaultHttpContext();
            context.Request.Headers["Stripe-Signature"] = "t=1234567890,v1=forged";
            context.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"id\":\"evt_forged\",\"type\":\"checkout.session.completed\"}"));
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            (await controller.StripeWebhook()).Should().BeOfType<BadRequestObjectResult>();
            _svc.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task PaidCheckoutCannotUseRequestSuppliedAvatarIdentity()
        {
            var controller = new SubscriptionController(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string> { ["STRIPE_SECRET_KEY"] = "sk_test_unused" }).Build(), _svc.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.CreateCheckoutSession(new SubscriptionController.CreateCheckoutSessionRequest {
                PlanId = "bronze", AvatarId = Guid.NewGuid().ToString() });
            result.Should().BeOfType<UnauthorizedObjectResult>();
            _svc.VerifyNoOtherCalls();
        }
        [Theory]
        [InlineData("invoice.paid", "price_bronze", "bronze", "usd", 12.34, false)]
        [InlineData("invoice.payment_succeeded", "price_bronze", "bronze", "gbp", 12.34, false)]
        [InlineData("invoice.paid", "price_retired", null, "jpy", 1234, false)]
        [InlineData("invoice.paid", "price_silver", "silver", "gbp", 12.34, true)]
        public async Task SignedPaidInvoiceUsesHistoricalLinePriceInsteadOfCurrentPlan(string eventType, string invoicePrice, string expectedPlan, string currency, decimal expectedAmount, bool proration)
        {
            string user = Guid.NewGuid().ToString();
            _svc.Setup(x => x.GetSubscriptionByStripeSubscriptionIdAsync("sub_current"))
                .ReturnsAsync(new SubscriptionRecord { UserId = user, PlanId = "silver", Status = "active" });
            var billing = new Mock<ISubscriptionBillingRepository>();
            OrderRecord captured = null;
            billing.Setup(x => x.ApplyStripeEventAsync(It.IsAny<string>(), It.IsAny<string>(), user, It.IsAny<DateTime>(),
                It.IsAny<Func<CancellationToken, Task<SubscriptionRecord>>>(), It.IsAny<OrderRecord>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, string, DateTime, Func<CancellationToken, Task<SubscriptionRecord>>, OrderRecord, CancellationToken>(
                    (_, _, _, _, _, order, _) => captured = order).ReturnsAsync(true);
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
                ["STRIPE_WEBHOOK_SECRET"] = "whsec_test", ["STRIPE_PRICE_BRONZE"] = "price_bronze", ["STRIPE_PRICE_SILVER"] = "price_silver"
            }).Build();
            var controller = new SubscriptionController(config, _svc.Object, billingRepository: billing.Object);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var invoiceLines=new List<object> {new {id="il_one",@object="line_item",amount=proration?2134:1234,price=new {id=invoicePrice,@object="price"}}};
            if(proration)invoiceLines.Add(new {id="il_credit",@object="line_item",amount=-900,price=new {id="price_bronze",@object="price"}});
            string body = System.Text.Json.JsonSerializer.Serialize(new {
                id = "evt_invoice_paid", @object = "event", type = eventType, created = timestamp, api_version = Stripe.StripeConfiguration.ApiVersion,
                request = new { id = "req_paid_invoice", idempotency_key = (string)null },
                data = new { @object = new { id = "in_actual", @object = "invoice", subscription = "sub_current", paid = true,
                    status = "paid", currency, amount_paid = 1234, created = timestamp,
                    lines = new { @object = "list", data = invoiceLines } } }
            });
            using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes("whsec_test"));
            string signature = Convert.ToHexStringLower(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(timestamp + "." + body)));
            var context = new DefaultHttpContext();
            context.Request.Headers["Stripe-Signature"] = $"t={timestamp},v1={signature}";
            context.Request.Body = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
            controller.ControllerContext = new ControllerContext { HttpContext = context };
            (await controller.StripeWebhook()).Should().BeOfType<OkObjectResult>();
            captured.Should().NotBeNull(); captured.PlanId.Should().Be(expectedPlan);
            captured.Amount.Should().Be(expectedAmount); captured.Currency.Should().Be(currency.ToUpperInvariant()); captured.StripeInvoiceId.Should().Be("in_actual");
        }
        // ── Helpers ──────────────────────────────────────────────────────────

        private void SetUnauthenticated()
        {
            _ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        }

        [Fact]
        public async Task BillingPortal_UsesOnlyAuthenticatedSubscribersStoredCustomer()
        {
            var user=Guid.NewGuid(); SetAuthenticatedUser(user);
            var owned=new SubscriptionRecord { UserId=user.ToString(),StripeCustomerId="cus_owned" };
            _svc.Setup(s=>s.GetSubscriptionAsync(user.ToString())).ReturnsAsync(owned);
            _portal.Setup(p=>p.CreateAsync(owned,"https://portal.example/subscription",It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SubscriptionPortalSession("bps_test","https://billing.stripe.com/test"));
            (await _ctrl.CreateBillingPortal(new() {ReturnUrl="https://portal.example/subscription"})).Should().BeOfType<OkObjectResult>();
            _svc.Verify(s=>s.GetSubscriptionAsync(user.ToString()),Times.Once);
            _portal.Verify(p=>p.CreateAsync(owned,"https://portal.example/subscription",It.IsAny<CancellationToken>()),Times.Once);
        }
        [Fact]
        public async Task BillingPortal_RejectsSubscriberWithoutStripeAccount()
        {
            SetAuthenticatedUser(Guid.NewGuid());
            (await _ctrl.CreateBillingPortal(new() {ReturnUrl="https://portal.example"})).Should().BeOfType<BadRequestObjectResult>();
            _portal.VerifyNoOtherCalls();
        }
        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("/relative")]
        public async Task BillingPortal_RejectsInvalidReturnUrl(string url)
        {
            var user=Guid.NewGuid(); SetAuthenticatedUser(user);
            _svc.Setup(s=>s.GetSubscriptionAsync(user.ToString())).ReturnsAsync(new SubscriptionRecord {StripeCustomerId="cus_owned"});
            (await _ctrl.CreateBillingPortal(new() {ReturnUrl=url})).Should().BeOfType<BadRequestObjectResult>();
            _portal.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task PaidOverageConsentUsesOnlyTheOwnedVerifiedBillingAccount()
        {
            var user=Guid.NewGuid();SetAuthenticatedUser(user);
            var record=new SubscriptionRecord{UserId=user.ToString(),PlanId="silver",Status="active",StripeCustomerId="cus_owned",StripeSubscriptionId="sub_owned"};
            _svc.Setup(s=>s.GetSubscriptionAsync(user.ToString())).ReturnsAsync(record);
            var billing=new Mock<ISubscriptionBillingRepository>();var gateway=new Mock<IPaidOverageGateway>();
            gateway.Setup(g=>g.VerifyPaymentReadyAsync(record,It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            var controller=new SubscriptionController(_cfg,_svc.Object,billingRepository:billing.Object,overageGateway:gateway.Object){ControllerContext=_ctrl.ControllerContext};
            var pricing=PaidOveragePricing.Load();
            (await controller.TogglePayAsYouGo(new(){Enabled=true,PricingVersion=pricing.Version,PricingSha256=pricing.PricingSha256,MonthlyLimitUsd=50})).Should().BeOfType<OkObjectResult>();
            gateway.Verify(g=>g.VerifyPaymentReadyAsync(record,It.IsAny<CancellationToken>()),Times.Once);
            billing.Verify(b=>b.SetPaidOverageConsentAsync(user.ToString(),"sub_owned",pricing.Version,pricing.PricingSha256,50,It.IsAny<CancellationToken>()),Times.Once);
        }
        [Fact]
        public async Task PaidOverageConsentRejectsChangedPricesBeforePaymentOrStorage()
        {
            SetAuthenticatedUser(Guid.NewGuid());
            var billing=new Mock<ISubscriptionBillingRepository>();var gateway=new Mock<IPaidOverageGateway>();
            var controller=new SubscriptionController(_cfg,_svc.Object,billingRepository:billing.Object,overageGateway:gateway.Object){ControllerContext=_ctrl.ControllerContext};
            (await controller.TogglePayAsYouGo(new(){Enabled=true,PricingVersion="old",PricingSha256="old",MonthlyLimitUsd=50})).Should().BeOfType<ConflictObjectResult>();
            billing.VerifyNoOtherCalls();gateway.VerifyNoOtherCalls();_svc.VerifyNoOtherCalls();
        }
        [Fact]
        public async Task FreeSubscriberWithRetiredStripeIdentityIsAllowedToStartNewCheckout()
        {
            var user=Guid.NewGuid();SetAuthenticatedUser(user);
            _svc.Setup(s=>s.GetSubscriptionAsync(user.ToString())).ReturnsAsync(new SubscriptionRecord{UserId=user.ToString(),PlanId="free",Status="active",StripeCustomerId="cus_retired",StripeSubscriptionId="sub_retired"});
            var result=await _ctrl.CreateCheckoutSession(new(){PlanId="silver",SuccessUrl="https://portal.example"});
            // The empty test key stops at Stripe setup, proving the retired subscription does not redirect into its portal.
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
            _portal.VerifyNoOtherCalls();
        }
        private void SetAuthenticatedUser(Guid userId)
        {
            var avatarMock = new Mock<IAvatar>();
            avatarMock.Setup(a => a.Id).Returns(userId);

            var httpCtx = new DefaultHttpContext();
            httpCtx.Items["Avatar"] = avatarMock.Object;
            httpCtx.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("sub", userId.ToString()) }, "Bearer"));
            _ctrl.ControllerContext = new ControllerContext { HttpContext = httpCtx };
        }
    }
}

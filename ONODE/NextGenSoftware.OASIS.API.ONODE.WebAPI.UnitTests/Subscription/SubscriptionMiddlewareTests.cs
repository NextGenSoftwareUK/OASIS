using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription
{
    public class SubscriptionMiddlewareTests
    {
        private readonly Mock<ISubscriptionService> _svc;
        private readonly SubscriptionMiddleware _middleware;

        public SubscriptionMiddlewareTests()
        {
            _svc = new Mock<ISubscriptionService>();
            _middleware = new SubscriptionMiddleware(
                _ => Task.CompletedTask,
                NullLogger<SubscriptionMiddleware>.Instance);
        }

        // ── Skip paths bypass the subscription check ─────────────────────────

        [Theory]
        [InlineData("/health")]
        [InlineData("/api/health")]
        [InlineData("/api/auth/login")]
        [InlineData("/api/avatar/signin")]
        [InlineData("/api/avatar/signup")]
        [InlineData("/api/subscription/plans")]
        [InlineData("/swagger/index.html")]
        [InlineData("/favicon.ico")]
        public async Task SkipPaths_BypassSubscriptionCheck_CallNext(string path)
        {
            var nextCalled = false;
            var middleware = new SubscriptionMiddleware(
                ctx => { nextCalled = true; return Task.CompletedTask; },
                NullLogger<SubscriptionMiddleware>.Instance);

            var ctx = BuildContext(path);
            await middleware.InvokeAsync(ctx);

            nextCalled.Should().BeTrue($"path '{path}' should bypass subscription check");
        }

        // ── Unauthenticated requests (no Avatar in Items) pass through ────────

        [Fact]
        public async Task NoAvatar_CallsNextWithoutSubscriptionCheck()
        {
            var nextCalled = false;
            var middleware = new SubscriptionMiddleware(
                ctx => { nextCalled = true; return Task.CompletedTask; },
                NullLogger<SubscriptionMiddleware>.Instance);

            var ctx = BuildContext("/api/some/endpoint");
            await middleware.InvokeAsync(ctx);

            nextCalled.Should().BeTrue();
        }

        // ── Active subscription: request passes through ───────────────────────

        [Fact]
        public async Task ActiveSubscription_CallsNext_And_IncrementsUsage()
        {
            var userId = Guid.NewGuid();
            var nextCalled = false;

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord { UserId = userId.ToString(), PlanId = "bronze", Status = "active" });
            _svc.Setup(s => s.GetUsageAsync(userId.ToString(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new UsageRecord { RequestCount = 0 });
            _svc.Setup(s => s.IncrementUsageAsync(userId.ToString())).Returns(Task.CompletedTask);

            var middleware = new SubscriptionMiddleware(
                ctx => { nextCalled = true; return Task.CompletedTask; },
                NullLogger<SubscriptionMiddleware>.Instance);

            var ctx = BuildContextWithAvatar("/api/some/endpoint", userId);
            RegisterService(ctx, _svc.Object);

            await middleware.InvokeAsync(ctx);

            nextCalled.Should().BeTrue();
            _svc.Verify(s => s.IncrementUsageAsync(userId.ToString()), Times.Once);
        }

        // ── No subscription record → 402 ─────────────────────────────────────

        [Fact]
        public async Task NoSubscriptionRecord_Returns402()
        {
            var userId = Guid.NewGuid();

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString())).ReturnsAsync((SubscriptionRecord?)null);

            var ctx = BuildContextWithAvatar("/api/something", userId);
            RegisterService(ctx, _svc.Object);

            await _middleware.InvokeAsync(ctx);

            ctx.Response.StatusCode.Should().Be(402);
        }

        // ── Cancelled subscription → 402 ─────────────────────────────────────

        [Fact]
        public async Task CancelledSubscription_Returns402()
        {
            var userId = Guid.NewGuid();

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord { UserId = userId.ToString(), Status = "cancelled", PlanId = "bronze" });

            var ctx = BuildContextWithAvatar("/api/something", userId);
            RegisterService(ctx, _svc.Object);

            await _middleware.InvokeAsync(ctx);

            ctx.Response.StatusCode.Should().Be(402);
        }

        // ── Expired period → 402 ─────────────────────────────────────────────

        [Fact]
        public async Task ExpiredSubscription_Returns402()
        {
            var userId = Guid.NewGuid();

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord
                {
                    UserId = userId.ToString(),
                    Status = "active",
                    PlanId = "bronze",
                    CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1) // expired yesterday
                });

            var ctx = BuildContextWithAvatar("/api/something", userId);
            RegisterService(ctx, _svc.Object);

            await _middleware.InvokeAsync(ctx);

            ctx.Response.StatusCode.Should().Be(402);
        }

        // ── Plan limit exceeded, no pay-as-you-go → 429 ──────────────────────

        [Fact]
        public async Task PlanLimitExceeded_NoPAYG_Returns429()
        {
            var userId = Guid.NewGuid();

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord
                {
                    UserId = userId.ToString(),
                    Status = "active",
                    PlanId = "free",
                    PayAsYouGoEnabled = false
                });
            _svc.Setup(s => s.GetUsageAsync(userId.ToString(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new UsageRecord { RequestCount = 1001 }); // free limit is 1000

            var ctx = BuildContextWithAvatar("/api/something", userId);
            RegisterService(ctx, _svc.Object);

            await _middleware.InvokeAsync(ctx);

            ctx.Response.StatusCode.Should().Be(429);
        }

        // ── Plan limit exceeded, PAYG enabled → increments overage ───────────

        [Fact]
        public async Task PlanLimitExceeded_WithPAYG_IncrementsOverage()
        {
            var userId = Guid.NewGuid();
            var nextCalled = false;

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString()))
                .ReturnsAsync(new SubscriptionRecord
                {
                    UserId = userId.ToString(),
                    Status = "active",
                    PlanId = "bronze",
                    PayAsYouGoEnabled = true
                });
            _svc.Setup(s => s.GetUsageAsync(userId.ToString(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new UsageRecord { RequestCount = 99_999 }); // silver limit: 100_000; bronze: 10_000 — over
            _svc.Setup(s => s.IncrementOverageAsync(userId.ToString())).Returns(Task.CompletedTask);

            var middleware = new SubscriptionMiddleware(
                ctx => { nextCalled = true; return Task.CompletedTask; },
                NullLogger<SubscriptionMiddleware>.Instance);

            var ctx = BuildContextWithAvatar("/api/something", userId);
            RegisterService(ctx, _svc.Object);

            await middleware.InvokeAsync(ctx);

            nextCalled.Should().BeTrue();
            _svc.Verify(s => s.IncrementOverageAsync(userId.ToString()), Times.Once);
        }

        // ── Free-tier paths allowed even without subscription ────────────────

        [Theory]
        [InlineData("/api/avatar/profile")]
        [InlineData("/api/avatar/basic-info")]
        public async Task FreeTierPaths_NoSubscription_CallsNext(string path)
        {
            var userId = Guid.NewGuid();
            var nextCalled = false;

            _svc.Setup(s => s.GetSubscriptionAsync(userId.ToString())).ReturnsAsync((SubscriptionRecord?)null);

            var middleware = new SubscriptionMiddleware(
                ctx => { nextCalled = true; return Task.CompletedTask; },
                NullLogger<SubscriptionMiddleware>.Instance);

            var ctx = BuildContextWithAvatar(path, userId);
            RegisterService(ctx, _svc.Object);

            await middleware.InvokeAsync(ctx);

            nextCalled.Should().BeTrue($"path '{path}' is a free-tier path and should be allowed without subscription");
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static HttpContext BuildContext(string path)
        {
            var ctx = new DefaultHttpContext();
            ctx.Request.Path = path;
            ctx.Response.Body = new MemoryStream();
            return ctx;
        }

        private static HttpContext BuildContextWithAvatar(string path, Guid userId)
        {
            var ctx = BuildContext(path);
            var avatar = new Mock<IAvatar>();
            avatar.Setup(a => a.Id).Returns(userId);
            ctx.Items["Avatar"] = avatar.Object;
            return ctx;
        }

        private static void RegisterService(HttpContext ctx, ISubscriptionService svc)
        {
            var services = new ServiceCollection();
            services.AddSingleton(svc);
            ctx.RequestServices = services.BuildServiceProvider();
        }
    }
}

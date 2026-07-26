using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.STAR.WebAPI.Services.Subscription;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.UnitTests.Controllers
{
    public class STARSubscriptionModelsTests
    {
        // ── SubscriptionRecord ──────────────────────────────────────────────

        [Fact]
        public void SubscriptionRecord_Defaults_ShouldBeCorrect()
        {
            var record = new SubscriptionRecord();

            record.PlanId.Should().Be("free");
            record.Status.Should().Be("active");
            record.PayAsYouGoEnabled.Should().BeFalse();
            record.CurrentPeriodEnd.Should().BeNull();
        }

        [Fact]
        public void SubscriptionRecord_CanSetProperties()
        {
            var now = DateTime.UtcNow;
            var record = new SubscriptionRecord
            {
                UserId = "user-star-1",
                PlanId = "gold",
                Status = "trialing",
                PayAsYouGoEnabled = true,
                CurrentPeriodEnd = now.AddMonths(1)
            };

            record.UserId.Should().Be("user-star-1");
            record.PlanId.Should().Be("gold");
            record.Status.Should().Be("trialing");
            record.PayAsYouGoEnabled.Should().BeTrue();
        }

        // ── UsageRecord ──────────────────────────────────────────────────────

        [Fact]
        public void UsageRecord_Defaults_ShouldBeZero()
        {
            var record = new UsageRecord();

            record.RequestCount.Should().Be(0);
            record.OverageCount.Should().Be(0);
        }

        [Fact]
        public void UsageRecord_CounterIncrement_ShouldBeCorrect()
        {
            var record = new UsageRecord
            {
                UserId = "user-star-2",
                Year = 2026,
                Month = 7
            };

            record.RequestCount++;
            record.RequestCount.Should().Be(1);

            record.RequestCount++;
            record.OverageCount++;
            record.OverageCount.Should().Be(1);
            record.RequestCount.Should().Be(2);
        }

        // ── IsActive logic (mirrors middleware check) ─────────────────────────

        [Fact]
        public void SubscriptionRecord_ActiveStatus_ShouldBeActive()
        {
            var record = new SubscriptionRecord { Status = "active" };
            IsActive(record).Should().BeTrue();
        }

        [Fact]
        public void SubscriptionRecord_FreeStatus_ShouldBeActive()
        {
            var record = new SubscriptionRecord { Status = "free" };
            IsActive(record).Should().BeTrue();
        }

        [Fact]
        public void SubscriptionRecord_TrialingStatus_ShouldBeActive()
        {
            var record = new SubscriptionRecord { Status = "trialing" };
            IsActive(record).Should().BeTrue();
        }

        [Fact]
        public void SubscriptionRecord_CancelledStatus_ShouldNotBeActive()
        {
            var record = new SubscriptionRecord { Status = "cancelled" };
            IsActive(record).Should().BeFalse();
        }

        [Fact]
        public void SubscriptionRecord_PastDueStatus_ShouldNotBeActive()
        {
            var record = new SubscriptionRecord { Status = "past_due" };
            IsActive(record).Should().BeFalse();
        }

        [Fact]
        public void SubscriptionRecord_ExpiredPeriod_ShouldNotBeActive()
        {
            var record = new SubscriptionRecord
            {
                Status = "active",
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1)
            };
            IsActive(record).Should().BeFalse();
        }

        [Fact]
        public void SubscriptionRecord_FuturePeriodEnd_ShouldBeActive()
        {
            var record = new SubscriptionRecord
            {
                Status = "active",
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(30)
            };
            IsActive(record).Should().BeTrue();
        }

        // Replicate the middleware IsActive logic
        private static bool IsActive(SubscriptionRecord r)
        {
            if (r.Status != "active" && r.Status != "trialing" && r.Status != "free")
                return false;
            if (r.CurrentPeriodEnd.HasValue && r.CurrentPeriodEnd.Value < DateTime.UtcNow)
                return false;
            return true;
        }

        // ── Live HolonManager (skip unless infrastructure available) ──────────

        [Fact(Skip = "Requires live HolonManager — enable in integration CI")]
        public async Task SubscriptionService_GetUsage_ShouldReturnEmptyForNewUser()
        {
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionService>();
            var service = new SubscriptionService(logger);

            var now = DateTime.UtcNow;
            var usage = await service.GetUsageAsync(Guid.NewGuid().ToString(), now.Year, now.Month);

            usage.RequestCount.Should().Be(0);
            usage.OverageCount.Should().Be(0);
        }
    }
}

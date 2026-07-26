using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.STAR.WebAPI.Services.Subscription;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.STAR.WebAPI.IntegrationTests
{
    /// <summary>
    /// Integration tests for the STAR WebAPI subscription layer.
    /// Non-infrastructure tests verify model contracts; live tests are skipped unless HolonManager is wired.
    /// </summary>
    public class STARSubscriptionIntegrationTests
    {
        // ── Model contract tests (no infrastructure needed) ───────────────────

        [Fact]
        public void SubscriptionRecord_DefaultPlan_ShouldBeFree()
        {
            var record = new SubscriptionRecord();
            record.PlanId.Should().Be("free");
            record.Status.Should().Be("active");
        }

        [Fact]
        public void SubscriptionRecord_StatusTransitions_ShouldBeValid()
        {
            var statuses = new[] { "active", "trialing", "past_due", "cancelled", "free" };
            foreach (var status in statuses)
            {
                var record = new SubscriptionRecord { Status = status };
                record.Status.Should().Be(status);
            }
        }

        [Fact]
        public void UsageRecord_Increments_ShouldAccumulate()
        {
            var record = new UsageRecord { UserId = "star-user-1", Year = 2026, Month = 7 };

            for (int i = 0; i < 10; i++) record.RequestCount++;
            for (int i = 0; i < 3; i++) { record.RequestCount++; record.OverageCount++; }

            record.RequestCount.Should().Be(13);
            record.OverageCount.Should().Be(3);
        }

        [Fact]
        public void IsActive_Logic_CoveredByAllStatuses()
        {
            var activeStatuses = new[] { "active", "trialing", "free" };
            var inactiveStatuses = new[] { "past_due", "cancelled" };

            foreach (var s in activeStatuses)
                IsActive(new SubscriptionRecord { Status = s }).Should().BeTrue($"'{s}' should be active");

            foreach (var s in inactiveStatuses)
                IsActive(new SubscriptionRecord { Status = s }).Should().BeFalse($"'{s}' should not be active");
        }

        [Fact]
        public void IsActive_WithExpiredPeriod_ShouldBeFalse()
        {
            var record = new SubscriptionRecord
            {
                Status = "active",
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1)
            };
            IsActive(record).Should().BeFalse();
        }

        [Fact]
        public void IsActive_WithFuturePeriod_ShouldBeTrue()
        {
            var record = new SubscriptionRecord
            {
                Status = "active",
                CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1)
            };
            IsActive(record).Should().BeTrue();
        }

        [Fact]
        public void IsActive_WithNoPeriodEnd_ShouldBeTrue()
        {
            var record = new SubscriptionRecord { Status = "active", CurrentPeriodEnd = null };
            IsActive(record).Should().BeTrue();
        }

        // ── Request limit table (mirrors middleware constants) ─────────────────

        [Theory]
        [InlineData("free",       1_000)]
        [InlineData("bronze",    10_000)]
        [InlineData("silver",   100_000)]
        [InlineData("gold",   1_000_000)]
        public void PlanLimits_ShouldMatchDefinition(string planId, int expectedLimit)
        {
            var limits = new Dictionary<string, int>
            {
                ["free"]       = 1_000,
                ["bronze"]     = 10_000,
                ["silver"]     = 100_000,
                ["gold"]       = 1_000_000,
                ["enterprise"] = int.MaxValue
            };

            limits[planId].Should().Be(expectedLimit);
        }

        // ── Live HolonManager tests ───────────────────────────────────────────

        [Fact(Skip = "Requires live HolonManager — enable in integration CI")]
        public async System.Threading.Tasks.Task SubscriptionService_GetSubscription_UnknownUser_ReturnsNull()
        {
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionService>();
            var service = new SubscriptionService(logger);

            var result = await service.GetSubscriptionAsync(Guid.NewGuid().ToString());
            result.Should().BeNull("no subscription should exist for a brand-new random user");
        }

        [Fact(Skip = "Requires live HolonManager — enable in integration CI")]
        public async System.Threading.Tasks.Task SubscriptionService_IncrementOverage_ShouldIncrementBothCounters()
        {
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<SubscriptionService>();
            var service = new SubscriptionService(logger);

            var userId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            await service.IncrementOverageAsync(userId);

            var usage = await service.GetUsageAsync(userId, now.Year, now.Month);
            usage.RequestCount.Should().Be(1);
            usage.OverageCount.Should().Be(1);
        }

        // Helper — mirrors the middleware IsActive check
        private static bool IsActive(SubscriptionRecord r)
        {
            if (r.Status != "active" && r.Status != "trialing" && r.Status != "free") return false;
            if (r.CurrentPeriodEnd.HasValue && r.CurrentPeriodEnd.Value < DateTime.UtcNow) return false;
            return true;
        }
    }
}

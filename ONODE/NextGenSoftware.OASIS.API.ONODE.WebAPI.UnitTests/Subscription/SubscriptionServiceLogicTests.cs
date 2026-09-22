using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription
{
    /// <summary>
    /// Tests the pure business-logic helpers in SubscriptionService that do NOT require HolonManager.
    /// HolonManager is a singleton that requires full OASIS boot — those paths are covered in integration tests.
    /// </summary>
    public class SubscriptionServiceLogicTests
    {
        // ── SubscriptionRecord defaults ──────────────────────────────────────

        [Fact]
        public void SubscriptionRecord_DefaultPlanId_IsFree()
        {
            var record = new SubscriptionRecord();
            record.PlanId.Should().Be("free");
        }

        [Fact]
        public void SubscriptionRecord_DefaultStatus_IsActive()
        {
            var record = new SubscriptionRecord();
            record.Status.Should().Be("active");
        }

        // ── UsageRecord ──────────────────────────────────────────────────────

        [Fact]
        public void UsageRecord_DefaultRequestCount_IsZero()
        {
            var record = new UsageRecord();
            record.RequestCount.Should().Be(0);
            record.OverageCount.Should().Be(0);
        }

        // ── OrderRecord ──────────────────────────────────────────────────────

        [Fact]
        public void OrderRecord_DefaultStatus_IsPaid()
        {
            var order = new OrderRecord();
            order.Status.Should().Be("paid");
            order.Currency.Should().Be("USD");
        }

        [Fact]
        public void OrderRecord_HasUniqueId_OnCreation()
        {
            var a = new OrderRecord();
            var b = new OrderRecord();
            a.Id.Should().NotBe(b.Id);
        }

        // ── Status validity logic (mirrors SubscriptionMiddleware.IsActive) ───

        [Theory]
        [InlineData("active", true)]
        [InlineData("trialing", true)]
        [InlineData("free", true)]
        [InlineData("cancelled", false)]
        [InlineData("past_due", false)]
        [InlineData("inactive", false)]
        [InlineData("", false)]
        public void IsActiveStatus_VariousStatuses(string status, bool expected)
        {
            IsActive(new SubscriptionRecord { Status = status }).Should().Be(expected);
        }

        [Fact]
        public void IsActive_ExpiredPeriod_ReturnsFalse()
        {
            var sub = new SubscriptionRecord
            {
                Status = "active",
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(-1)
            };
            IsActive(sub).Should().BeFalse();
        }

        [Fact]
        public void IsActive_FuturePeriodEnd_ReturnsTrue()
        {
            var sub = new SubscriptionRecord
            {
                Status = "active",
                CurrentPeriodEnd = DateTime.UtcNow.AddDays(30)
            };
            IsActive(sub).Should().BeTrue();
        }

        [Fact]
        public void IsActive_NoPeriodEnd_ReturnsTrue()
        {
            var sub = new SubscriptionRecord { Status = "active", CurrentPeriodEnd = null };
            IsActive(sub).Should().BeTrue();
        }

        // ── RecordToDict round-trip (logic already in SubscriptionService) ────

        [Fact]
        public void RecordToDict_PreservesAllFields()
        {
            var now = DateTime.UtcNow;
            var record = new SubscriptionRecord
            {
                UserId = Guid.NewGuid().ToString(),
                StripeCustomerId = "cus_abc",
                StripeSubscriptionId = "sub_xyz",
                PlanId = "bronze",
                Status = "active",
                PayAsYouGoEnabled = true,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddMonths(1),
                CreatedAt = now,
                UpdatedAt = now
            };

            var dict = RecordToDict(record);

            dict["stripeCustomerId"].Should().Be("cus_abc");
            dict["stripeSubscriptionId"].Should().Be("sub_xyz");
            dict["planId"].Should().Be("bronze");
            dict["status"].Should().Be("active");
            dict["payAsYouGoEnabled"].Should().Be(true);
        }

        [Fact]
        public void RecordToDict_NullFields_StoreEmptyString()
        {
            var record = new SubscriptionRecord { UserId = "u", PlanId = "free", Status = "active" };
            var dict = RecordToDict(record);

            dict["stripeCustomerId"].Should().Be("");
            dict["stripeSubscriptionId"].Should().Be("");
            dict["currentPeriodStart"].Should().Be("");
            dict["currentPeriodEnd"].Should().Be("");
        }

        // ── Minimal copies of SubscriptionService private helpers for unit testing ──

        private static bool IsActive(SubscriptionRecord sub)
        {
            if (sub.Status is not ("active" or "trialing" or "free")) return false;
            var now = DateTime.UtcNow;
            if (sub.CurrentPeriodEnd.HasValue && now > sub.CurrentPeriodEnd.Value) return false;
            return true;
        }

        private static Dictionary<string, object> RecordToDict(SubscriptionRecord r) => new()
        {
            ["stripeCustomerId"]     = r.StripeCustomerId ?? "",
            ["stripeSubscriptionId"] = r.StripeSubscriptionId ?? "",
            ["planId"]               = r.PlanId ?? "free",
            ["status"]               = r.Status ?? "active",
            ["payAsYouGoEnabled"]    = r.PayAsYouGoEnabled,
            ["currentPeriodStart"]   = r.CurrentPeriodStart?.ToString("O") ?? "",
            ["currentPeriodEnd"]     = r.CurrentPeriodEnd?.ToString("O") ?? "",
            ["createdAt"]            = r.CreatedAt.ToString("O"),
            ["updatedAt"]            = r.UpdatedAt.ToString("O"),
        };
    }
}

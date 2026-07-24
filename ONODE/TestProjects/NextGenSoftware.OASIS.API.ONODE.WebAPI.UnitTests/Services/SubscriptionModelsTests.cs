using System;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Services
{
    public class SubscriptionModelsTests
    {
        // ── SubscriptionRecord ──────────────────────────────────────────────

        [Fact]
        public void SubscriptionRecord_Defaults_ShouldBeCorrect()
        {
            var record = new SubscriptionRecord();

            record.PlanId.Should().Be("free");
            record.Status.Should().Be("active");
            record.PayAsYouGoEnabled.Should().BeFalse();
            record.StripeCustomerId.Should().BeNull();
            record.StripeSubscriptionId.Should().BeNull();
            record.CurrentPeriodStart.Should().BeNull();
            record.CurrentPeriodEnd.Should().BeNull();
        }

        [Fact]
        public void SubscriptionRecord_CreatedAt_ShouldDefaultToUtcNow()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var record = new SubscriptionRecord();
            var after = DateTime.UtcNow.AddSeconds(1);

            record.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
        }

        [Fact]
        public void SubscriptionRecord_CanSetAllProperties()
        {
            var id = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            var record = new SubscriptionRecord
            {
                UserId = id,
                StripeCustomerId = "cus_test123",
                StripeSubscriptionId = "sub_test456",
                PlanId = "gold",
                Status = "trialing",
                PayAsYouGoEnabled = true,
                CurrentPeriodStart = now,
                CurrentPeriodEnd = now.AddMonths(1),
                CreatedAt = now,
                UpdatedAt = now
            };

            record.UserId.Should().Be(id);
            record.StripeCustomerId.Should().Be("cus_test123");
            record.StripeSubscriptionId.Should().Be("sub_test456");
            record.PlanId.Should().Be("gold");
            record.Status.Should().Be("trialing");
            record.PayAsYouGoEnabled.Should().BeTrue();
            record.CurrentPeriodStart.Should().Be(now);
            record.CurrentPeriodEnd.Should().Be(now.AddMonths(1));
        }

        // ── UsageRecord ──────────────────────────────────────────────────────

        [Fact]
        public void UsageRecord_Defaults_ShouldBeZero()
        {
            var record = new UsageRecord();

            record.RequestCount.Should().Be(0);
            record.OverageCount.Should().Be(0);
            record.StorageUsedGB.Should().Be(0);
        }

        [Fact]
        public void UsageRecord_CanSetCounters()
        {
            var record = new UsageRecord
            {
                UserId = "user-1",
                Year = 2026,
                Month = 7,
                RequestCount = 500,
                OverageCount = 50,
                StorageUsedGB = 2.5
            };

            record.RequestCount.Should().Be(500);
            record.OverageCount.Should().Be(50);
            record.StorageUsedGB.Should().Be(2.5);
            record.Year.Should().Be(2026);
            record.Month.Should().Be(7);
        }

        // ── OrderRecord ──────────────────────────────────────────────────────

        [Fact]
        public void OrderRecord_Id_ShouldDefaultToNewGuid()
        {
            var r1 = new OrderRecord();
            var r2 = new OrderRecord();

            r1.Id.Should().NotBeNullOrEmpty();
            r2.Id.Should().NotBeNullOrEmpty();
            r1.Id.Should().NotBe(r2.Id);
        }

        [Fact]
        public void OrderRecord_Defaults_ShouldBeCorrect()
        {
            var record = new OrderRecord();

            record.Currency.Should().Be("USD");
            record.Status.Should().Be("paid");
        }

        [Fact]
        public void OrderRecord_CanSetAllProperties()
        {
            var now = DateTime.UtcNow;
            var record = new OrderRecord
            {
                UserId = "user-2",
                PlanId = "silver",
                StripeInvoiceId = "in_test789",
                Description = "Monthly Silver subscription",
                Amount = 29.00m,
                Currency = "GBP",
                Status = "open",
                CreatedAt = now
            };

            record.PlanId.Should().Be("silver");
            record.StripeInvoiceId.Should().Be("in_test789");
            record.Amount.Should().Be(29.00m);
            record.Currency.Should().Be("GBP");
            record.Status.Should().Be("open");
        }
    }
}

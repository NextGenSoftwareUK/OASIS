using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public class SandboxUsageAllowanceTests
{
    private static readonly DateTime Now = new(2026, 10, 31, 12, 0, 0, DateTimeKind.Utc);
    private sealed class Clock : TimeProvider { public override DateTimeOffset GetUtcNow() => new(Now); }
    private static SubscriptionRecord Temporary() => new()
    {
        UserId = Guid.NewGuid().ToString("D"), PlanId = "free", Status = "free",
        CreatedAt = Now.AddMinutes(-5), CurrentPeriodStart = Now.AddMinutes(-5),
        CurrentPeriodEnd = Now.AddHours(1), SandboxExpiresAtUtc = Now.AddHours(1)
    };

    [Fact]
    public void ActiveIssuedDemoGets200CallsWithoutChangingOtherLimitsOrTheSharedFreePolicy()
    {
        var ordinary = SubscriptionService.GetUsagePolicy("free", 0);
        var effective = SandboxSubscriptionRules.EffectivePolicy(ordinary, Temporary(), Now);

        Assert.Equal(200, effective.DailyCallLimit);
        Assert.Equal(20, ordinary.DailyCallLimit);
        Assert.Equal(ordinary.MonthlyRequestLimit, effective.MonthlyRequestLimit);
        Assert.Equal(ordinary.DailyTokenLimit, effective.DailyTokenLimit);
        Assert.Equal(ordinary.MonthlyBudgetUsd, effective.MonthlyBudgetUsd);
        Assert.NotSame(ordinary, effective);
    }

    [Theory]
    [InlineData("ordinary")] [InlineData("paid-plan")] [InlineData("stripe-customer")]
    [InlineData("stripe-subscription")] [InlineData("overages")] [InlineData("period-mismatch")]
    [InlineData("expired")] [InlineData("future")] [InlineData("cancelled")]
    [InlineData("missing-created")] [InlineData("period-start-mismatch")]
    public void OrdinaryPaidExpiredOrInvalidSubscriptionsNeverReceiveTheDemoAllowance(string scenario)
    {
        var record = Temporary();
        switch (scenario)
        {
            case "ordinary": record.SandboxExpiresAtUtc = null; break;
            case "paid-plan": record.PlanId = "bronze"; break;
            case "stripe-customer": record.StripeCustomerId = "synthetic-customer"; break;
            case "stripe-subscription": record.StripeSubscriptionId = "synthetic-subscription"; break;
            case "overages": record.PayAsYouGoEnabled = true; break;
            case "period-mismatch": record.CurrentPeriodEnd = Now.AddHours(2); break;
            case "expired": record.CurrentPeriodEnd = record.SandboxExpiresAtUtc = Now; break;
            case "future": record.CreatedAt = Now.AddMinutes(1); break;
            case "cancelled": record.Status = "cancelled"; break;
            case "missing-created": record.CreatedAt = default; break;
            case "period-start-mismatch": record.CurrentPeriodStart = Now; break;
        }
        var ordinary = SubscriptionService.GetUsagePolicy(record.PlanId, 0);
        Assert.Same(ordinary, SandboxSubscriptionRules.EffectivePolicy(ordinary, record, Now));
    }

    [Fact]
    public void ExistingUsageIsPreservedAndEnforcementStopsAt200WithTokenAndMoneyCapsIntact()
    {
        var ordinary = SubscriptionService.GetUsagePolicy("free", 0);
        var effective = SandboxSubscriptionRules.EffectivePolicy(ordinary, Temporary(), Now);
        var aggregate = new SubscriptionUsageAggregate { DailyCalls = 20, MonthlyRequests = 20 };
        Assert.Throws<SubscriptionUsageLimitException>(() => SubscriptionUsagePolicyEvaluator.EnsureAuthorized(ordinary, aggregate, 0, 0));
        SubscriptionUsagePolicyEvaluator.EnsureAuthorized(effective, aggregate, 0, 0);
        aggregate.DailyCalls = 200;
        Assert.Equal("DAILY_CALL_LIMIT_EXCEEDED", Assert.Throws<SubscriptionUsageLimitException>(() =>
            SubscriptionUsagePolicyEvaluator.EnsureAuthorized(effective, aggregate, 0, 0)).Code);
        aggregate.DailyCalls = 20;
        Assert.Equal("DAILY_TOKEN_LIMIT_EXCEEDED", Assert.Throws<SubscriptionUsageLimitException>(() =>
            SubscriptionUsagePolicyEvaluator.EnsureAuthorized(effective, aggregate, 50_001, 0)).Code);
        Assert.Equal("MONTHLY_BUDGET_EXCEEDED", Assert.Throws<SubscriptionUsageLimitException>(() =>
            SubscriptionUsagePolicyEvaluator.EnsureAuthorized(effective, aggregate, 0, 2)).Code);
        Assert.Equal(20, aggregate.DailyCalls); // No usage reset or replacement subscription.
    }

    [Fact]
    public async Task UsageSummaryReportsTheSameActiveDemoPolicyAndExistingCounters()
    {
        var record = Temporary();
        var billing = new Mock<ISubscriptionBillingRepository>(MockBehavior.Strict);
        billing.Setup(store => store.GetSubscriptionAsync(record.UserId, default)).ReturnsAsync(record);
        var usage = new Mock<ISubscriptionUsageRepository>(MockBehavior.Strict);
        usage.Setup(store => store.GetAggregateAsync(record.UserId, default))
            .ReturnsAsync(new SubscriptionUsageAggregate { DailyCalls = 20, MonthlyRequests = 20 });
        var service = new SubscriptionService(NullLogger<SubscriptionService>.Instance, usage.Object, billing.Object, new Clock());

        var summary = await service.GetUsageSummaryAsync(record.UserId, 0, default);

        Assert.Equal("free", summary.PlanId);
        Assert.Equal(200, summary.DailyCallLimit);
        Assert.Equal(20, summary.DailyCalls);
        Assert.Equal(180, summary.DailyCallsRemaining);
        Assert.Equal(50_000, summary.DailyTokenLimit);
        Assert.Equal(1m, summary.MonthlyBudgetUsd);
        Assert.False(record.PayAsYouGoEnabled);
        billing.Verify(store => store.GetSubscriptionAsync(record.UserId, default), Times.Once);
        usage.Verify(store => store.GetAggregateAsync(record.UserId, default), Times.Once);
        billing.VerifyNoOtherCalls();
        usage.VerifyNoOtherCalls();
    }
}

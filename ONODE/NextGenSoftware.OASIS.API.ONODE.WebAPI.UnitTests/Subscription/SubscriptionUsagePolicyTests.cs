using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

public class SubscriptionUsagePolicyTests
{
    [Theory]
    [InlineData("free", 1000, 20, 50000, 1)]
    [InlineData("bronze", 10000, 100, 250000, 10)]
    [InlineData("silver", 100000, 500, 1000000, 50)]
    [InlineData("gold", 1000000, 2000, 5000000, 250)]
    [InlineData("enterprise", -1, 0, 0, 0)]
    public void EveryPlanHasOneAuthoritativePolicy(string plan, int monthly, int daily, long tokens, decimal budget)
    {
        var policy = SubscriptionService.GetUsagePolicy(plan, 0);
        policy.MonthlyRequestLimit.Should().Be(monthly);
        policy.DailyCallLimit.Should().Be(daily);
        policy.DailyTokenLimit.Should().Be(tokens);
        policy.MonthlyBudgetUsd.Should().Be(budget);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, 1)]
    [InlineData(499, 1)]
    [InlineData(500, 1.5)]
    [InlineData(999, 1.5)]
    [InlineData(1000, 2)]
    [InlineData(4999, 2)]
    [InlineData(5000, 3)]
    [InlineData(19999, 3)]
    [InlineData(20000, 5)]
    [InlineData(99999, 5)]
    [InlineData(100000, 10)]
    public void KarmaBoundaryMatrixIsStable(int karma, decimal multiplier) =>
        SubscriptionService.KarmaMultiplier(karma).Should().Be(multiplier);

    [Theory]
    [InlineData("free", 500, 30)]
    [InlineData("bronze", 1000, 200)]
    [InlineData("silver", 5000, 1500)]
    [InlineData("gold", 100000, 20000)]
    public void KarmaOnlyScalesDailyCalls(string plan, int karma, int expectedDailyCalls)
    {
        var baseline = SubscriptionService.GetUsagePolicy(plan, 0);
        var scaled = SubscriptionService.GetUsagePolicy(plan, karma);
        scaled.DailyCallLimit.Should().Be(expectedDailyCalls);
        scaled.MonthlyRequestLimit.Should().Be(baseline.MonthlyRequestLimit);
        scaled.DailyTokenLimit.Should().Be(baseline.DailyTokenLimit);
        scaled.MonthlyBudgetUsd.Should().Be(baseline.MonthlyBudgetUsd);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(500)]
    [InlineData(100000)]
    public void EnterpriseRemainsUnlimitedAtEveryKarmaLevel(int karma)
    {
        var policy = SubscriptionService.GetUsagePolicy("enterprise", karma);
        policy.MonthlyRequestLimit.Should().Be(-1);
        policy.DailyCallLimit.Should().Be(0);
        policy.DailyTokenLimit.Should().Be(0);
        policy.MonthlyBudgetUsd.Should().Be(0);
    }

    [Theory]
    [InlineData(10, 9, 0, 0, 0, 0, 1, 0, null)]
    [InlineData(10, 10, 0, 0, 0, 0, 1, 0, "MONTHLY_REQUEST_LIMIT_EXCEEDED")]
    [InlineData(-1, 999999, 20, 19, 0, 0, 1, 0, null)]
    [InlineData(-1, 0, 20, 20, 0, 0, 1, 0, "DAILY_CALL_LIMIT_EXCEEDED")]
    [InlineData(-1, 0, 0, 0, 100, 99, 1, 0, null)]
    [InlineData(-1, 0, 0, 0, 100, 99, 2, 0, "DAILY_TOKEN_LIMIT_EXCEEDED")]
    public void EveryCounterBoundaryIsEnforced(int monthlyLimit, long monthlyUsed, int dailyLimit, long dailyUsed,
        long tokenLimit, long tokensUsed, long requestedUnits, decimal estimatedCost, string expectedCode)
    {
        var policy = new SubscriptionUsagePolicy { MonthlyRequestLimit=monthlyLimit, DailyCallLimit=dailyLimit, DailyTokenLimit=tokenLimit };
        var aggregate = new SubscriptionUsageAggregate { MonthlyRequests=monthlyUsed, DailyCalls=dailyUsed, DailyTokens=tokensUsed };
        Action action = () => SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy, aggregate, requestedUnits, estimatedCost);
        if (expectedCode == null) action.Should().NotThrow();
        else action.Should().Throw<SubscriptionUsageLimitException>().Which.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData(10, 4, 5, 1, null)]
    [InlineData(10, 4, 5, 1.01, "MONTHLY_BUDGET_EXCEEDED")]
    [InlineData(0, 1000000, 1000000, 1000000, null)]
    public void BudgetIncludesReservedAndSettledSpend(decimal budget, decimal settled, decimal reserved, decimal estimate, string expectedCode)
    {
        var policy = new SubscriptionUsagePolicy { MonthlyRequestLimit=-1, MonthlyBudgetUsd=budget };
        var aggregate = new SubscriptionUsageAggregate { SettledCostUsd=settled, ReservedCostUsd=reserved };
        Action action = () => SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy, aggregate, 0, estimate);
        if (expectedCode == null) action.Should().NotThrow();
        else action.Should().Throw<SubscriptionUsageLimitException>().Which.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -0.01)]
    public void NegativeReservationInputsAreRejected(long units, decimal cost)
    {
        Action action = () => SubscriptionUsagePolicyEvaluator.EnsureAuthorized(
            new SubscriptionUsagePolicy { MonthlyRequestLimit=-1 }, new SubscriptionUsageAggregate(), units, cost);
        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}

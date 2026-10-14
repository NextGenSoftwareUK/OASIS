using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class PaidOverageMathTests
{
    [Theory]
    [InlineData(9999,0)]
    [InlineData(10000,0.001)]
    [InlineData(10001,0.001)]
    public void OnlyRequestsAboveTheIncludedMonthlyAllowanceReserveAFee(long used,decimal expected)
        => Assert.Equal(expected,PaidOveragePricing.RequestReservation(new(){MonthlyRequestLimit=10000},new(){MonthlyRequests=used},0.001m));
    [Fact]
    public void UnlimitedPlansHaveNoRequestOverages()
        => Assert.Equal(0,PaidOveragePricing.RequestReservation(new(){MonthlyRequestLimit=-1},new(){MonthlyRequests=long.MaxValue},0.0001m));
    [Theory]
    [InlineData(0,0.8,0)]
    [InlineData(0.8,0.4,0.2)]
    [InlineData(1.2,0.4,0.4)]
    public void AiBillsOnlyMeasuredUsdCostBeyondTheIncludedBudget(decimal used,decimal actual,decimal expected)
        => Assert.Equal(expected,PaidOveragePricing.AiSettlement(new(){MonthlyBudgetUsd=1},new(){SettledCostUsd=used},actual));
    [Fact]
    public void AiSettlementOrderCannotCreateOrLoseCharges()
    {
        var policy=new SubscriptionUsagePolicy {MonthlyBudgetUsd=1};
        var usage=new SubscriptionUsageAggregate();
        var first=PaidOveragePricing.AiSettlement(policy,usage,0.6m);usage.SettledCostUsd+=0.6m;
        var second=PaidOveragePricing.AiSettlement(policy,usage,0.6m);
        Assert.Equal(0.2m,first+second);
    }
    [Theory]
    [InlineData(0.004,0)]
    [InlineData(0.005,1)]
    [InlineData(0.014,1)]
    [InlineData(0.015,2)]
    public void AggregateUsdBillingRoundsOnceAtTheStripeCentBoundary(decimal amount,long expected)
        => Assert.Equal(expected,PaidOveragePricing.MinorUnits(amount));
    [Fact]
    public void FailedOveragePaymentDoesNotBlockZeroCostIncludedRequests()
    {
        var policy=new SubscriptionUsagePolicy {MonthlyRequestLimit=100,MonthlyBudgetUsd=1};
        var usage=new SubscriptionUsageAggregate {MonthlyRequests=10,SettledCostUsd=2};
        SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy,usage,0,0);
        Assert.Throws<SubscriptionUsageLimitException>(()=>SubscriptionUsagePolicyEvaluator.EnsureAuthorized(policy,usage,1,.01m));
    }
    [Fact]
    public void AgreedPricingRejectsAnAiMarginOrMixedCurrency()
    {
        var pricing=new PaidOveragePricing {Version="v1",Currency="USD",RequestRatesUsd=new(){{"bronze",.001m},{"silver",.0005m},{"gold",.0002m},{"enterprise",.0001m}}};
        pricing.Validate();pricing.AiMargin=.1m;Assert.Throws<InvalidOperationException>(()=>pricing.Validate());
        pricing.AiMargin=0;pricing.Currency="GBP";Assert.Throws<InvalidOperationException>(()=>pricing.Validate());
    }
}

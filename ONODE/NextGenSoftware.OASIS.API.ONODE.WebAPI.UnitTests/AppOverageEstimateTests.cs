using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class AppOverageEstimateTests
{
    [Fact]
    public void NothingOwedWithinTheIncludedAllowance()
    {
        var r = AppOverageEstimate.Compute(900, 1000, 0.001m, 50m);
        Assert.Equal(0, r.OverRequests); Assert.Equal(0m, r.GrossUsd); Assert.False(r.CapReached);
    }

    [Fact]
    public void OverageIsRequestsAboveTheLimitTimesTheRate()
    {
        var r = AppOverageEstimate.Compute(1500, 1000, 0.001m, 50m);
        Assert.Equal(500, r.OverRequests); Assert.Equal(0.5m, r.GrossUsd); Assert.Equal(0.5m, r.ChargedUsd); Assert.False(r.CapReached);
        Assert.Equal(49500, r.RequestsLeftUnderCap);
    }

    [Fact]
    public void TheOwnersMonthlyCapLimitsWhatWouldBeBilled()
    {
        var r = AppOverageEstimate.Compute(200_000, 1000, 0.001m, 50m);
        Assert.Equal(199, r.GrossUsd); Assert.Equal(50m, r.ChargedUsd); Assert.True(r.CapReached); Assert.Equal(0, r.RequestsLeftUnderCap);
    }

    [Theory]
    [InlineData(5000, 0, 0.001, 50)]   // unlimited plan
    [InlineData(5000, 1000, 0, 50)]    // no price (free plan)
    public void NoLimitOrNoPriceMeansNothingCanBeOwed(long used, long limit, double rate, double cap)
    {
        var r = AppOverageEstimate.Compute(used, limit, (decimal)rate, (decimal)cap);
        Assert.Equal(0m, r.GrossUsd); Assert.Equal(0m, r.ChargedUsd); Assert.False(r.CapReached);
    }

    [Fact]
    public void NoCapSetMeansTheGrossAmountIsShownButNotCapped()
    {
        var r = AppOverageEstimate.Compute(2000, 1000, 0.002m, 0m);
        Assert.Equal(2m, r.ChargedUsd); Assert.False(r.CapReached);
    }
}

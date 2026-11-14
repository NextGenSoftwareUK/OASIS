using System;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class UserOverageEstimateTests
{
    [Fact]
    public void ProjectsMonthEndFromThePaceSoFar()
    {
        // 10 days into a 30-day month with 12,000 requests: 36,000 expected by month end.
        var r = UserOverageEstimate.Compute(12_000, 30_000, 0.001m, 50m, new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc));
        Assert.Equal(36_000, r.ProjectedMonthRequests); Assert.Equal(0, r.SoFar.OverRequests);
        Assert.Equal(6_000, r.Projected.OverRequests); Assert.Equal(6m, r.Projected.ChargedUsd);
    }

    [Fact]
    public void OnTheLastDayTheProjectionIsTheActualCount()
    {
        var r = UserOverageEstimate.Compute(31_000, 30_000, 0.001m, 50m, new DateTime(2026, 11, 30, 9, 0, 0, DateTimeKind.Utc));
        Assert.Equal(31_000, r.ProjectedMonthRequests); Assert.Equal(1m, r.SoFar.ChargedUsd); Assert.Equal(1m, r.Projected.ChargedUsd);
    }

    [Fact]
    public void TheCapLimitsBothNumbers()
    {
        var r = UserOverageEstimate.Compute(500_000, 30_000, 0.001m, 20m, new DateTime(2026, 11, 15, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(20m, r.SoFar.ChargedUsd); Assert.True(r.SoFar.CapReached); Assert.Equal(20m, r.Projected.ChargedUsd);
    }

    [Fact]
    public void NoPriceOrUnlimitedPlanMeansNothingOwed()
    {
        Assert.Equal(0m, UserOverageEstimate.Compute(999_999, 30_000, 0m, 50m, DateTime.UtcNow).Projected.ChargedUsd);
        Assert.Equal(0m, UserOverageEstimate.Compute(999_999, 0, 0.001m, 50m, DateTime.UtcNow).Projected.ChargedUsd);
    }
}

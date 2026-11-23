using System;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class UsageVisibilityTests
{
    [Fact]
    public void ShippedAppOveragePricesMirrorIndividualsAndFreeHasNone()
    {
        var prices = AppOveragePrices.Load();
        Assert.False(prices.HasPrice("free"));
        foreach (var plan in new[] { "bronze", "silver", "gold", "enterprise" }) Assert.True(prices.HasPrice(plan));
        Assert.StartsWith("app-overages-", prices.Version);
    }

    [Fact]
    public void UsageVisibilityDefaultsAreOnExceptNothingElseSurprising()
    {
        var s = new UsageVisibilitySettings();
        Assert.True(s.OwnerViewEnabled); Assert.True(s.WarningsEnabled); Assert.True(s.EmailEnabled); Assert.True(s.AppPayAsYouGoEnabled);
    }

    [Fact]
    public void DefaultThresholdsAreUsedWhenNoneAreChosen()
        => Assert.Equal(new[] { 80, 100 }, UsageAlerts.NormaliseThresholds(null, new[] { 80, 100 }).ToArray());

    [Fact]
    public void ThresholdsAreSortedDistinctAndLimited()
    {
        Assert.Equal(new[] { 50, 90, 100 }, UsageAlerts.NormaliseThresholds(new[] { 100, 50, 90, 50 }, null).ToArray());
        Assert.Throws<ArgumentException>(() => UsageAlerts.NormaliseThresholds(new[] { 0 }, null));
        Assert.Throws<ArgumentException>(() => UsageAlerts.NormaliseThresholds(new[] { 101 }, null));
        Assert.Throws<ArgumentException>(() => UsageAlerts.NormaliseThresholds(new[] { 10, 20, 30, 40, 50 }, null));
    }

    [Theory]
    [InlineData(79, 100, 0)]
    [InlineData(80, 100, 80)]
    [InlineData(99, 100, 80)]
    [InlineData(100, 100, 100)]
    [InlineData(250, 100, 100)]
    public void EvaluateReportsTheHighestThresholdReached(long used, long limit, int expectedThreshold)
    {
        var alert = UsageAlerts.Evaluate("user", null, used, limit, new[] { 80, 100 });
        if (expectedThreshold == 0) Assert.Null(alert);
        else { Assert.Equal(expectedThreshold, alert.ThresholdPercent); Assert.Equal(used >= limit ? "exhausted" : "warning", alert.Level); }
    }

    [Fact]
    public void UnlimitedPlansNeverAlert() => Assert.Null(UsageAlerts.Evaluate("user", null, 1_000_000, 0, new[] { 80, 100 }));

    [Fact]
    public void CustomThresholdsAreHonoured()
    {
        var alert = UsageAlerts.Evaluate("app", "piper", 50, 100, new[] { 50, 90 });
        Assert.Equal(50, alert.ThresholdPercent); Assert.Equal("app", alert.Scope); Assert.Equal("piper", alert.AppId);
    }
}

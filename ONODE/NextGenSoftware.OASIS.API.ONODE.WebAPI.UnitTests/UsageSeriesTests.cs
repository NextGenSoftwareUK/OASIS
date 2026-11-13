using System;
using System.Linq;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class UsageSeriesTests
{
    private static readonly DateTime Now = new DateTime(2026, 11, 13, 15, 0, 0, DateTimeKind.Utc);
    private static UsageDay D(string date, long r, long u = 0, decimal c = 0) => new UsageDay { Date = date, Requests = r, Units = u, CostUsd = c };

    [Fact]
    public void DefaultRangeIsTheLast30DaysEndingToday()
    {
        var (from, to) = UsageSeries.NormaliseRange(null, null, Now);
        Assert.Equal(new DateTime(2026, 11, 13), to);
        Assert.Equal(new DateTime(2026, 10, 15), from);
        Assert.Equal(30, (to - from).Days + 1);
    }

    [Fact]
    public void ReversedRangeIsSwappedAndFutureEndIsClampedToToday()
    {
        var (from, to) = UsageSeries.NormaliseRange("2026-11-20", "2026-11-05", Now);
        Assert.Equal(new DateTime(2026, 11, 5), from);
        Assert.Equal(new DateTime(2026, 11, 13), to);
    }

    [Fact]
    public void RangesOver92DaysAndBadDatesAreRejected()
    {
        Assert.Throws<ArgumentException>(() => UsageSeries.NormaliseRange("2026-01-01", "2026-11-13", Now));
        Assert.Throws<ArgumentException>(() => UsageSeries.NormaliseRange("not-a-date", null, Now));
        Assert.Throws<ArgumentException>(() => UsageSeries.NormaliseRange("2026-13-40", null, Now));
        Assert.Throws<ArgumentException>(() => UsageSeries.NormaliseRange("2026-08-12", "2026-11-12", Now)); // 93 days
    }

    [Fact]
    public void ExactlyNinetyTwoDaysIsAllowed()
    {
        var (from, to) = UsageSeries.NormaliseRange("2026-08-13", "2026-11-12", Now);
        Assert.Equal(92, (to - from).Days + 1);
    }

    [Fact]
    public void MissingDaysAreZeroFilledAndOrderedOldestFirst()
    {
        var days = UsageSeries.Fill(new[] { D("2026-11-12", 7), D("2026-11-10", 3) }, new DateTime(2026, 11, 10), new DateTime(2026, 11, 13));
        Assert.Equal(new[] { "2026-11-10", "2026-11-11", "2026-11-12", "2026-11-13" }, days.Select(d => d.Date).ToArray());
        Assert.Equal(new long[] { 3, 0, 7, 0 }, days.Select(d => d.Requests).ToArray());
    }

    [Fact]
    public void PoolShardBucketsForTheSameDayAreSummed()
    {
        var days = UsageSeries.Fill(new[] { D("2026-11-12", 5, 100, 0.5m), D("2026-11-12", 6, 50, 0.25m), D("2026-11-12", 1) }, new DateTime(2026, 11, 12), new DateTime(2026, 11, 12));
        var day = Assert.Single(days);
        Assert.Equal(12, day.Requests); Assert.Equal(150, day.Units); Assert.Equal(0.75m, day.CostUsd);
    }

    [Theory]
    [InlineData(0, 100, 80, "ok")]
    [InlineData(79, 100, 80, "ok")]
    [InlineData(80, 100, 80, "warning")]
    [InlineData(99, 100, 80, "warning")]
    [InlineData(100, 100, 80, "exhausted")]
    [InlineData(150, 100, 80, "exhausted")]
    [InlineData(50, 100, 50, "warning")]
    [InlineData(5000, 0, 80, "")]
    public void StatusFollowsTheThresholds(long used, long limit, int warnAt, string expected)
        => Assert.Equal(expected, UsageSeries.Status(used, limit, warnAt));

    [Fact]
    public void BuildReportsTotalsPeakTodayAndLimits()
    {
        var r = UsageSeries.Build("u1", new[] { D("2026-11-12", 40), D("2026-11-13", 90) }, new DateTime(2026, 11, 11), new DateTime(2026, 11, 13), 100, 1000, 850, Now);
        Assert.Equal(130, r.TotalRequests); Assert.Equal(90, r.PeakDayRequests); Assert.Equal(90, r.TodayRequests);
        Assert.Equal(3, r.Days.Count); Assert.Equal(100, r.DailyLimit); Assert.Equal(1000, r.MonthlyLimit); Assert.Equal("warning", r.Status);
    }

    [Fact]
    public void UnlimitedPlansReportNoLimitAndNoStatus()
    {
        var r = UsageSeries.Build("u1", new UsageDay[0], new DateTime(2026, 11, 13), new DateTime(2026, 11, 13), -1, -1, 999999, Now);
        Assert.Equal(0, r.MonthlyLimit); Assert.Equal(0, r.DailyLimit); Assert.Equal("", r.Status);
    }
}

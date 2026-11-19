using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

/// <summary>Pool thresholds, shard routing and the admin status summary. Pure logic; no database.</summary>
[Collection("PooledFlag")]
public sealed class PooledAppQuotaOperationsTests : IDisposable
{
    private readonly string _original = Environment.GetEnvironmentVariable("OASIS_POOL_SHARDS");
    public void Dispose() => Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", _original);

    [Theory]
    [InlineData(78, 100, new int[0])]
    [InlineData(79, 100, new[] { 80 })]
    [InlineData(80, 100, new int[0])]
    [InlineData(99, 100, new[] { 100 })]
    [InlineData(100, 100, new int[0])]
    [InlineData(0, 1, new[] { 80, 100 })]
    [InlineData(5, 0, new int[0])]
    public void ThresholdsFireOnceAtTheRequestThatCrossesThem(long before, long limit, int[] expected) =>
        Assert.Equal(expected, MongoSubscriptionUsageRepository.ThresholdsCrossed(before, limit).ToArray());

    [Theory]
    [InlineData(null, 1)] [InlineData("", 1)] [InlineData("abc", 1)] [InlineData("0", 1)]
    [InlineData("4", 4)] [InlineData("99", 16)]
    public void ShardCountDefaultsToOneAndIsClamped(string value, int expected)
    {
        Environment.SetEnvironmentVariable("OASIS_POOL_SHARDS", value);
        Assert.Equal(expected, MongoSubscriptionUsageRepository.PoolShardCount);
    }

    [Fact]
    public void OneShardKeepsTheOriginalPoolSubject() =>
        Assert.Equal("app:demo", MongoSubscriptionUsageRepository.PoolWriteSubject("demo", Guid.NewGuid().ToString(), 1));

    [Fact]
    public void ShardRoutingIsStablePerOperationAndSpreadsAcrossShards()
    {
        var op = Guid.NewGuid().ToString();
        Assert.Equal(MongoSubscriptionUsageRepository.PoolWriteSubject("demo", op, 8), MongoSubscriptionUsageRepository.PoolWriteSubject("demo", op, 8));
        var subjects = Enumerable.Range(0, 400).Select(_ => MongoSubscriptionUsageRepository.PoolWriteSubject("demo", Guid.NewGuid().ToString(), 8)).Distinct().ToList();
        Assert.Equal(8, subjects.Count);
        Assert.All(subjects, s => Assert.StartsWith("app:demo", s));
        Assert.Contains("app:demo", subjects);
    }

    [Theory]
    [InlineData(10, 10, "ok")]
    [InlineData(79, 10, "ok")]
    [InlineData(80, 10, "warning")]
    [InlineData(99, 10, "warning")]
    [InlineData(100, 10, "exhausted")]
    [InlineData(10, 85, "warning")]
    [InlineData(10, 100, "exhausted")]
    public void StatusReflectsTheBusiestLimit(long monthly, long daily, string expected)
    {
        var limits = new SubscriptionUsagePolicy { MonthlyRequestLimit = 100, DailyCallLimit = 100 };
        var usage = new SubscriptionUsageAggregate { MonthlyRequests = monthly, DailyCalls = daily };
        var account = new AppAccountRecord { AppId = "demo", PlanId = "gold", PooledQuota = true };
        Assert.Equal(expected, AppPoolUsage.Build(account, limits, usage, true).Status);
    }

    [Fact]
    public void UnlimitedPlansReportNoFractionAndOk()
    {
        var usage = AppPoolUsage.Build(new AppAccountRecord { AppId = "demo", PlanId = "enterprise" }, new SubscriptionUsagePolicy(),
            new SubscriptionUsageAggregate { MonthlyRequests = 1_000_000 }, true);
        Assert.Null(usage.MonthlyRequestsUsedFraction);
        Assert.Equal("ok", usage.Status);
        Assert.False(usage.PoolActive);
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Subscription;

/// <summary>The pooled-quota flag is a process-wide environment variable, so these tests must not run in parallel with each other.</summary>
[CollectionDefinition("PooledFlag", DisableParallelization = true)]
public sealed class PooledFlagCollection { }

[Collection("PooledFlag")]
public sealed class PooledAppQuotaTests : IDisposable
{
    private readonly string _original = Environment.GetEnvironmentVariable(AppAccountRules.PooledFlagName);
    public void Dispose() => Environment.SetEnvironmentVariable(AppAccountRules.PooledFlagName, _original);
    private static void Flag(bool on) => Environment.SetEnvironmentVariable(AppAccountRules.PooledFlagName, on ? "true" : null);

    private static AppAccountRecord Pooled(string plan = "gold", string perUser = null) =>
        new() { AppId = "demo-app", PlanId = plan, Status = "active", PooledQuota = true, PerUserPlanId = perUser };

    // ---- rules ---------------------------------------------------------------------------------------------------

    [Fact]
    public void TheFlagIsOffByDefault() { Flag(false); Assert.False(AppAccountRules.PooledQuotasEnabled); }

    [Theory]
    [InlineData("free")] [InlineData("silver")] [InlineData("gold")]
    public void AFinitePlanIsRefusedWithoutAPool(string plan)
    {
        Flag(true);
        Assert.Throws<ArgumentException>(() => AppAccountRules.ValidateAppPlan(plan, pooled: false));
    }

    [Fact]
    public void AFinitePooledPlanIsRefusedWhileTheFlagIsOff()
    {
        Flag(false);
        Assert.Throws<ArgumentException>(() => AppAccountRules.ValidateAppPlan("gold", pooled: true));
    }

    [Fact]
    public void AFinitePooledPlanIsAcceptedWhenTheFlagIsOn()
    {
        Flag(true);
        AppAccountRules.ValidateAppPlan("gold", pooled: true);
    }

    [Fact]
    public void EnterpriseStaysValidWithAndWithoutPoolAndFlag()
    {
        Flag(false); AppAccountRules.ValidateAppPlan("enterprise"); AppAccountRules.ValidateAppPlan("enterprise", true);
        Flag(true); AppAccountRules.ValidateAppPlan("enterprise"); AppAccountRules.ValidateAppPlan("enterprise", true);
    }

    [Fact]
    public void ThePoolAppliesOnlyWhenPooledFiniteAndFlagOn()
    {
        Flag(true);
        Assert.True(AppAccountRules.UsesPool(Pooled("gold")));
        Assert.False(AppAccountRules.UsesPool(Pooled("enterprise")));
        Assert.False(AppAccountRules.UsesPool(new AppAccountRecord { PlanId = "gold", PooledQuota = false }));
        Assert.False(AppAccountRules.UsesPool(null));
        Flag(false);
        Assert.False(AppAccountRules.UsesPool(Pooled("gold")));
    }

    [Fact]
    public void ThePerUserPlanDefaultsToSilverAndIsNeverEnterprise()
    {
        Assert.Equal("silver", AppAccountRules.PerUserPlan(Pooled("gold")));
        Assert.Equal("bronze", AppAccountRules.PerUserPlan(Pooled("gold", "bronze")));
        Assert.Throws<ArgumentException>(() => AppAccountRules.ValidatePerUserPlan("enterprise"));
        Assert.Throws<ArgumentException>(() => AppAccountRules.ValidatePerUserPlan("platinum"));
        AppAccountRules.ValidatePerUserPlan(null);
    }

    [Fact]
    public void ThePoolSubjectCannotCollideWithAnAvatarId() =>
        Assert.False(Guid.TryParse(MongoSubscriptionUsageRepository.PoolSubject("demo-app"), out _));

    // ---- what the service stamps on the usage event ----------------------------------------------------------------

    private static async Task<SubscriptionUsageEvent> Authorize(AppAccountRecord app)
    {
        SubscriptionUsageEvent captured = null;
        var repository = new Mock<ISubscriptionUsageRepository>();
        repository.Setup(x => x.AuthorizeAsync(It.IsAny<SubscriptionUsageEvent>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Callback<SubscriptionUsageEvent, int, CancellationToken>((e, _, _) => captured = e)
            .ThrowsAsync(new SubscriptionUsageLimitException("STOP", "stop"));
        var service = new SubscriptionService(NullLogger<SubscriptionService>.Instance, repository.Object, new Mock<ISubscriptionBillingRepository>().Object);
        var request = new UsageAuthorizationRequest
        {
            OperationId = Guid.NewGuid().ToString("D"), ConsumingService = "WEB4", Endpoint = "POST /x",
            MeterCategory = "api.request", RequestedUnits = 1, RequestFingerprint = new string('a', 64)
        };
        await service.AuthorizeUsageAsync(Guid.NewGuid().ToString("D"), 0, request, app, CancellationToken.None);
        return captured;
    }

    [Fact]
    public async Task ARequestWithoutAnAppIsUnchanged()
    {
        Flag(true);
        var e = await Authorize(null);
        Assert.Null(e.AppId); Assert.Null(e.AppPlanId); Assert.Null(e.PooledAppId);
    }

    [Fact]
    public async Task AnEnterpriseAppKeepsItsPerUserPlanAndHasNoPool()
    {
        Flag(true);
        var e = await Authorize(new AppAccountRecord { AppId = "piper", PlanId = "enterprise", Status = "active" });
        Assert.Equal("enterprise", e.AppPlanId); Assert.Null(e.PooledAppId);
    }

    [Fact]
    public async Task APooledAppGivesUsersTheirPerUserFloorAndStampsThePool()
    {
        Flag(true);
        var e = await Authorize(Pooled("gold", "bronze"));
        Assert.Equal("bronze", e.AppPlanId);
        Assert.Equal("demo-app", e.PooledAppId); Assert.Equal("gold", e.PooledAppPlanId);
    }

    [Fact]
    public async Task APooledAppBehavesLikeAPerUserAppWhileTheFlagIsOff()
    {
        Flag(false);
        var e = await Authorize(Pooled("gold"));
        Assert.Null(e.PooledAppId);
    }

    // ---- reporting ------------------------------------------------------------------------------------------------

    [Fact]
    public void UsageReportShowsFractionsOfThePool()
    {
        var limits = new SubscriptionUsagePolicy { MonthlyRequestLimit = 1000, DailyCallLimit = 100 };
        var usage = new SubscriptionUsageAggregate { MonthlyRequests = 800, DailyCalls = 25 };
        var r = AppPoolUsage.Build(Pooled("gold"), limits, usage, true);
        Assert.True(r.PoolActive);
        Assert.Equal(0.8, r.MonthlyRequestsUsedFraction);
        Assert.Equal(0.25, r.DailyCallsUsedFraction);
    }

    [Fact]
    public void UsageReportMarksAnInactivePoolAndHandlesUnlimited()
    {
        var limits = new SubscriptionUsagePolicy { MonthlyRequestLimit = -1, DailyCallLimit = 0 };
        var r = AppPoolUsage.Build(Pooled("gold"), limits, new SubscriptionUsageAggregate(), false);
        Assert.False(r.PoolActive);
        Assert.Null(r.MonthlyRequestsUsedFraction); Assert.Null(r.DailyCallsUsedFraction);
    }
}

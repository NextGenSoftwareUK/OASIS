using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlFederationCursorHostTests
{
    [Theory]
    [InlineData("")]
    [InlineData("connection-string-is-not-a-digest")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void DeploymentEnrollmentRequiresAnExplicitCanonicalDigest(string identity)
        => Assert.Throws<ArgumentException>(() => new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), identity));

    [Fact]
    public void MalformedIntentDoesNotProduceBindingOrExecuteAdapter()
    {
        var host = Host();
        var adapter = new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        var result = host.BindIntent(new CqlFederationV1Request(), null!, new[] { adapter.Object });
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        adapter.VerifyNoOtherCalls();
    }

    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly ProviderType Mongo = ProviderType.MongoDBOASIS;
    private sealed class Clock : TimeProvider
    {
        internal DateTimeOffset Now = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static CqlFederationCursorHost Host(Clock? clock = null)
        => new(new EphemeralDataProtectionProvider(), new string('a', 64), clock);
    private static CqlFederationCursorState State(CqlFederationCursorHost host, Clock clock, Guid key)
        => new(Actor, new string('c', 64), host.Deployment, Guid.NewGuid(), key, clock.Now,
            clock.Now.AddMinutes(5), 1, new[] { Mongo }, 9999, new[] { 999 },
            1000, 1000, 1000, 63, 4000);

    [Theory]
    [InlineData("valid", true)]
    [InlineData("header-other-key", false)]
    [InlineData("admitted-other-key", false)]
    [InlineData("admitted-other-owner", false)]
    [InlineData("admitted-other-service", false)]
    [InlineData("uppercase-header", false)]
    [InlineData("multiple-headers", false)]
    [InlineData("no-handle", false)]
    [InlineData("state-other-owner", false)]
    public void ExactHeaderMustBelongToCapturedDurableAdmission(string mode, bool expected)
    {
        var clock = new Clock();
        var host = Host(clock);
        var key = Guid.Parse("abcdef01-abcd-abcd-abcd-abcdef012345");
        var state = State(host, clock, key);
        var http = new DefaultHttpContext();
        http.Request.Headers["Idempotency-Key"] = mode switch
        {
            "uppercase-header" => key.ToString("D").ToUpperInvariant(),
            "header-other-key" => Guid.NewGuid().ToString("D"),
            _ => key.ToString("D")
        };
        if (mode == "multiple-headers") http.Request.Headers.Append("Idempotency-Key", key.ToString("D"));
        var owner = mode == "admitted-other-owner" ? Guid.NewGuid() : Actor;
        var service = mode == "admitted-other-service" ? "WEB6" : "WEB4";
        var admittedKey = mode == "admitted-other-key" ? Guid.NewGuid() : key;
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var authority = new Mock<IUsageReservationAuthority>(MockBehavior.Strict);
        if (mode != "no-handle")
            http.Items[Web4UsageMeasurement.ExecutionKey] = new UsageExecutionHandle(new UsageOutboxOperation
            {
                Id = UsageCanonical.OperationId(service, owner.ToString("D"), admittedKey.ToString("D")),
                UserId = owner.ToString("D"), Service = service
            }, store.Object, new(), new(), authority.Object);
        var bound = host.BindRequest(http, mode == "state-other-owner" ? Guid.NewGuid() : Actor, state);
        Assert.Equal(!expected, bound.IsError);
        Assert.Equal(expected, bound.Result);
        store.VerifyNoOtherCalls();
        authority.VerifyNoOtherCalls();
    }

    [Fact]
    public void ProtectedHostBindingRejectsAnotherDeploymentAndKeyOwner()
    {
        var clock = new Clock();
        var keys = new EphemeralDataProtectionProvider();
        var first = new CqlFederationCursorHost(keys, new string('a', 64), clock);
        var otherDeployment = new CqlFederationCursorHost(keys, new string('b', 64), clock);
        var state = State(first, clock, Guid.NewGuid());
        var token = first.Codec.Protect(state).Result;
        Assert.False(first.Read(token, Actor).IsError);
        Assert.True(otherDeployment.Read(token, Actor).IsError);
        Assert.True(Host(clock).Read(token, Actor).IsError);
        clock.Now = state.Expires;
        Assert.True(first.Read(token, Actor).IsError);
        Assert.True(first.Restore(state, CosmicFederationOptions.Create().Result).IsError);
    }

    [Fact]
    public void RestorePreservesEveryRemainingDimensionAndRejectsInconsistentVector()
    {
        var clock = new Clock();
        var host = Host(clock);
        var state = State(host, clock, Guid.NewGuid());
        var remaining = host.Restore(state, CosmicFederationOptions.Create().Result);
        Assert.False(remaining.IsError, remaining.Message);
        Assert.Equal(state.InputRows, remaining.Result.InputRows);
        Assert.Equal(state.ProviderRows[0], remaining.Result.ProviderRows[Mongo]);
        Assert.Equal(state.InputBytes, remaining.Result.InputBytes);
        Assert.Equal(state.OutputBytes, remaining.Result.OutputBytes);
        Assert.Equal(state.Work, remaining.Result.Work);
        Assert.Equal(state.Attempts, remaining.Result.Attempts);
        Assert.Equal(state.DurationMilliseconds, remaining.Result.DurationMilliseconds);
        Assert.True(host.Restore(state, CosmicFederationOptions.Create(maximumRowsPerProvider: 900).Result).IsError);
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("metadata")]
    [InlineData("page")]
    [InlineData("policy")]
    [InlineData("deployment")]
    public void BindingIncludesOriginalHttpSemanticsAndFreshAuthoritativePolicy(string change)
    {
        var clock = new Clock();
        var host = Host(clock);
        var capability = new CosmicQueryCapabilities(CosmicQueryFeatures.Paging | CosmicQueryFeatures.Membership,
            new[] { new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal),
                new CosmicQueryFieldCapability("Id", typeof(Guid), CosmicQueryComparisonOperator.Equal) });
        var adapter = new Mock<ICosmicQueryStorageProvider>(MockBehavior.Strict);
        adapter.SetupGet(value => value.QueryProviderType).Returns(Mongo);
        CosmicFederationPlan<Holon> Plan(bool changed)
        {
            var access = CosmicQueryReadAccess.AuthoritativeRecords(Actor, Mongo,
                changed && change == "policy" ? new[] { Guid.NewGuid() } : Array.Empty<Guid>()).Result;
            return CosmicFederationPlanner.Plan(CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Mongo).Result),
                CosmicFederationOptions.Create(skip: changed && change == "page" ? 1 : 0).Result, Mongo,
                new[] { new CosmicQueryTarget(Mongo, capability) }, new[] { new CosmicFederationReadPolicy(Mongo, access) }).Result;
        }
        var request = new CqlFederationV1Request();
        var baseline = host.BindIntent(request, Plan(false), new[] { adapter.Object });
        Assert.False(baseline.IsError, baseline.Message);
        Assert.Equal(baseline.Result, host.BindIntent(request, Plan(false), new[] { adapter.Object }).Result);
        if (change == "scope") request.Scope = "AllEligible";
        if (change == "metadata") request.Source.IncludeMetadata = true;
        if (change == "deployment") host = new CqlFederationCursorHost(new EphemeralDataProtectionProvider(), new string('b', 64), clock);
        var changed = host.BindIntent(request, Plan(true), new[] { adapter.Object });
        Assert.False(changed.IsError, changed.Message);
        Assert.NotEqual(baseline.Result, changed.Result);
        adapter.Verify(value => value.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Fact]
    public async Task ExplicitlyUnmeasuredSdkAdmissionKeepsOptionalTariffsWithoutInventedReceipts()
    {
        var provider = ProviderType.SQLLiteDBOASIS;
        var source = new Source(provider);
        var native = source.Storage.As<ICosmicQueryProjectionCompositionStorageProvider>();
        var paths = CosmicProjectionCompositionExecutorTests.Columns;
        var profile = CosmicProjectionCompositionExecutorTests.Profile;
        native.SetupGet(value => value.ProjectionCapabilities).Returns(paths);
        native.SetupGet(value => value.CompositionCapabilities).Returns(profile);
        native.Setup(value => value.ExecuteCosmicProjectionCompositionAsync(It.IsAny<CosmicProjectionCompositionPlan<Holon, string>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<CosmicProjectionCompositionPacket>(CosmicProjectionCompositionExecutorTests.Packet()) { IsLoaded = true });
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result).Select(h => h.Name).Take(1).Result;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, provider,
            new[] { new CosmicProjectionCompositionTarget(provider, Capabilities, paths, profile) }).Result;
        var store = new Mock<IUsageOutboxStore>();
        var handle = new UsageExecutionHandle(new UsageOutboxOperation
        {
            Id = Guid.NewGuid().ToString("D"), UserId = Actor.ToString("D"), ExecutionOwner = "test",
            Policy = new UsageEndpointPolicy { RequiresProviderMeasurement = false }
        }, store.Object, new Web4UsageLedgerOptions(), new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        var policy = new PreparedStorageExecution(handle, "optional-cql", new Dictionary<ProviderType, string>(), new[] { provider });
        var result = await policy.RunAsync(() => source.CreateManager(source.Storage.Object)
            .ExecuteProjectionCompositionQueryAsync(plan, new[] { source.Adapter.Object }));
        Assert.False(result.IsError, result.Message);
        Assert.Equal("coffee", result.Result.Providers.Single().Rows.Single());
        store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public async Task EveryExistingCqlShapeRejectsMissingOrdinaryTariffBeforeAttempt(int shape)
    {
        var provider = ProviderType.SQLLiteDBOASIS;
        var source = new Source(provider);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result).Result;
        var admitted = Admission(provider, tariff: false);
        async Task<string> Execute()
        {
            if (shape == 0)
            {
                var plan = CosmicQueryPlanner.Plan(query, provider, new[] { new CosmicQueryTarget(provider, Capabilities) }).Result;
                return (await source.CreateManager(source.Storage.Object).ExecuteQueryAsync(plan, new[] { source.Adapter.Object })).ErrorCode;
            }
            if (shape == 1)
            {
                var capabilities = new CosmicScalarCapabilities(CosmicScalarOperation.LongCount);
                source.Storage.As<ICosmicQueryScalarStorageProvider>().SetupGet(value => value.ScalarCapabilities).Returns(capabilities);
                var plan = CosmicScalarPlanner.Plan(query.LongCount().Result, provider,
                    new[] { new CosmicScalarTarget(provider, Capabilities, capabilities) }).Result;
                return (await source.CreateManager(source.Storage.Object).ExecuteScalarQueryAsync(plan, new[] { source.Adapter.Object })).ErrorCode;
            }
            if (shape == 2)
            {
                var paths = CosmicProjectionCompositionExecutorTests.Columns;
                source.Storage.As<ICosmicQueryProjectionStorageProvider>().SetupGet(value => value.ProjectionCapabilities).Returns(paths);
                var plan = CosmicProjectionPlanner.Plan(query.Select(h => h.Name).Result, provider,
                    new[] { new CosmicProjectionTarget(provider, Capabilities, paths) }).Result;
                return (await source.CreateManager(source.Storage.Object).ExecuteProjectionQueryAsync(plan, new[] { source.Adapter.Object })).ErrorCode;
            }
            if (shape == 3)
            {
                var capabilities = CosmicJoinExecutorTests.Capabilities;
                source.Storage.As<ICosmicQueryJoinStorageProvider>().SetupGet(value => value.JoinCapabilities).Returns(capabilities);
                var selected = query.Join(query, left => left.Name, right => right.Name).Select((left, right) => left.Name).Result;
                var plan = CosmicJoinPlanner.Plan(selected, provider, new[] { new CosmicJoinTarget(provider, Capabilities, capabilities) }).Result;
                return (await source.CreateManager(source.Storage.Object).ExecuteJoinQueryAsync(plan, new[] { source.Adapter.Object })).ErrorCode;
            }
            var grouped = new CosmicGroupedCapabilities(CosmicProjectionCompositionExecutorTests.Columns, new CosmicScalarCapabilities());
            source.Storage.As<ICosmicQueryGroupedStorageProvider>().SetupGet(value => value.GroupedCapabilities).Returns(grouped);
            var selection = query.GroupBy(h => h.Name).Select(group => group.Key).Result;
            var groupedPlan = CosmicGroupedPlanner.Plan(selection, provider,
                new[] { new CosmicGroupedTarget(provider, Capabilities, grouped) }).Result;
            return (await source.CreateManager(source.Storage.Object).ExecuteGroupedQueryAsync(groupedPlan, new[] { source.Adapter.Object })).ErrorCode;
        }
        Assert.Equal(PreparedStorageExecution.RejectedCode, await admitted.Policy.RunAsync(Execute));
        Assert.Empty(admitted.Receipts); Assert.Null(source.Observed);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public async Task CompositionDenialCannotBeginAnInheritedAttempt(int denial)
    {
        var provider = ProviderType.SQLLiteDBOASIS;
        var source = new Source(provider);
        var native = source.Storage.As<ICosmicQueryProjectionCompositionStorageProvider>();
        var paths = CosmicProjectionCompositionExecutorTests.Columns;
        var profile = CosmicProjectionCompositionExecutorTests.Profile;
        native.SetupGet(value => value.ProjectionCapabilities).Returns(paths);
        native.SetupGet(value => value.CompositionCapabilities)
            .Returns(denial == 0 ? CosmicProjectionCompositionExecutorTests.Denied : profile);
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result)
            .Select(h => h.Name).Take(1).Result;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, provider,
            new[] { new CosmicProjectionCompositionTarget(provider, Capabilities, paths, profile) }).Result;
        var admitted = Admission(provider, reservedActor: denial == 1 ? Guid.NewGuid() : null, tariff: denial != 2);
        var result = await admitted.Policy.RunAsync(() => source.CreateManager(source.Storage.Object)
            .ExecuteProjectionCompositionQueryAsync(plan, new[] { source.Adapter.Object }));
        Assert.True(result.IsError);
        Assert.Empty(admitted.Receipts);
        Assert.Null(source.Observed);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        native.Verify(value => value.ExecuteCosmicProjectionCompositionAsync(It.IsAny<CosmicProjectionCompositionPlan<Holon, string>>(),
            It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)] [InlineData(ProviderType.SQLLiteDBOASIS)] [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ComposedSdkQuerySharesOneInheritedDurableAttemptAndReceipt(ProviderType provider)
    {
        var source = new Source(provider); var calls = 0;
        var native = source.Storage.As<ICosmicQueryProjectionCompositionStorageProvider>();
        var paths = CosmicProjectionCompositionExecutorTests.Columns;
        var profile = CosmicProjectionCompositionExecutorTests.Profile;
        native.SetupGet(value => value.ProjectionCapabilities).Returns(paths);
        native.SetupGet(value => value.CompositionCapabilities).Returns(profile);
        native.Setup(value => value.ExecuteCosmicProjectionCompositionAsync(
            It.IsAny<CosmicProjectionCompositionPlan<Holon, string>>(), It.IsAny<CosmicProjectionCompositionExecutionLimits>(), It.IsAny<CancellationToken>()))
            .Returns(() => { calls++; return Task.FromResult(new OASISResult<CosmicProjectionCompositionPacket>(CosmicProjectionCompositionExecutorTests.Packet()) { IsLoaded = true }); });
        var query = CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Actor).Result).Select(h => h.Name).Take(1).Result;
        var plan = CosmicProjectionCompositionPlanner.Plan(query, provider,
            new[] { new CosmicProjectionCompositionTarget(provider, Capabilities, paths, profile) }).Result;
        var admitted = Admission(provider);
        var result = await admitted.Policy.RunAsync(() => source.CreateManager(source.Storage.Object)
            .ExecuteProjectionCompositionQueryAsync(plan, new[] { source.Adapter.Object }));
        Assert.False(result.IsError, result.Message); Assert.Equal(1, calls); Assert.Null(source.Observed);
        Assert.Equal("coffee", result.Result.Providers.Single().Rows.Single());
        var receipt = Assert.Single(admitted.Receipts); Assert.Equal(1, receipt.Units); Assert.Equal(0.002m, receipt.ActualCostUsd);
        Assert.Equal("CQL-service-query-attempt-tariff", receipt.CostSource);
        admitted.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

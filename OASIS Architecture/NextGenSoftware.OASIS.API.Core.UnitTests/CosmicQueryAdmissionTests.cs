using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicQueryAdmissionTests
{
    [Fact]
    public async Task AccessActorMustMatchTheAdmittedExecutionBeforeAnyProviderAttempt()
    {
        var store=new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var adapter=Adapter(ProviderType.MongoDBOASIS);
        var query=CosmicQuery<Holon>.Create().WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var capabilities=new CosmicQueryCapabilities(CosmicQueryFeatures.Or|CosmicQueryFeatures.And,
            new[]{new CosmicQueryFieldCapability("IsPublic",typeof(bool),new[]{CosmicQueryComparisonOperator.Equal}),
                new CosmicQueryFieldCapability("CreatedByAvatarId",typeof(Guid),new[]{CosmicQueryComparisonOperator.Equal})},
            Array.Empty<CosmicQueryMetadataCapability>());
        var planned=CosmicQueryPlanner.Plan(query,ProviderType.MongoDBOASIS,
            new[]{new CosmicQueryTarget(ProviderType.MongoDBOASIS,capabilities)});
        Assert.False(planned.IsError,planned.Message);
        var result=await Policy(store,ProviderType.MongoDBOASIS).RunAsync(()=>CosmicQueryExecutor.ExecuteAsync(
            planned.Result,new[]{adapter.Query.Object}));
        Assert.Equal("CQL_ACTOR_MISMATCH",result.ErrorCode);
        Assert.Null(result.Result);
        NeverRead(adapter.Query);
        store.VerifyNoOtherCalls();
    }

    private static CosmicQueryCapabilities Capabilities => new(CosmicQueryFeatures.None,
        new CosmicQueryFieldCapability("Name", typeof(string), CosmicQueryComparisonOperator.Equal));

    private static CosmicQueryPlan<Holon> Plan(params ProviderType[] providers)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(providers).Result);
        var plan = CosmicQueryPlanner.Plan(query, providers[0],
            providers.Select(p => new CosmicQueryTarget(p, Capabilities)).ToArray());
        Assert.False(plan.IsError, plan.Message);
        return plan.Result;
    }

    private static PreparedStorageExecution Policy(Mock<IUsageOutboxStore> store, params ProviderType[] allowed)
    {
        var operation = new UsageOutboxOperation
        {
            Id = "cql-admission-test", ExecutionOwner = "test-owner", Policy = new UsageEndpointPolicy()
        };
        var handle = new UsageExecutionHandle(operation, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        return new PreparedStorageExecution(handle, "cql-test", new Dictionary<ProviderType, string>(), allowed);
    }

    private static (Mock<IOASISStorageProvider> Storage, Mock<ICosmicQueryStorageProvider> Query) Adapter(
        ProviderType type, Func<OASISResult<IReadOnlyList<IHolon>>>? read = null)
    {
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(type));
        var query = storage.As<ICosmicQueryStorageProvider>();
        query.SetupGet(p => p.QueryProviderType).Returns(type);
        query.SetupGet(p => p.QueryCapabilities).Returns(Capabilities);
        query.Setup(p => p.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(read?.Invoke() ?? new OASISResult<IReadOnlyList<IHolon>>(Array.Empty<IHolon>())));
        return (storage, query);
    }

    private static void NeverRead(Mock<ICosmicQueryStorageProvider> query) =>
        query.Verify(p => p.ExecuteCosmicQueryAsync(It.IsAny<CosmicQueryPlan<Holon>>(),
            It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task DurableAttemptMustCompleteBeforeNativeRead(ProviderType type)
    {
        var events = new List<string>();
        var store = new Mock<IUsageOutboxStore>();
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store.Setup(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderAttempt attempt, CancellationToken token) =>
            {
                Assert.Equal(type.ToString(), attempt.Provider);
                Assert.Equal("cql-test.route.1.non-native", attempt.ClientRequestId);
                events.Add("attempt");
                return admitted.Task;
            });
        var adapter = Adapter(type, () =>
        {
            events.Add("read");
            return new OASISResult<IReadOnlyList<IHolon>>(new[] { new Holon { Id = Guid.NewGuid() } });
        });

        var pending = Policy(store, type).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }));

        Assert.Equal(new[] { "attempt" }, events);
        NeverRead(adapter.Query);
        admitted.SetResult();
        var result = await pending;

        Assert.False(result.IsError, result.Message);
        Assert.Equal(new[] { "attempt", "read" }, events);
        Assert.Equal(type, Assert.Single(result.Result.Providers).Provider);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task DeniedProviderCannotRead(ProviderType type)
    {
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var adapter = Adapter(type);
        var result = await Policy(store).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }));

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.Null(result.Result);
        NeverRead(adapter.Query);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RejectedDurableAttemptPreservesCauseAndDoesNotRead()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var cause = new UsageProtocolException("PROVIDER_ATTEMPT_BUDGET_EXCEEDED", "Budget exhausted.");
        var store = new Mock<IUsageOutboxStore>();
        store.Setup(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>())).ThrowsAsync(cause);
        var adapter = Adapter(type);

        var result = await Policy(store, type).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }));

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.Same(cause, result.Exception);
        Assert.Null(result.Result);
        NeverRead(adapter.Query);
    }

    [Fact]
    public async Task QueryIdentityCannotDisguiseActualStorageProvider()
    {
        var adapter = Adapter(ProviderType.MongoDBOASIS);
        adapter.Query.SetupGet(p => p.QueryProviderType).Returns(ProviderType.SQLLiteDBOASIS);
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);

        var result = await Policy(store, ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS)
            .RunAsync(() => CosmicQueryExecutor.ExecuteAsync(Plan(ProviderType.SQLLiteDBOASIS),
                new[] { adapter.Query.Object }));

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        NeverRead(adapter.Query);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task IdentityPreflightRejectsAllTargetsBeforeAnyRead()
    {
        var first = Adapter(ProviderType.MongoDBOASIS);
        var second = Adapter(ProviderType.Neo4jOASIS);
        second.Storage.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS));
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);

        var result = await Policy(store, ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS)
            .RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
                Plan(ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS), new[] { first.Query.Object, second.Query.Object }));

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        NeverRead(first.Query);
        NeverRead(second.Query);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExpiredInheritedScopeCannotReadOrBecomeUnmetered()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var adapter = Adapter(type);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<OASISResult<CosmicQueryExecution>>? detached = null;
        await Policy(new Mock<IUsageOutboxStore>(), type).RunAsync(() =>
        {
            detached = Task.Run(async () =>
            {
                await release.Task;
                return await CosmicQueryExecutor.ExecuteAsync(Plan(type), new[] { adapter.Query.Object });
            });
            return Task.FromResult(true);
        });
        release.SetResult();
        var result = await detached!;

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.IsType<UsageProtocolException>(result.Exception);
        NeverRead(adapter.Query);
    }

    [Fact]
    public async Task PureQueryAdapterCannotBypassInheritedStorageAdmission()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var adapter = new Mock<ICosmicQueryStorageProvider>();
        adapter.SetupGet(p => p.QueryProviderType).Returns(type);
        adapter.SetupGet(p => p.QueryCapabilities).Returns(Capabilities);
        var result = await Policy(new Mock<IUsageOutboxStore>(), type).RunAsync(() =>
            CosmicQueryExecutor.ExecuteAsync(Plan(type), new[] { adapter.Object }));

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        NeverRead(adapter);
    }

    [Fact]
    public async Task CancellationWhilePersistingAttemptStopsTheNativeRead()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var persisted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IUsageOutboxStore>();
        store.Setup(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>())).Returns(persisted.Task);
        var adapter = Adapter(type);
        using var cancellation = new CancellationTokenSource();
        var pending = Policy(store, type).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }, cancellationToken: cancellation.Token));

        cancellation.Cancel();
        persisted.SetResult();
        var result = await pending;

        Assert.Equal("CQL_QUERY_CANCELLED", result.ErrorCode);
        Assert.Null(result.Result);
        NeverRead(adapter.Query);
    }

    [Fact]
    public async Task ScopeEndingDuringAttemptPersistenceCannotAuthorizeALaterRead()
    {
        var type = ProviderType.SQLLiteDBOASIS;
        var persisted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IUsageOutboxStore>();
        store.Setup(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>())).Returns(persisted.Task);
        var adapter = Adapter(type);
        Task<OASISResult<CosmicQueryExecution>>? detached = null;
        await Policy(store, type).RunAsync(() =>
        {
            detached = CosmicQueryExecutor.ExecuteAsync(Plan(type), new[] { adapter.Query.Object });
            return Task.FromResult(true);
        });

        persisted.SetResult();
        var result = await detached!;

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.Null(result.Result);
        NeverRead(adapter.Query);
    }

    [Fact]
    public async Task NativePreparationCannotBeDowngradedToOrdinaryAdmission()
    {
        var type = ProviderType.MongoDBOASIS;
        var adapter = Adapter(type);
        adapter.Storage.As<IPreparedNativeStorageProvider>();
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var result = await Policy(store, type).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }));

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        NeverRead(adapter.Query);
        store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AdmittedNativeErrorKeepsItsCodeAndCause()
    {
        var type = ProviderType.Neo4jOASIS;
        var cause = new InvalidOperationException("Controlled native failure.");
        var adapter = Adapter(type, () => new OASISResult<IReadOnlyList<IHolon>>
        {
            IsError = true, ErrorCode = "CQL_NATIVE_TEST_FAILURE", Exception = cause
        });
        var store = new Mock<IUsageOutboxStore>();
        var result = await Policy(store, type).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }));

        Assert.Equal("CQL_NATIVE_TEST_FAILURE", result.ErrorCode);
        Assert.Same(cause, result.Exception);
        Assert.Null(result.Result);
        store.Verify(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProviderIdentityChangingDuringAttemptPersistenceCannotRead()
    {
        var type = ProviderType.MongoDBOASIS;
        var persisted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IUsageOutboxStore>();
        store.Setup(s => s.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>())).Returns(persisted.Task);
        var adapter = Adapter(type);
        var pending = Policy(store, type).RunAsync(() => CosmicQueryExecutor.ExecuteAsync(
            Plan(type), new[] { adapter.Query.Object }));

        adapter.Storage.SetupGet(p => p.ProviderType).Returns(new EnumValue<ProviderType>(ProviderType.Neo4jOASIS));
        persisted.SetResult();
        var result = await pending;

        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        NeverRead(adapter.Query);
    }
}

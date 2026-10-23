using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicProjectionAdmissionTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicProjectionPlan<Holon, string> Plan(ProviderType provider)
        => CosmicProjectionPlanner.Plan(CosmicQuery<Holon>.Create().Select(holon => holon.Name).Result, provider,
            new[] { new CosmicProjectionTarget(provider, Rows, new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>())) }).Result;

    private static PreparedStorageExecution Policy(Mock<IUsageOutboxStore> store, ProviderType provider, bool allowed)
    {
        var operation = new UsageOutboxOperation { Id = "projection-admission", ExecutionOwner = "owner", Policy = new UsageEndpointPolicy() };
        var handle = new UsageExecutionHandle(operation, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        return new PreparedStorageExecution(handle, "projection-admission", new Dictionary<ProviderType, string>(),
            allowed ? new[] { provider } : Array.Empty<ProviderType>(),
            allowed ? new[] { new StorageQueryTariff(provider, 0.001m, "projection-test-v1") } : Array.Empty<StorageQueryTariff>());
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ProjectionUsesTheDurableAttemptBeforeNativeWorkAndTheSameCompletedQueryReceipt(ProviderType provider)
    {
        var events = new List<string>();
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderAttempt attempt, CancellationToken token) =>
            {
                Assert.Equal(provider.ToString(), attempt.Provider);
                Assert.Equal("projection-admission.route.1.non-native", attempt.ClientRequestId);
                events.Add("attempt");
                return admitted.Task;
            });
        store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderReceipt receipt, CancellationToken token) =>
            {
                Assert.Equal(provider.ToString(), receipt.Provider);
                Assert.Equal("projection-admission.route.1.non-native", receipt.ProviderRequestId);
                Assert.Equal(1, receipt.Units); Assert.Equal(0.001m, receipt.ActualCostUsd);
                Assert.Equal("CQL-service-query-attempt-tariff", receipt.CostSource);
                events.Add("receipt");
                return Task.CompletedTask;
            });
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var projection = storage.As<ICosmicQueryProjectionStorageProvider>();
        projection.SetupGet(value => value.QueryProviderType).Returns(provider);
        projection.SetupGet(value => value.QueryCapabilities).Returns(Rows);
        projection.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>()));
        projection.Setup(value => value.ExecuteCosmicProjectionAsync(It.IsAny<CosmicProjectionPlan<Holon, string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => { events.Add("native"); return Task.FromResult(new OASISResult<IReadOnlyList<CosmicProjectionRow>>(new[] { new CosmicProjectionRow("coffee") }) { IsLoaded = true }); });

        var pending = Policy(store, provider, true).RunAsync(() => CosmicProjectionExecutor.ExecuteAsync(Plan(provider),
            new ICosmicQueryStorageProvider[] { projection.Object }));
        Assert.Equal(new[] { "attempt" }, events);
        admitted.SetResult();
        var result = await pending;
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal("coffee", Assert.Single(result.Result.Providers).Rows.Single());
        Assert.Equal(new[] { "attempt", "native", "receipt" }, events);
        projection.Verify(value => value.ExecuteCosmicProjectionAsync(It.IsAny<CosmicProjectionPlan<Holon, string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AProjectionCannotBypassAnInheritedProviderDenial()
    {
        var provider = ProviderType.MongoDBOASIS;
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var projection = storage.As<ICosmicQueryProjectionStorageProvider>();
        projection.SetupGet(value => value.QueryProviderType).Returns(provider);
        projection.SetupGet(value => value.QueryCapabilities).Returns(Rows);
        projection.SetupGet(value => value.ProjectionCapabilities).Returns(new CosmicProjectionCapabilities(new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>()));
        var result = await Policy(store, provider, false).RunAsync(() => CosmicProjectionExecutor.ExecuteAsync(Plan(provider),
            new ICosmicQueryStorageProvider[] { projection.Object }));
        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.Null(result.Result);
        projection.Verify(value => value.ExecuteCosmicProjectionAsync(It.IsAny<CosmicProjectionPlan<Holon, string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        store.VerifyNoOtherCalls();
    }
}

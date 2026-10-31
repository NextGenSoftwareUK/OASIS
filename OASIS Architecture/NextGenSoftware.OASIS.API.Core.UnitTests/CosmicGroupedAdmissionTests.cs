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

public class CosmicGroupedAdmissionTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicGroupedPlan<Holon, string, string> Plan(ProviderType provider)
        => CosmicGroupedPlanner.Plan(CosmicQuery<Holon>.Create().GroupBy(h => h.Name).Select(g => g.Key).Result, provider,
            new[] { new CosmicGroupedTarget(provider, Rows, Profile()) }).Result;
    private static CosmicGroupedCapabilities Profile() => new(new CosmicProjectionCapabilities(
        new[] { new CosmicProjectionFieldCapability("Name", typeof(string)) }, Array.Empty<Type>()),
        new CosmicScalarCapabilities(CosmicScalarOperation.Count));

    private static PreparedStorageExecution Policy(Mock<IUsageOutboxStore> store, ProviderType provider, bool allowed)
    {
        var operation = new UsageOutboxOperation { Id = "grouped-admission", ExecutionOwner = "owner", Policy = new UsageEndpointPolicy() };
        var handle = new UsageExecutionHandle(operation, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        return new PreparedStorageExecution(handle, "grouped-admission", new Dictionary<ProviderType, string>(),
            allowed ? new[] { provider } : Array.Empty<ProviderType>(),
            allowed ? new[] { new StorageQueryTariff(provider, 0.001m, "grouped-test-v1") } : Array.Empty<StorageQueryTariff>());
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task GroupingUsesTheDurableAttemptBeforeNativeWorkAndTheSameCompletedQueryReceipt(ProviderType provider)
    {
        var events = new List<string>();
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderAttempt attempt, CancellationToken token) =>
            {
                Assert.Equal(provider.ToString(), attempt.Provider);
                Assert.Equal("grouped-admission.route.1.non-native", attempt.ClientRequestId);
                events.Add("attempt");
                return admitted.Task;
            });
        store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderReceipt receipt, CancellationToken token) =>
            {
                Assert.Equal(provider.ToString(), receipt.Provider);
                Assert.Equal("grouped-admission.route.1.non-native", receipt.ProviderRequestId);
                Assert.Equal(1, receipt.Units); Assert.Equal(0.001m, receipt.ActualCostUsd);
                Assert.Equal("CQL-service-query-attempt-tariff", receipt.CostSource);
                events.Add("receipt");
                return Task.CompletedTask;
            });
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var projection = storage.As<ICosmicQueryGroupedStorageProvider>();
        projection.SetupGet(value => value.QueryProviderType).Returns(provider);
        projection.SetupGet(value => value.QueryCapabilities).Returns(Rows);
        projection.SetupGet(value => value.GroupedCapabilities).Returns(Profile());
        projection.Setup(value => value.ExecuteCosmicGroupedAsync(It.IsAny<CosmicGroupedPlan<Holon, string, string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .Returns(() => { events.Add("native"); return Task.FromResult(new OASISResult<IReadOnlyList<CosmicGroupedRow>>(new[] { new CosmicGroupedRow(1, new object[] { "coffee" }, "coffee") }) { IsLoaded = true }); });

        var pending = Policy(store, provider, true).RunAsync(() => CosmicGroupedExecutor.ExecuteAsync(Plan(provider),
            new ICosmicQueryStorageProvider[] { projection.Object }));
        Assert.Equal(new[] { "attempt" }, events);
        admitted.SetResult();
        var result = await pending;
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal("coffee", Assert.Single(result.Result.Providers).Rows.Single());
        Assert.Equal(new[] { "attempt", "native", "receipt" }, events);
        projection.Verify(value => value.ExecuteCosmicGroupedAsync(It.IsAny<CosmicGroupedPlan<Holon, string, string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GroupingCannotBypassAnInheritedProviderDenial()
    {
        var provider = ProviderType.MongoDBOASIS;
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var projection = storage.As<ICosmicQueryGroupedStorageProvider>();
        projection.SetupGet(value => value.QueryProviderType).Returns(provider);
        projection.SetupGet(value => value.QueryCapabilities).Returns(Rows);
        projection.SetupGet(value => value.GroupedCapabilities).Returns(Profile());
        var result = await Policy(store, provider, false).RunAsync(() => CosmicGroupedExecutor.ExecuteAsync(Plan(provider),
            new ICosmicQueryStorageProvider[] { projection.Object }));
        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.Null(result.Result);
        projection.Verify(value => value.ExecuteCosmicGroupedAsync(It.IsAny<CosmicGroupedPlan<Holon, string, string>>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        store.VerifyNoOtherCalls();
    }
}

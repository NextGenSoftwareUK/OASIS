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

public class CosmicScalarAdmissionTests
{
    private static CosmicQueryCapabilities Rows => new(CosmicQueryFeatures.None, Array.Empty<CosmicQueryFieldCapability>());
    private static CosmicScalarPlan<Holon, long> Plan(ProviderType provider)
        => CosmicScalarPlanner.Plan(CosmicQuery<Holon>.Create().LongCount().Result, provider,
            new[] { new CosmicScalarTarget(provider, Rows, new CosmicScalarCapabilities(CosmicScalarOperation.LongCount)) }).Result;

    private static PreparedStorageExecution Policy(Mock<IUsageOutboxStore> store, ProviderType provider, bool allowed)
    {
        var operation = new UsageOutboxOperation { Id = "scalar-admission", ExecutionOwner = "owner", Policy = new UsageEndpointPolicy() };
        var handle = new UsageExecutionHandle(operation, store.Object, new Web4UsageLedgerOptions(),
            new SubscriptionAuthorizationResult(), Mock.Of<IUsageReservationAuthority>());
        return new PreparedStorageExecution(handle, "scalar-admission", new Dictionary<ProviderType, string>(),
            allowed ? new[] { provider } : Array.Empty<ProviderType>(),
            allowed ? new[] { new StorageQueryTariff(provider, 0.001m, "scalar-test-v1") } : Array.Empty<StorageQueryTariff>());
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS)]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.Neo4jOASIS)]
    public async Task ScalarUsesTheDurableAttemptBeforeNativeWorkAndTheSameCompletedQueryReceipt(ProviderType provider)
    {
        var events = new List<string>();
        var admitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        store.Setup(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderAttempt attempt, CancellationToken token) =>
            {
                Assert.Equal(provider.ToString(), attempt.Provider);
                Assert.Equal("scalar-admission.route.1.non-native", attempt.ClientRequestId);
                events.Add("attempt");
                return admitted.Task;
            });
        store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Returns((string id, string owner, UsageProviderReceipt receipt, CancellationToken token) =>
            {
                Assert.Equal(provider.ToString(), receipt.Provider);
                Assert.Equal("scalar-admission.route.1.non-native", receipt.ProviderRequestId);
                Assert.Equal(1, receipt.Units); Assert.Equal(0.001m, receipt.ActualCostUsd);
                Assert.Equal("CQL-service-query-attempt-tariff", receipt.CostSource);
                events.Add("receipt");
                return Task.CompletedTask;
            });
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var scalar = storage.As<ICosmicQueryScalarStorageProvider>();
        scalar.SetupGet(value => value.QueryProviderType).Returns(provider);
        scalar.SetupGet(value => value.QueryCapabilities).Returns(Rows);
        scalar.SetupGet(value => value.ScalarCapabilities).Returns(new CosmicScalarCapabilities(CosmicScalarOperation.LongCount));
        scalar.Setup(value => value.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, long>>(), It.IsAny<CancellationToken>()))
            .Returns(() => { events.Add("native"); return Task.FromResult(new OASISResult<long>(10001) { IsLoaded = true }); });

        var pending = Policy(store, provider, true).RunAsync(() => CosmicScalarExecutor.ExecuteAsync(Plan(provider),
            new ICosmicQueryStorageProvider[] { scalar.Object }));
        Assert.Equal(new[] { "attempt" }, events);
        admitted.SetResult();
        var result = await pending;
        Assert.False(result.IsError, result.Message + result.Exception);
        Assert.Equal(10001L, Assert.Single(result.Result.Providers).Value);
        Assert.Equal(new[] { "attempt", "native", "receipt" }, events);
        scalar.Verify(value => value.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, long>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AScalarCannotBypassAnInheritedProviderDenial()
    {
        var provider = ProviderType.MongoDBOASIS;
        var store = new Mock<IUsageOutboxStore>(MockBehavior.Strict);
        var storage = new Mock<IOASISStorageProvider>();
        storage.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var scalar = storage.As<ICosmicQueryScalarStorageProvider>();
        scalar.SetupGet(value => value.QueryProviderType).Returns(provider);
        scalar.SetupGet(value => value.QueryCapabilities).Returns(Rows);
        scalar.SetupGet(value => value.ScalarCapabilities).Returns(new CosmicScalarCapabilities(CosmicScalarOperation.LongCount));
        var result = await Policy(store, provider, false).RunAsync(() => CosmicScalarExecutor.ExecuteAsync(Plan(provider),
            new ICosmicQueryStorageProvider[] { scalar.Object }));
        Assert.Equal(PreparedStorageExecution.RejectedCode, result.ErrorCode);
        Assert.Null(result.Result);
        scalar.Verify(value => value.ExecuteCosmicScalarAsync(It.IsAny<CosmicScalarPlan<Holon, long>>(), It.IsAny<CancellationToken>()), Times.Never);
        store.VerifyNoOtherCalls();
    }
}

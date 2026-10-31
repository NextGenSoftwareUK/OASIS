using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class StorageAttemptTariffTests
{
    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown-operation")]
    [InlineData("negative-price")]
    [InlineData("unsupported-provider")]
    [InlineData("disallowed-provider")]
    [InlineData("missing-catalogue")]
    [InlineData("oversized")]
    public void InvalidTariffsCannotEnterTheServerPolicy(string invalid)
    {
        var row = new StorageAttemptTariff(ProviderType.SQLLiteDBOASIS, "LoadHolon", 0.001m, "storage-v1");
        var rows = invalid switch
        {
            "duplicate" => new[] { row, row },
            "unknown-operation" => new[] { row with { Operation = "DeleteEverything" } },
            "negative-price" => new[] { row with { CostUsd = -1 } },
            "unsupported-provider" => new[] { row with { Provider = ProviderType.DynamoDBOASIS } },
            "disallowed-provider" => new[] { row with { Provider = ProviderType.MongoDBOASIS } },
            "missing-catalogue" => new[] { row with { CatalogueVersion = "" } },
            _ => Enumerable.Repeat(row, 10).ToArray()
        };

        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(rows,
            invalid == "unsupported-provider" ? new[] { ProviderType.DynamoDBOASIS } : new[] { ProviderType.SQLLiteDBOASIS }));
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("missing-read")]
    [InlineData("catalogue")]
    [InlineData("budget")]
    public void ConfiguredWritesRequireACompleteAffordableOperationProfile(string profile)
    {
        var rows = StorageAttemptTariff.SupportedOperations.Select(operation =>
            new StorageAttemptTariff(ProviderType.SQLLiteDBOASIS, operation, 0.003m, "storage-v1")).ToArray();
        if (profile == "missing-read") rows = rows.Where(row => row.Operation != "LoadHolon").ToArray();
        if (profile == "catalogue") rows[0] = rows[0] with { CatalogueVersion = "other-v1" };
        var deployment = new StorageAdmissionDeployment
        {
            OrdinaryProviders = new[] { ProviderType.SQLLiteDBOASIS },
            NativeProviders = Array.Empty<NativeStorageAdmissionBinding>(), StorageTariffs = rows,
            Routes = new[]
            {
                new StorageAdmissionRoute("POST", "/api/data/save-holon", new UsageEndpointPolicy
                {
                    MeterCategory = "fixture-write", PricingCatalogueVersion = "storage-v1",
                    RequiresProviderMeasurement = true, ReservedUnits = 3, MaximumProviderAttempts = 3,
                    MaximumNativeFeeCostUsd = 0, EstimatedCostUsd = profile == "budget" ? 0.001m : 0.02m
                })
            }
        };
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
        };
        jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PREPARED_STORAGE_ADMISSION_JSON"] = System.Text.Json.JsonSerializer.Serialize(deployment, jsonOptions)
        }).Build();
        var services = new ServiceCollection();

        if (profile == "valid")
        {
            var select = services.AddConfiguredStorageAdmission(configuration, true, _ => null!);
            var context = new DefaultHttpContext();
            context.Request.Method = "POST";
            context.Request.Path = "/api/data/save-holon";
            Assert.NotNull(select(context));
        }
        else Assert.Throws<InvalidOperationException>(() =>
            services.AddConfiguredStorageAdmission(configuration, true, _ => null!));
    }

    [Fact]
    public void ExistingPublicConstructorSignaturesRemainAvailable()
    {
        Assert.NotNull(typeof(PreparedStorageExecution).GetConstructor(new[]
        {
            typeof(UsageExecutionHandle), typeof(string), typeof(IDictionary<ProviderType, string>),
            typeof(IEnumerable<ProviderType>), typeof(IEnumerable<StorageQueryTariff>)
        }));
        Assert.NotNull(typeof(PricedStorageAdmissionFactory).GetConstructor(new[]
        {
            typeof(NativeFeePriceQuoteFactory), typeof(IEnumerable<NativeStorageAdmissionBinding>),
            typeof(IEnumerable<ProviderType>), typeof(IEnumerable<StorageQueryTariff>)
        }));
    }

    [Fact]
    public async Task FactoryCapturesItsTariffsBeforeCallerArrayReplacement()
    {
        var fixture = Create(ProviderType.SQLLiteDBOASIS);
        var rows = new[] { new StorageAttemptTariff(ProviderType.SQLLiteDBOASIS, "SaveHolon", 0.003m, "storage-v1") };
        var factory = new PricedStorageAdmissionFactory(null, Array.Empty<NativeStorageAdmissionBinding>(),
            new[] { ProviderType.SQLLiteDBOASIS }, null, rows);
        rows[0] = rows[0] with { CostUsd = 99 };
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/data/save-holon";
        var policy = await factory.CreateAsync(context, Handle(fixture.Store), CancellationToken.None);

        var result = await policy.ExecuteAsync(fixture.Provider.Object,
            () => Task.FromResult(new OASISResult<bool>(true)), "SaveHolon");

        Assert.False(result.IsError, result.Message);
        Assert.Equal(0.003m, Assert.Single(fixture.Receipts).ActualCostUsd);
    }

    [Theory]
    [InlineData(ProviderType.MongoDBOASIS, "LoadHolon")]
    [InlineData(ProviderType.MongoDBOASIS, "SaveHolon")]
    [InlineData(ProviderType.MongoDBOASIS, "SaveHolons")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "LoadHolon")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "SaveHolon")]
    [InlineData(ProviderType.SQLLiteDBOASIS, "SaveHolons")]
    [InlineData(ProviderType.Neo4jOASIS, "LoadHolon")]
    [InlineData(ProviderType.Neo4jOASIS, "SaveHolon")]
    [InlineData(ProviderType.Neo4jOASIS, "SaveHolons")]
    public async Task CompletedStorageInvocationUsesItsExactOperationPrice(ProviderType provider, string operation)
    {
        var fixture = Create(provider);
        var policy = Policy(fixture.Store, provider, new[]
        {
            new StorageAttemptTariff(provider, operation, 0.003m, "storage-v1")
        });

        var result = await policy.ExecuteAsync(fixture.Provider.Object,
            () => Task.FromResult(new OASISResult<bool>(true)), operation);

        Assert.False(result.IsError, result.Message);
        var receipt = Assert.Single(fixture.Receipts);
        Assert.Equal(0.003m, receipt.ActualCostUsd);
        Assert.Equal(provider.ToString(), receipt.Provider);
        Assert.Equal("storage-v1", receipt.PricingCatalogueVersion);
        Assert.Equal("COSMIC-storage-service-attempt-tariff", receipt.CostSource);
        Assert.Equal("storage-tariff.route.1.non-native", receipt.ClientRequestId);
        Assert.Equal(receipt.ClientRequestId, receipt.ProviderRequestId);
        Assert.Null(receipt.NativeFeeValuations);
        Assert.Null(receipt.NativeResourceUsage);
    }

    [Fact]
    public async Task MissingStorageTariffRejectsBeforeAnyAttemptOrProviderInvocation()
    {
        var fixture = Create(ProviderType.SQLLiteDBOASIS);
        var policy = Policy(fixture.Store, ProviderType.SQLLiteDBOASIS, Array.Empty<StorageAttemptTariff>());
        var invoked = false;

        var result = await policy.ExecuteAsync(fixture.Provider.Object, () =>
        {
            invoked = true;
            return Task.FromResult(new OASISResult<bool>(true));
        }, "SaveHolon");

        Assert.True(result.IsError);
        Assert.False(invoked);
        Assert.Empty(fixture.Receipts);
        fixture.Store.Verify(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("POST", "/api/data/save-holon")]
    [InlineData("post", "/API/DATA/SAVE-HOLON/false/true/0/true/")]
    [InlineData("POST", "/api/data/save-holons")]
    public async Task QueryTariffCannotAuthorizeOrPriceAnOrdinarySave(string method, string path)
    {
        var fixture = Create(ProviderType.MongoDBOASIS);
        var factory = new PricedStorageAdmissionFactory(null,
            Array.Empty<NativeStorageAdmissionBinding>(), new[] { ProviderType.MongoDBOASIS },
            new[] { new StorageQueryTariff(ProviderType.MongoDBOASIS, 0.001m, "storage-v1") });
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        var policy = await factory.CreateAsync(context, Handle(fixture.Store), CancellationToken.None);
        var invoked = false;

        var result = await policy.ExecuteAsync(fixture.Provider.Object, () =>
        {
            invoked = true;
            return Task.FromResult(new OASISResult<bool>(true));
        }, "SaveHolon");

        Assert.True(result.IsError);
        Assert.False(invoked);
        Assert.Empty(fixture.Receipts);
    }

    [Fact]
    public async Task InvocationWaitsForDurableAttemptAndReceiptIsAwaitedBeforeReturn()
    {
        var fixture = Create(ProviderType.Neo4jOASIS);
        var attempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receipt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var receiptEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Store.Setup(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>())).Returns(attempt.Task);
        fixture.Store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Callback(() => receiptEntered.SetResult()).Returns(receipt.Task);
        var policy = Policy(fixture.Store, ProviderType.Neo4jOASIS, new[]
        {
            new StorageAttemptTariff(ProviderType.Neo4jOASIS, "SaveHolons", 0.003m, "storage-v1")
        });
        var invoked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var execution = policy.ExecuteAsync(fixture.Provider.Object, () =>
        {
            invoked.SetResult();
            return Task.FromResult(new OASISResult<bool>(true));
        }, "SaveHolons");

        Assert.False(invoked.Task.IsCompleted);
        attempt.SetResult();
        await invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.WhenAny(execution, receiptEntered.Task).WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(receiptEntered.Task.IsCompleted, "Completed invocations must enter durable receipt persistence.");
        Assert.False(execution.IsCompleted);
        receipt.SetResult();
        Assert.False((await execution).IsError);
    }

    [Fact]
    public async Task ThrownInvocationDoesNotInventCompletedStorageReceipt()
    {
        var fixture = Create(ProviderType.SQLLiteDBOASIS);
        var policy = Policy(fixture.Store, ProviderType.SQLLiteDBOASIS, new[]
        {
            new StorageAttemptTariff(ProviderType.SQLLiteDBOASIS, "SaveHolon", 0.003m, "storage-v1")
        });

        var result = await policy.ExecuteAsync<bool>(fixture.Provider.Object,
            () => throw new IOException("Controlled unknown outcome"), "SaveHolon");

        Assert.True(result.IsError);
        Assert.Empty(fixture.Receipts);
    }

    [Fact]
    public async Task CompletedProviderErrorStillRecordsTheExplicitServiceAttemptPrice()
    {
        var fixture = Create(ProviderType.MongoDBOASIS);
        var policy = Policy(fixture.Store, ProviderType.MongoDBOASIS, new[]
        {
            new StorageAttemptTariff(ProviderType.MongoDBOASIS, "LoadHolon", 0.001m, "storage-v1")
        });

        var result = await policy.ExecuteAsync(fixture.Provider.Object,
            () => Task.FromResult(new OASISResult<bool> { IsError = true, ErrorCode = "CONTROL_PROVIDER_ERROR" }),
            "LoadHolon");

        Assert.Equal("CONTROL_PROVIDER_ERROR", result.ErrorCode);
        Assert.Equal(0.001m, Assert.Single(fixture.Receipts).ActualCostUsd);
    }

    private static PreparedStorageExecution Policy(Mock<IUsageOutboxStore> store, ProviderType provider,
        IEnumerable<StorageAttemptTariff> tariffs) => new(Handle(store), "storage-tariff",
            new Dictionary<ProviderType, string>(), new[] { provider }, null, tariffs, true);

    private static UsageExecutionHandle Handle(Mock<IUsageOutboxStore> store) => new(
        new UsageOutboxOperation
        {
            Id = "storage-tariff", ExecutionOwner = "fixture", UserId = Guid.NewGuid().ToString("D"),
            Policy = new UsageEndpointPolicy
            {
                RequiresProviderMeasurement = true, MaximumProviderAttempts = 10,
                EstimatedCostUsd = 1, PricingCatalogueVersion = "storage-v1"
            }
        }, store.Object, new Web4UsageLedgerOptions(), new SubscriptionAuthorizationResult(),
        Mock.Of<IUsageReservationAuthority>());

    private static (Mock<IOASISStorageProvider> Provider, Mock<IUsageOutboxStore> Store,
        List<UsageProviderReceipt> Receipts) Create(ProviderType provider)
    {
        var target = new Mock<IOASISStorageProvider>();
        target.SetupGet(value => value.ProviderType).Returns(new EnumValue<ProviderType>(provider));
        var store = new Mock<IUsageOutboxStore>();
        var receipts = new List<UsageProviderReceipt>();
        store.Setup(value => value.BeginProviderAttemptAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<UsageProviderAttempt>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, UsageProviderReceipt, CancellationToken>((_, _, row, _) => receipts.Add(row))
            .Returns(Task.CompletedTask);
        return (target, store, receipts);
    }
}

public partial class CosmicTrackingTests
{
    [Fact]
    public async Task OwnedSaveBindsDistinctReadAndWriteTariffsThroughHyperDrive()
    {
        var fixture = Fixture(true, conditional: true);
        var actor = Guid.NewGuid();
        var source = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid() };
        NextGenSoftware.OASIS.API.Core.Holons.CosmicConcurrency.SetProviderRevisionId(
            source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(source.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new NextGenSoftware.OASIS.API.Core.Holons.Holon
            {
                Id = source.Id, CreatedByAvatarId = actor
            }) { IsLoaded = true });
        fixture.Provider.As<IConditionalHolonStorageProvider>()
            .Setup(provider => provider.ExecuteConditionalHolonWriteAsync(
                It.IsAny<ConditionalHolonWriteRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCode = NextGenSoftware.OASIS.API.Core.Interfaces.CosmicConcurrencyException.Code
            });
        var store = new Mock<IUsageOutboxStore>();
        var receipts = new List<UsageProviderReceipt>();
        store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, UsageProviderReceipt, CancellationToken>((_, _, row, _) => receipts.Add(row))
            .Returns(Task.CompletedTask);
        var handle = new UsageExecutionHandle(new UsageOutboxOperation
        {
            Id = "owned-priced-write", ExecutionOwner = "fixture", UserId = actor.ToString("D"),
            Policy = new UsageEndpointPolicy { EstimatedCostUsd = 1, MaximumProviderAttempts = 10 }
        }, store.Object, new Web4UsageLedgerOptions(), new SubscriptionAuthorizationResult(),
            Mock.Of<IUsageReservationAuthority>());
        var policy = new PreparedStorageExecution(handle, "owned-priced-write",
            new Dictionary<ProviderType, string>(), new[] { ProviderType.SQLLiteDBOASIS }, null, new[]
            {
                new StorageAttemptTariff(ProviderType.SQLLiteDBOASIS, "LoadHolon", 0.001m, "storage-v1"),
                new StorageAttemptTariff(ProviderType.SQLLiteDBOASIS, "SaveHolon", 0.003m, "storage-v1")
            }, true);

        var result = await policy.RunAsync(() => fixture.Manager.SaveOwnedHolonAsync(source, actor,
            new CosmicWriteOptions(Guid.NewGuid()), saveChildren: false));

        Assert.Equal(NextGenSoftware.OASIS.API.Core.Interfaces.CosmicConcurrencyException.Code, result.ErrorCode);
        Assert.Equal(new[] { 0.001m, 0.003m }, receipts.Select(row => row.ActualCostUsd));
        Assert.Equal(2, receipts.Select(row => row.ClientRequestId).Distinct().Count());
    }
}

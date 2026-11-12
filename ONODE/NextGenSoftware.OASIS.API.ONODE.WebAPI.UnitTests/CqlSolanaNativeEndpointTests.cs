using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class SolanaPublicEndpointFactAttribute : FactAttribute
{
    public SolanaPublicEndpointFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SOLANA_PUBLIC_ENDPOINT_NATIVE") != "1")
            Skip = "Explicit public mainnet endpoint gate; reported separately from controlled tests.";
    }
}

public class CqlSolanaNativeEndpointTests
{
    private sealed class Sources(SolanaOASIS source, OASISDNA dna, ProviderManager registry) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) =>
            requested == ProviderType.SolanaOASIS && source.IsProviderActivated
                ? source : throw new InvalidOperationException("The fixture admits only the activated Solana provider.");
        public HolonManager CreateManager(IOASISStorageProvider provider) => CqlV1Providers.CreateQueryManager(dna, registry);
    }

    [SolanaPublicEndpointFact]
    public async Task ShippingEndpointDispatchesThreeTwoPublicAccountsThroughTheSolanaProvider()
    {
        var path = Environment.GetEnvironmentVariable("OASIS_SOLANA_PUBLIC_SOURCE")
            ?? throw new InvalidOperationException("Retained public source configuration required.");
        var json = await System.IO.File.ReadAllTextAsync(path);
        using var configuration = JsonDocument.Parse(json);
        var keys = configuration.RootElement.GetProperty("addresses").EnumerateArray().Select(row => row.GetString()!).ToArray();
        var sourceOwner = configuration.RootElement.GetProperty("sourceOwner").GetGuid();
        var sourceName = "solana-public-accounts:" + configuration.RootElement.GetProperty("genesisHash").GetString();
        var wallet = new Account();
        var source = new SolanaOASIS(configuration.RootElement.GetProperty("rpcUrl").GetString()!,
            wallet.PrivateKey.Key, wallet.PublicKey.Key);
        source.ConfigurePublicAccountQuery(json);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        var activated = await source.ActivateQuerySourceAsync(deadline.Token);
        Assert.True(activated.Result && !activated.IsError, activated.ErrorCode);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        var registry = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        var providers = new Sources(source, dna, registry);
        using var services = new ServiceCollection().BuildServiceProvider();
        var actor = Guid.NewGuid();
        var receipts = new List<UsageProviderReceipt>();
        var store = new Mock<IUsageOutboxStore>();
        store.Setup(value => value.TryBeginAsync(It.IsAny<UsageOutboxOperation>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
            .Callback((string operation, string owner, UsageProviderReceipt receipt, CancellationToken token) => {
                receipt.Validate(); receipts.Add(receipt);
            }).Returns(Task.CompletedTask);
        var authority = new Mock<IUsageReservationAuthority>();
        authority.Setup(value => value.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .Returns((string token, UsageAuthorizationRequest request, CancellationToken cancellation) => Task.FromResult(
                new SubscriptionAuthorizationResult { Allowed = true, UserId = actor.ToString("D"),
                    OperationId = request.OperationId, Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) }));
        authority.Setup(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()))
            .Returns((UsageStartRequest request, CancellationToken cancellation) => Task.FromResult(
                new UsageStartResult { Started = true, OperationId = request.OperationId }));
        foreach (var count in new[] { 3, 2 })
        {
            var request = new CqlV1Request { Provider = "SolanaOASIS", OrderBy = "None", Take = 8, IncludeMetadata = true,
                Filters = new[] { new CqlV1Filter { MetadataPath = new[] { SolanaPublicAccountMetadataSource.AddressMetadata },
                    Scalar = CqlV1Scalar.String, Operator = CqlV1Operator.In, Value = JsonSerializer.SerializeToElement(keys.Take(count).ToArray()) } } };
            var http = new DefaultHttpContext { RequestServices = services, RequestAborted = deadline.Token,
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, actor.ToString("D")) }, "controlled-native-test")) };
            http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = actor };
            var execution = new Web4UsageExecution(authority.Object, store.Object, new Web4UsageLedgerOptions { Service = "WEB4" });
            var handle = await execution.BeginAsync("controlled-native-test", actor.ToString("D"), Guid.NewGuid().ToString("D"),
                new string('a', 64), "/api/data/query-cql", new UsageEndpointPolicy { MeterCategory = "cql.native-test",
                    ReservedUnits = 1, PricingCatalogueVersion = "native-test", RequiresProviderMeasurement = true, MaximumProviderAttempts = 1 });
            http.Items[Web4UsageMeasurement.ExecutionKey] = handle;
            var admission = new PreparedStorageExecution(handle, "native-solana-query", new Dictionary<ProviderType, string>(),
                new[] { ProviderType.SolanaOASIS }, new[] { new StorageQueryTariff(ProviderType.SolanaOASIS, 0, "native-test") });
            var action = await admission.RunAsync(() => CqlV1Endpoint.ExecuteAsync(http, request, providers));
            var response = Assert.IsType<FileContentResult>(action);
            using var body = JsonDocument.Parse(response.FileContents);
            var rows = body.RootElement.GetProperty("result").GetProperty("rows").EnumerateArray().ToArray();
            var expectedIds = keys.Take(count)
                .Select(key => NativeRecordHolonMapper.StableHolonId(sourceOwner, ProviderType.SolanaOASIS, sourceName, key))
                .Order();
            Assert.Equal(expectedIds, rows.Select(row => row.GetProperty("id").GetGuid()).Order());
            Assert.Equal(keys.Take(count).Order(), rows.Select(row => row.GetProperty("metadata")
                .GetProperty(SolanaPublicAccountMetadataSource.AddressMetadata).GetString()).Order());
            Assert.Empty(registry.GetAllRegisteredProviders());
        }
        Assert.Equal(2, receipts.Count);
        var deactivated = await source.DeActivateProviderAsync();
        Assert.True(deactivated.Result && !deactivated.IsError);
    }
}

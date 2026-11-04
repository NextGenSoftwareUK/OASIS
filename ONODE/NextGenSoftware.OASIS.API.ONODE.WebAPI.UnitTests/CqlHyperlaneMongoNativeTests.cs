using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.Common;
using SynapseSource = NextGenSoftware.OASIS.API.Providers.SynapseOASIS.SynapsePublicTransactionMetadataSource;
using DeBridgeSource = NextGenSoftware.OASIS.API.Providers.deBridgeOASIS.DeBridgePublicOrderMetadataSource;
using StargateSource = NextGenSoftware.OASIS.API.Providers.StargateOASIS.StargatePublicMessageMetadataSource;
using LayerZeroSource = NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS.LayerZeroPublicMessageMetadataSource;
using HyperlaneSource = NextGenSoftware.OASIS.API.Providers.HyperlaneOASIS.HyperlanePublicMessageMetadataSource;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class HyperlaneMongoNativeFactAttribute : FactAttribute
{
    public HyperlaneMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_HYPERLANE_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Explicit public Hyperlane/Mongo native gate; reported separately from controlled API tests.";
    }
}

/// <summary>Real native reads through shipping CQL endpoints over loopback HTTP.
/// SDK-owned writes seed private Mongo copies. Controlled bearer/admission is
/// test infrastructure, not production signup/billing or replication proof.</summary>
public sealed class LayerZeroMongoNativeFactAttribute : FactAttribute
{
    public LayerZeroMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_LAYERZERO_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Explicit LayerZero/Mongo native gate.";
    }
}

public sealed class StargateMongoNativeFactAttribute : FactAttribute
{
    public StargateMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_STARGATE_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Explicit Stargate/Mongo native gate.";
    }
}

public sealed class DeBridgeMongoNativeFactAttribute : FactAttribute
{
    public DeBridgeMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_DEBRIDGE_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Explicit deBridge/Mongo native gate.";
    }
}

public sealed class SynapseMongoNativeFactAttribute : FactAttribute
{
    public SynapseMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_SYNAPSE_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Explicit Synapse/Mongo native gate.";
    }
}

public sealed class CqlHyperlaneMongoNativeTests
{
    private sealed class Sources(IOASISStorageProvider mongo, IOASISStorageProvider publicSource, ProviderType publicProvider,
        OASISDNA dna, ProviderManager registry) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => requested switch {
            null or ProviderType.MongoDBOASIS => mongo,
            var provider when provider == publicProvider => publicSource,
            _ => throw new InvalidOperationException("Only the two explicitly enrolled native fixture providers are available.") };
        public HolonManager CreateManager(IOASISStorageProvider provider) => new(null, dna, registry);
    }

    [HyperlaneMongoNativeFact]
    public Task NativeHyperlaneMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
        => VerifyAsync(ProviderType.HyperlaneOASIS, "Hyperlane", "hyperlane",
            HyperlaneSource.MessageIdMetadata, HyperlaneSource.FromJson,
            new[] { "HyperlaneOriginDomainId", "HyperlaneDestinationDomainId" },
            (native, metadata, _) => Assert.Equal((bool)native.MetaData["HyperlaneIsDelivered"],
                metadata.GetProperty("HyperlaneIsDelivered").GetBoolean()),
            Environment.GetEnvironmentVariable("COSMIC_HYPERLANE_HTTP_CONTINUATION") == "original-read-only",
            "explorer-reported-delivery-state");

    [LayerZeroMongoNativeFact]
    public Task NativeLayerZeroMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
        => VerifyAsync(ProviderType.LayerZeroV2OASIS, "LayerZeroV2", "layerzero",
            LayerZeroSource.MessageIdMetadata, LayerZeroSource.FromJson,
            new[] { "LayerZeroSourceEndpoint", "LayerZeroDestinationEndpoint" },
            VerifyScanMetadata, false, "scan-reported-message-status");

    [StargateMongoNativeFact]
    public Task NativeStargateMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
        => VerifyAsync(ProviderType.StargateOASIS, "Stargate", "stargate",
            StargateSource.MessageIdMetadata, StargateSource.FromJson,
            new[] { "LayerZeroSourceEndpoint", "LayerZeroDestinationEndpoint" },
            VerifyScanMetadata, false, "scan-reported-stargate-token-messaging");

    [DeBridgeMongoNativeFact]
    public Task NativeDeBridgeMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
        => VerifyAsync(ProviderType.deBridgeOASIS, "DeBridge", "debridge",
            DeBridgeSource.OrderIdMetadata, DeBridgeSource.FromJson, Array.Empty<string>(),
            (native, metadata, isMongo) =>
            {
                foreach (var field in new[] { "DeBridgeSourceChain", "DeBridgeDestinationChain", "DeBridgeGiveAmount" })
                    Assert.Equal((string)native.MetaData[field], metadata.GetProperty(field).GetString());
                var state = metadata.GetProperty("DeBridgeOrderState").GetString();
                Assert.False(string.IsNullOrWhiteSpace(state));
                if (isMongo) Assert.Equal((string)native.MetaData["DeBridgeOrderState"], state);
            }, false, "dln-indexed-order-state");

    [SynapseMongoNativeFact]
    public Task NativeSynapseMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
        => VerifyAsync(ProviderType.SynapseOASIS, "Synapse", "synapse",
            SynapseSource.TransactionIdMetadata, SynapseSource.FromJson,
            new[] { "SynapseOriginChainId", "SynapseDestinationChainId" },
            (_, _, _) => { }, false, "rfq-indexed-transaction-identity");

    private static void VerifyScanMetadata(IHolon native, JsonElement metadata, bool isMongo)
    {
        Assert.Equal((string)native.MetaData["LayerZeroNonce"], metadata.GetProperty("LayerZeroNonce").GetString());
        var status = metadata.GetProperty("LayerZeroScanStatus").GetString();
        Assert.False(string.IsNullOrWhiteSpace(status));
        if (isMongo) Assert.Equal((string)native.MetaData["LayerZeroScanStatus"], status);
    }

    private static async Task VerifyAsync<TSource>(ProviderType publicProvider, string directoryPrefix, string fixture,
        string metadataKey, Func<string, TSource> createSource, string[] endpointFields,
        Action<IHolon, JsonElement, bool> verifyMetadata, bool continuing, string policy)
        where TSource : IOASISStorageProvider, ICosmicQueryStorageProvider, ICosmicReadOnlyQueryActivation, IDisposable
    {
        var Database = $"cosmic_cql_{fixture}_http_demo";
        var Owner = $"cosmic-cql-{fixture}-http-v1";
        var Types = new[] { ProviderType.MongoDBOASIS, publicProvider };
        var providerName = publicProvider.ToString();
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        Assert.True(Directory.Exists(Path.Combine(root, $"LocalState/COSMIC/{directoryPrefix}PublicQualificationCurrent")));
        const string mongoConnection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs";
        var mongoUrl = new MongoUrl(mongoConnection);
        Assert.Equal("127.0.0.1", Assert.Single(mongoUrl.Servers).Host);
        Assert.Equal(27029, Assert.Single(mongoUrl.Servers).Port);
        Assert.Equal("oasisRecoveryRs", mongoUrl.ReplicaSetName);
        var state = Path.Combine(root, $"LocalState/COSMIC/{directoryPrefix}MongoHttpCurrent");
        Directory.CreateDirectory(state);
        var intent = Path.Combine(state, "fixture.intent.json");
        if (!continuing) Assert.Empty(Directory.EnumerateFiles(state));
        else Assert.False(IoFile.Exists(Path.Combine(state, "fixture.cleaned.json")));
        var configPath = Path.Combine(root, $"LocalState/COSMIC/{directoryPrefix}PublicQualificationCurrent/source.json");
        var configJson = await IoFile.ReadAllTextAsync(configPath);
        using var config = JsonDocument.Parse(configJson);
        var allowed = config.RootElement.GetProperty("messageIds").EnumerateArray().Select(key => key.GetString()!).ToArray();
        Assert.Equal(3, allowed.Length);
        using var previous = continuing ? JsonDocument.Parse(await IoFile.ReadAllTextAsync(intent)) : null;
        var prior = previous?.RootElement;
        if (prior is { } originalIntent)
        {
            Assert.Equal(Owner, originalIntent.GetProperty(nameof(Owner)).GetString());
            Assert.Equal(Database, originalIntent.GetProperty(nameof(Database)).GetString());
            Assert.Equal(allowed, originalIntent.GetProperty("allowed").EnumerateArray().Select(key => key.GetString()));
        }
        var actor = prior?.GetProperty("actor").GetGuid() ?? Guid.NewGuid();
        var foreignActor = prior?.GetProperty("foreignActor").GetGuid() ?? Guid.NewGuid();
        var nonce = prior?.GetProperty("nonce").GetString() ?? Guid.NewGuid().ToString("D");
        var identities = CqlHostedDemoIdentities.Create(new CqlDemoIdentityRequest {
            Provider = providerName, Keys = allowed.Take(2).ToArray() }, actor);
        var copies = identities.Records.Select(record => record.Id).ToArray();
        var operations = prior?.GetProperty("operations").EnumerateArray().Select(value => value.GetGuid()).ToArray()
            ?? new[] { Guid.NewGuid(), Guid.NewGuid() };
        var client = new MongoClient(mongoConnection);
        using (var names = await client.ListDatabaseNamesAsync())
        {
            if (continuing) Assert.Contains(Database, await names.ToListAsync());
            else Assert.DoesNotContain(Database, await names.ToListAsync());
        }
        if (continuing)
        {
            Assert.Equal(copies, prior!.Value.GetProperty("copies").EnumerateArray().Select(value => value.GetGuid()));
            Assert.Equal(2, operations.Length);
            for (int index = 0; index < copies.Length; index++)
            {
                using var receipt = JsonDocument.Parse(await IoFile.ReadAllTextAsync(
                    Path.Combine(state, $"save-{index}.acknowledged.json")));
                Assert.Equal(copies[index], receipt.RootElement.GetProperty("id").GetGuid());
                Assert.Equal(operations[index], receipt.RootElement.GetProperty("operation").GetGuid());
                Assert.Equal(1, receipt.RootElement.GetProperty("version").GetInt32());
            }
        }
        else
        {
            await using var file = new FileStream(intent, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(file, new { Owner, nonce, Database, actor, foreignActor, allowed, copies, operations });
            file.Flush(true);
        }

        using var publicSource = createSource(configJson);
        Assert.False((await publicSource.ActivateQuerySourceAsync(default)).IsError);
        var nativeQuery = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(publicProvider).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(row => allowed.Contains(CosmicQueryMetadata.Value<string>(row, metadataKey)));
        Assert.False(nativeQuery.IsError, nativeQuery.ErrorCode);
        var plan = CosmicQueryPlanner.Plan(nativeQuery.Result, publicProvider,
            new[] { new CosmicQueryTarget(publicProvider, publicSource.QueryCapabilities) });
        Assert.False(plan.IsError, plan.ErrorCode);
        var source = await publicSource.ExecuteCosmicQueryAsync(plan.Result);
        Assert.False(source.IsError, source.ErrorCode);
        Assert.Equal(3, source.Result.Count);

        var native = client.GetDatabase(Database);
        var markers = native.GetCollection<BsonDocument>("CosmicOwnedFixture");
        if (continuing)
        {
            var existing = await markers.Find(new BsonDocument("_id", nonce)).SingleAsync();
            Assert.Equal(Owner, existing["owner"].AsString);
            Assert.Equal(2, await native.GetCollection<BsonDocument>("Holon")
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        else await markers.InsertOneAsync(new BsonDocument { { "_id", nonce }, { "owner", Owner } });
        var mongo = new MongoSource(mongoConnection, Database);
        Assert.False((await mongo.ActivateProviderAsync()).IsError);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var registry = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        registry.RegisterProvider(mongo);
        var providers = new Sources(mongo, publicSource, publicProvider, dna, registry);
        var manager = providers.CreateManager(mongo);
        for (var index = 0; !continuing && index < identities.Records.Count; index++)
        {
            var identity = identities.Records[index];
            var original = source.Result.Single(row => (string)row.MetaData[metadataKey] == identity.NativeKey);
            var metadata = original.MetaData.ToDictionary(pair => pair.Key, pair => pair.Value);
            foreach (var pair in identity.Metadata) metadata[pair.Key] = pair.Value;
            metadata["COSMIC.Demo.Dataset"] = "COSMIC.CQL.Demo.v1";
            metadata["COSMIC.Demo.SourceHolonId"] = original.Id.ToString("D");
            var saved = await manager.SaveHolonAsync(new Holon { Id = identity.Id, Name = original.Name,
                Description = original.Description, IsNewHolon = true, IsPublic = false, IsActive = true, MetaData = metadata },
                actor, new CosmicWriteOptions(operations[index], new Dictionary<Guid, Guid?> { [identity.Id] = null }),
                saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Assert.False(saved.IsError, saved.ErrorCode);
            Assert.True(saved.IsSaved);
            Assert.Equal(actor, saved.Result.CreatedByAvatarId);
            Assert.Equal(1, saved.Result.Version);
            await IoFile.WriteAllTextAsync(Path.Combine(state, "save-" + index + ".acknowledged.json"),
                JsonSerializer.Serialize(new { id = identity.Id, operation = operations[index], version = saved.Result.Version }));
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>(), ContentRootPath = root });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(new Dictionary<string, Guid[]> { [providerName] = source.Result.Select(row => row.Id).ToArray() }) });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllers();
        builder.Services.AddConfiguredCqlReadAccessPolicy(builder.Configuration);
        await using var app = builder.Build();
        app.Use(async (http, next) =>
        {
            try { await next(http); }
            catch (Exception error)
            {
                if (http.Response.HasStarted) throw;
                http.Response.StatusCode = 500;
                await http.Response.WriteAsync($"{error.GetType().FullName}: {error.Message}\n{error.StackTrace}");
            }
        });
        var ownerToken = Guid.NewGuid().ToString("N"); var foreignToken = Guid.NewGuid().ToString("N");
        var receipts = new List<UsageProviderReceipt>();
        app.Use(async (http, next) =>
        {
            var bearer = http.Request.Headers.Authorization.ToString();
            var currentActor = bearer == "Bearer " + ownerToken ? actor : bearer == "Bearer " + foreignToken ? foreignActor : Guid.Empty;
            if (currentActor == Guid.Empty) { http.Response.StatusCode = 401; return; }
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, currentActor.ToString("D")) }, "controlled-native-test"));
            http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = currentActor };
            var store = new Mock<IUsageOutboxStore>();
            store.Setup(value => value.TryBeginAsync(It.IsAny<UsageOutboxOperation>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            store.Setup(value => value.AppendReceiptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UsageProviderReceipt>(), It.IsAny<CancellationToken>()))
                .Callback((string operation, string owner, UsageProviderReceipt receipt, CancellationToken token) => { receipt.Validate(); receipts.Add(receipt); })
                .Returns(Task.CompletedTask);
            var authority = new Mock<IUsageReservationAuthority>();
            authority.Setup(value => value.AuthorizeUsageAsync(It.IsAny<string>(), It.IsAny<UsageAuthorizationRequest>(), It.IsAny<CancellationToken>()))
                .Returns((string token, UsageAuthorizationRequest request, CancellationToken cancellation) => Task.FromResult(
                    new SubscriptionAuthorizationResult { Allowed = true, UserId = currentActor.ToString("D"),
                        OperationId = request.OperationId, Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) }));
            authority.Setup(value => value.StartUsageAsync(It.IsAny<UsageStartRequest>(), It.IsAny<CancellationToken>()))
                .Returns((UsageStartRequest request, CancellationToken cancellation) => Task.FromResult(
                    new UsageStartResult { Started = true, OperationId = request.OperationId }));
            http.Request.EnableBuffering();
            var fingerprint = Convert.ToHexString(await SHA256.HashDataAsync(http.Request.Body)).ToLowerInvariant();
            http.Request.Body.Position = 0;
            var execution = new Web4UsageExecution(authority.Object, store.Object, new Web4UsageLedgerOptions { Service = "WEB4" });
            http.Items[Web4UsageMeasurement.ExecutionKey] = await execution.BeginAsync(bearer, currentActor.ToString("D"),
                http.Request.Headers["Idempotency-Key"].ToString(), fingerprint, http.Request.Path,
                new UsageEndpointPolicy { MeterCategory = "cql.native-test", ReservedUnits = 1,
                    PricingCatalogueVersion = "native-test", RequiresProviderMeasurement = true, MaximumProviderAttempts = 2 });
            await next(http);
        });
        async Task Execute(HttpContext http, bool federation)
        {
            var handle = (UsageExecutionHandle)http.Items[Web4UsageMeasurement.ExecutionKey]!;
            var admission = new PreparedStorageExecution(handle, $"native-{fixture}-query", new Dictionary<ProviderType, string>(),
                Types, Types.Select(type => new StorageQueryTariff(type, 0, "native-test")),
                null!, false, null!, new[] { publicProvider });
            IActionResult action;
            if (federation)
            {
                var request = await http.Request.ReadFromJsonAsync<CqlFederationV1Request>();
                action = await admission.RunAsync(() => CqlV1Endpoint.ExecuteFederationAsync(http, request!, providers));
            }
            else
            {
                var request = await http.Request.ReadFromJsonAsync<CqlV1Request>();
                action = await admission.RunAsync(() => CqlV1Endpoint.ExecuteAsync(http, request!, providers));
            }
            await action.ExecuteResultAsync(new ActionContext(http, new RouteData(), new ActionDescriptor()));
        }
        app.MapPost("/api/data/query-cql", (HttpContext http) => Execute(http, false));
        app.MapPost("/api/data/query-cql/federation", (HttpContext http) => Execute(http, true));
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(20) };
            var counts = new Dictionary<string, int>();
            object Selection(IEnumerable<string> selected) => new { orderBy = "None", take = 8, includeMetadata = true,
                filters = new[] { new { metadataPath = new[] { metadataKey }, scalar = "String", @operator = "In", value = selected.ToArray() } } };
            async Task Check(string name, string token, string route, object request, int expected)
            {
                http.DefaultRequestHeaders.Authorization = new("Bearer", token);
                http.DefaultRequestHeaders.Remove("Idempotency-Key");
                http.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
                using var response = await http.PostAsJsonAsync(route, request);
                Assert.True(response.IsSuccessStatusCode, name + ": HTTP " + (int)response.StatusCode + "\n" +
                    await response.Content.ReadAsStringAsync());
                using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
                var rows = body.RootElement.GetProperty("result").GetProperty("rows");
                Assert.Equal(expected, rows.GetArrayLength()); counts.Add(name, rows.GetArrayLength());
                // Counts alone could hide duplicate or substituted records. Check every
                // selected copy against its server-derived identity and visibility.
                var selectedIds = new HashSet<Guid>();
                foreach (var item in rows.EnumerateArray())
                {
                    var paired = route.EndsWith("/federation", StringComparison.Ordinal);
                    var selected = paired ? item.GetProperty("selected") : default;
                    var row = paired ? selected.GetProperty("row") : item;
                    var id = row.GetProperty("id").GetGuid();
                    Assert.True(selectedIds.Add(id), "Each preserved physical copy must be unique.");
                    var isMongo = copies.Contains(id);
                    Assert.True(isMongo || source.Result.Any(original => original.Id == id));
                    if (paired) Assert.Equal(isMongo ? "MongoDBOASIS" : providerName, selected.GetProperty("provider").GetString());
                    Assert.Equal(!isMongo, row.GetProperty("isPublic").GetBoolean());
                    var metadata = row.GetProperty("metadata");
                    var nativeKey = metadata.GetProperty(metadataKey).GetString()!;
                    var nativeRow = source.Result.Single(value =>
                        (string)value.MetaData[metadataKey] == nativeKey);
                    foreach (var field in endpointFields)
                        Assert.Equal((long)nativeRow.MetaData[field], metadata.GetProperty(field).GetInt64());
                    verifyMetadata(nativeRow, metadata, isMongo);
                    if (isMongo)
                    {
                        Assert.Equal(ownerToken, token);
                        Assert.Equal(actor, row.GetProperty("createdByAvatarId").GetGuid());
                        Assert.Equal(1, row.GetProperty("version").GetInt32());
                    }
                }
            }
            foreach (var count in new[] { 3, 2 })
            {
                var selected = allowed.Take(count).ToArray();
                await Check(fixture + "-" + count, ownerToken, "/api/data/query-cql", new { provider = providerName,
                    orderBy = "None", take = 8, includeMetadata = true,
                    filters = new[] { new { metadataPath = new[] { metadataKey }, scalar = "String", @operator = "In", value = selected } } }, count);
                await Check("paired-" + count, ownerToken, "/api/data/query-cql/federation", new { scope = "Named",
                    providers = new[] { "MongoDBOASIS", providerName }, source = Selection(selected),
                    identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 }, count + 2);
            }
            await Check("foreign-private", foreignToken, "/api/data/query-cql", new { provider = "MongoDBOASIS",
                orderBy = "Id", take = 8, filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = copies } } }, 0);
            await Check("foreign-public-pair", foreignToken, "/api/data/query-cql/federation", new { scope = "Named",
                providers = new[] { "MongoDBOASIS", providerName }, source = Selection(allowed),
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 }, 3);
            Assert.Equal(configJson, await IoFile.ReadAllTextAsync(configPath));
            Assert.Equal(9, receipts.Count);
            await IoFile.WriteAllTextAsync(Path.Combine(root, $"TestResults/{directoryPrefix}PublicSourceCurrent/{fixture}-mongo-native-summary.json"),
                JsonSerializer.Serialize(new { counts, sdkOwnedSaves = continuing ? 0 : 2,
                    receiptCount = receipts.Count, productionBilling = false, replicationAcceptance = false, browserAcceptance = false }));
        }
        finally { await app.StopAsync(); await publicSource.DeActivateProviderAsync(); }
        // Success-only cleanup, after HTTP consumers stop and exact ownership
        // checks. Failed/uncertain writes retain the original DB and intent.
        Assert.Equal(2, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var marker = await native.GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id", nonce)).SingleAsync();
        Assert.Equal(Owner, marker["owner"].AsString);
        await mongo.DeActivateProviderAsync();
        await client.DropDatabaseAsync(Database);
        using var remaining = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(Database, await remaining.ToListAsync());
        await IoFile.WriteAllTextAsync(Path.Combine(state, "fixture.cleaned.json"), JsonSerializer.Serialize(new { nonce, databaseAbsent = true }));
        var sourceSha256 = Convert.ToHexString(SHA256.HashData(await IoFile.ReadAllBytesAsync(configPath))).ToLowerInvariant();
        await using var qualified = new FileStream(Path.Combine(Path.GetDirectoryName(configPath)!,
            "web4-local-qualified.acknowledged.json"), FileMode.CreateNew);
        await JsonSerializer.SerializeAsync(qualified, new { schema = 1, web4LocalQualified = true, sourceSha256,
            policy, nativeCounts = new[] { 3, 2 }, federationCounts = new[] { 5, 4 },
            foreignPrivate = 0, foreignPublic = 3, savedPrivateCopies = 2, fixtureCleaned = true });
        qualified.Flush(true);
    }
}

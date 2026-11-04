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
using ClickHouseSource = NextGenSoftware.OASIS.API.Providers.ClickHouseOASIS.ClickHousePublicCountryMetadataSource;
using D1Source = NextGenSoftware.OASIS.API.Providers.CloudflareD1OASIS.D1WorkerSymbolQuerySource;
using QuestDbSource = NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.QuestDbSymbolQuerySource;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class ClickHouseMongoNativeFactAttribute : FactAttribute
{
    public ClickHouseMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_CLICKHOUSE_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Explicit public ClickHouse/Mongo native gate; reported separately from controlled API tests.";
    }
}

public sealed class QuestDbMongoNativeFactAttribute : FactAttribute
{
    public QuestDbMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_QUESTDB_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Requires the isolated QuestDB and Mongo native fixtures.";
    }
}

public sealed class D1MongoNativeFactAttribute : FactAttribute
{
    public D1MongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_D1_HTTP_NATIVE") != "owned-local-27029")
            Skip = "Requires the isolated D1 Worker and Mongo native fixtures.";
    }
}

/// <summary>Real native reads through shipping CQL endpoints over loopback HTTP.
/// SDK-owned writes seed private Mongo copies. Controlled bearer/admission is
/// test infrastructure, not production signup/billing or replication proof.</summary>
public sealed class CqlClickHouseMongoNativeTests
{



    private sealed class Sources(IOASISStorageProvider mongo, IOASISStorageProvider clickhouseSource,
        OASISDNA dna, ProviderManager registry, ProviderType sourceType) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => requested switch {
            null or ProviderType.MongoDBOASIS => mongo,
            var type when type == sourceType => clickhouseSource,
            _ => throw new InvalidOperationException("Only the two explicitly enrolled native fixture providers are available.") };
        public HolonManager CreateManager(IOASISStorageProvider provider) => new(null, dna, registry);
    }

    [ClickHouseMongoNativeFact]
    public async Task NativeClickHouseMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
    {
        await RunNativeFederationAsync(ProviderType.ClickHouseOASIS);
    }

    [QuestDbMongoNativeFact]
    public async Task NativeQuestDbSymbolsAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
    {
        await RunNativeFederationAsync(ProviderType.QuestDBOASIS);
    }

    [D1MongoNativeFact]
    public async Task NativeD1SymbolsAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
    {
        await RunNativeFederationAsync(ProviderType.CloudflareD1OASIS);
    }

    private static async Task RunNativeFederationAsync(ProviderType sourceType)
    {
        bool clickHouse = sourceType == ProviderType.ClickHouseOASIS;
        var providerName = sourceType.ToString();
        var (metadataKey, slug, fixture) = sourceType switch
        {
            ProviderType.CloudflareD1OASIS => (D1Source.SymbolMetadata, "d1", "D1"),
            ProviderType.QuestDBOASIS => (QuestDbSource.SymbolMetadata, "questdb", "QuestDb"),
            _ => (ClickHouseSource.AddressMetadata, "clickhouse", "ClickHouse")
        };
        var Database = $"cosmic_cql_{slug}_http_demo";
        var Owner = $"cosmic-cql-{slug}-http-v1";
        var Types = new[] { ProviderType.MongoDBOASIS, sourceType };
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));

        const string mongoConnection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs";
        var mongoUrl = new MongoUrl(mongoConnection);
        Assert.Equal("127.0.0.1", Assert.Single(mongoUrl.Servers).Host);
        Assert.Equal(27029, Assert.Single(mongoUrl.Servers).Port);
        Assert.Equal("oasisRecoveryRs", mongoUrl.ReplicaSetName);
        var state = Path.Combine(root, $"LocalState/COSMIC/{fixture}MongoHttpCurrent");
        Directory.CreateDirectory(state);
        var intent = Path.Combine(state, "fixture.intent.json");
        Assert.False(IoFile.Exists(intent), "Preserve the original fixture outcome; never reseed/replay it.");
        Assert.Empty(Directory.EnumerateFiles(state));
        var configPath = Path.Combine(root, "LocalState/COSMIC/HostedClickHousePublicSourceCurrent/source.json");
        var configJson = clickHouse ? await IoFile.ReadAllTextAsync(configPath) : null;
        using var config = configJson == null ? null : JsonDocument.Parse(configJson);
        var allowed = !clickHouse ? new[] { "BTC-USDT", "ETH-USDT", "SOL-USDT" }
            : config!.RootElement.GetProperty("countries").EnumerateArray().Select(country => country.GetString()!).ToArray();
        Assert.Equal(3, allowed.Length);
        var actor = Guid.NewGuid();
        var foreignActor = Guid.NewGuid();
        var nonce = Guid.NewGuid().ToString("D");
        var identities = CqlHostedDemoIdentities.Create(new CqlDemoIdentityRequest {
            Provider = providerName, Keys = allowed.Take(2).ToArray() }, actor);
        var copies = identities.Records.Select(record => record.Id).ToArray();
        var operations = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var client = new MongoClient(mongoConnection);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.DoesNotContain(Database, await names.ToListAsync());
        await using (var file = new FileStream(intent, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            await JsonSerializer.SerializeAsync(file, new { Owner, nonce, Database, actor, foreignActor, allowed, copies, operations });
            file.Flush(true);
        }

        using var nativeHttp = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        using IDisposable disposableSource = sourceType switch
        {
            ProviderType.CloudflareD1OASIS => new D1Source(nativeHttp, new Uri(
                Environment.GetEnvironmentVariable("OASIS_D1_SYMBOL_TEST_ENDPOINT") ??
                    throw new InvalidOperationException("D1 fixture endpoint required.")), actor, allowed),
            ProviderType.QuestDBOASIS => new QuestDbSource(nativeHttp,
                new Uri("http://127.0.0.1:29000/api/v1/sql/execute"), actor, allowed),
            _ => ClickHouseSource.FromJson(configJson!)
        };
        var clickhouseSource = (IOASISStorageProvider)disposableSource;
        var querySource = (ICosmicQueryStorageProvider)disposableSource;
        Assert.False((await ((ICosmicReadOnlyQueryActivation)disposableSource).ActivateQuerySourceAsync(default)).IsError);
        var nativeQuery = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(sourceType).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(row => allowed.Contains(CosmicQueryMetadata.Value<string>(row, metadataKey)));
        Assert.False(nativeQuery.IsError, nativeQuery.ErrorCode);
        var plan = CosmicQueryPlanner.Plan(nativeQuery.Result, sourceType,
            new[] { new CosmicQueryTarget(sourceType, querySource.QueryCapabilities) });
        Assert.False(plan.IsError, plan.ErrorCode);
        var source = await querySource.ExecuteCosmicQueryAsync(plan.Result);
        Assert.False(source.IsError, source.ErrorCode);
        Assert.Equal(3, source.Result.Count);

        var native = client.GetDatabase(Database);
        await native.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
            new BsonDocument { { "_id", nonce }, { "owner", Owner } });
        var mongo = new MongoSource(mongoConnection, Database);
        Assert.False((await mongo.ActivateProviderAsync()).IsError);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var registry = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        registry.RegisterProvider(mongo);
        var providers = new Sources(mongo, clickhouseSource, dna, registry, sourceType);
        var manager = providers.CreateManager(mongo);
        for (var index = 0; index < identities.Records.Count; index++)
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
            var admission = new PreparedStorageExecution(handle, $"native-{slug}-query", new Dictionary<ProviderType, string>(),
                Types, Types.Select(type => new StorageQueryTariff(type, 0, "native-test")),
                null!, false, null!, new[] { sourceType });
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
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(40) };
            var counts = new Dictionary<string, int>();
            object Selection(IEnumerable<string> selected) => new { orderBy = "None", take = 8, includeMetadata = true,
                filters = new[] { new { metadataPath = new[] { metadataKey }, scalar = "String", @operator = "In", value = selected.ToArray() } } };
            async Task Check(string name, string token, string route, object request, int expected)
            {
                http.DefaultRequestHeaders.Authorization = new("Bearer", token);
                http.DefaultRequestHeaders.Remove("Idempotency-Key");
                http.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
                using var response = await http.PostAsJsonAsync(route, request);
                Assert.True(response.IsSuccessStatusCode, name + ": HTTP " + (int)response.StatusCode);
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
                    var observedMetadata = row.GetProperty("metadata");
                    var nativeKey = observedMetadata.GetProperty(metadataKey).GetString()!;
                    Assert.Contains(nativeKey, allowed);
                    if (clickHouse)
                    {
                        Assert.Equal("operator-reported-public-country-summary", observedMetadata.GetProperty("ClickHouseObservationPolicy").GetString());
                        Assert.Equal("blogs.countries", observedMetadata.GetProperty("ClickHouseTable").GetString());
                        var polygonCount = observedMetadata.GetProperty("ClickHousePolygonCount").GetString()!;
                        Assert.True(ulong.TryParse(polygonCount, System.Globalization.NumberStyles.None,
                            System.Globalization.CultureInfo.InvariantCulture, out var count) && count > 0);
                        Assert.Equal(count.ToString(System.Globalization.CultureInfo.InvariantCulture), polygonCount);
                    }
                    if (isMongo)
                    {
                        Assert.Equal(ownerToken, token);
                        Assert.Equal(actor, row.GetProperty("createdByAvatarId").GetGuid());
                        Assert.Equal(1, row.GetProperty("version").GetInt32());
                        var original = source.Result.Single(value => value.MetaData[metadataKey].ToString() ==
                            row.GetProperty("metadata").GetProperty(metadataKey).GetString());
                        foreach (var field in original.MetaData.Keys.Where(key => key.StartsWith(fixture, StringComparison.Ordinal)))
                            Assert.Equal(original.MetaData[field].ToString(), row.GetProperty("metadata").GetProperty(field).GetString());
                    }
                }
            }
            foreach (var count in new[] { 3, 2 })
            {
                var selected = allowed.Take(count).ToArray();
                await Check(slug + "-" + count, ownerToken, "/api/data/query-cql", new { provider = providerName, maximumDurationMilliseconds = 30000,
                    orderBy = "None", take = 8, includeMetadata = true,
                    filters = new[] { new { metadataPath = new[] { metadataKey }, scalar = "String", @operator = "In", value = selected } } }, count);
                await Check("paired-" + count, ownerToken, "/api/data/query-cql/federation", new { scope = "Named", maximumDurationMilliseconds = 30000,
                    providers = new[] { "MongoDBOASIS", providerName }, source = Selection(selected),
                    identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 }, count + 2);
            }
            await Check("foreign-private", foreignToken, "/api/data/query-cql", new { provider = "MongoDBOASIS",
                orderBy = "Id", take = 8, filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = copies } } }, 0);
            await Check("foreign-public-pair", foreignToken, "/api/data/query-cql/federation", new { scope = "Named", maximumDurationMilliseconds = 30000,
                providers = new[] { "MongoDBOASIS", providerName }, source = Selection(allowed),
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, take = 8 }, 3);
            if (clickHouse) Assert.Equal(configJson, await IoFile.ReadAllTextAsync(configPath));
            Assert.Equal(9, receipts.Count);
            Directory.CreateDirectory(Path.Combine(root, $"TestResults/{fixture}PublicSourceCurrent"));
            await IoFile.WriteAllTextAsync(Path.Combine(root, $"TestResults/{fixture}PublicSourceCurrent/{slug}-mongo-native-summary.json"),
                JsonSerializer.Serialize(new { counts, sdkOwnedSaves = 2,
                    receiptCount = receipts.Count, productionBilling = false, replicationAcceptance = false, browserAcceptance = false }));
        }
        finally { await app.StopAsync(); await clickhouseSource.DeActivateProviderAsync(); }
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
    }
}

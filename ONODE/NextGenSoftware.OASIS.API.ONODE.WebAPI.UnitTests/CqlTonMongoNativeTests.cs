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
using TonSource = NextGenSoftware.OASIS.API.Providers.TONOASIS.TonPublicHeaderMetadataSource;
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class TonMongoNativeFactAttribute : FactAttribute
{
    public TonMongoNativeFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_REPLICA_REPAIR_NATIVE") != "owned-local-27029")
            Skip = "Explicit public Ton/Mongo native gate; reported separately from controlled API tests.";
    }
}

/// <summary>Real native reads through shipping CQL endpoints over loopback HTTP.
/// SDK-owned writes seed private Mongo copies. Controlled bearer/admission is
/// test infrastructure, not production signup/billing or replication proof.</summary>
public sealed class CqlTonMongoNativeTests
{
    private const string Database = "cosmic_cql_ton_http_demo";
    private const string Owner = "cosmic-cql-ton-http-v1";
    private static readonly ProviderType[] Types = { ProviderType.MongoDBOASIS, ProviderType.TONOASIS };
    private sealed class Sources(IOASISStorageProvider mongo, IOASISStorageProvider tonSource,
        OASISDNA dna, ProviderManager registry) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => requested switch {
            null or ProviderType.MongoDBOASIS => mongo,
            ProviderType.TONOASIS => tonSource,
            _ => throw new InvalidOperationException("Only the two explicitly enrolled native fixture providers are available.") };
        public HolonManager CreateManager(IOASISStorageProvider provider) => new(null, dna, registry);
    }

    [TonMongoNativeFact]
    public async Task NativeTonMetadataAndPrivateMongoCopiesProduceThreeTwoFiveFourOverHttp()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        Assert.True(Directory.Exists(Path.Combine(root, "LocalState/COSMIC/HostedTonPublicSourceCurrent")));
        const string mongoConnection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs";
        var mongoUrl = new MongoUrl(mongoConnection);
        Assert.Equal("127.0.0.1", Assert.Single(mongoUrl.Servers).Host);
        Assert.Equal(27029, Assert.Single(mongoUrl.Servers).Port);
        Assert.Equal("oasisRecoveryRs", mongoUrl.ReplicaSetName);
        var state = Path.Combine(root, "LocalState/COSMIC/TonMongoHttpCurrent");
        Directory.CreateDirectory(state);
        var intent = Path.Combine(state, "fixture.intent.json");
        bool nativeBudgetContinuation = Environment.GetEnvironmentVariable("COSMIC_TON_HTTP_CONTINUE_READS") == "reviewed-native-budget";
        bool readOnlyContinuation = nativeBudgetContinuation || Environment.GetEnvironmentVariable("COSMIC_TON_HTTP_CONTINUE_READS") == "reviewed-two-saves";
        bool continuing = readOnlyContinuation || Environment.GetEnvironmentVariable("COSMIC_TON_HTTP_CONTINUE_PREPERSISTENCE") == "reviewed-original-intent";
        string reviewPath = Path.Combine(state, "pre-persistence-failure.review.json");
        string continuationPath = Path.Combine(state, "pre-persistence-continuation.intent.json");
        var savedFiles = new[] { intent, reviewPath, continuationPath,
            Path.Combine(state, "save-0.acknowledged.json"), Path.Combine(state, "save-1.acknowledged.json") };
        var expectedFiles = nativeBudgetContinuation ? savedFiles.Append(Path.Combine(state, "read-continuation.intent.json")).Order().ToArray()
            : readOnlyContinuation ? savedFiles.Order().ToArray()
            : continuing ? new[] { intent, reviewPath }.Order().ToArray() : Array.Empty<string>();
        Assert.Equal(expectedFiles, Directory.EnumerateFiles(state).Order().ToArray());
        var configPath = Path.Combine(root, "LocalState/COSMIC/HostedTonPublicSourceCurrent/source.json");
        var configJson = await IoFile.ReadAllTextAsync(configPath);
        using var config = JsonDocument.Parse(configJson);
        var allowed = config.RootElement.GetProperty("blocks").EnumerateArray().Select(block => block.GetProperty("rootHash").GetString()!).ToArray();
        Assert.Equal(3, allowed.Length);
        using var originalIntent = continuing ? JsonDocument.Parse(await IoFile.ReadAllBytesAsync(intent)) : null;
        using var review = continuing ? JsonDocument.Parse(await IoFile.ReadAllBytesAsync(reviewPath)) : null;
        if (continuing)
        {
            string Digest(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.True(review!.RootElement.GetProperty("beforeFirstPersistenceByControlFlow").GetBoolean());
            Assert.True(review.RootElement.GetProperty("identitiesMustBeReused").GetBoolean());
            Assert.Equal(review.RootElement.GetProperty("originalIntentSha256").GetString(), Digest(await IoFile.ReadAllBytesAsync(intent)));
            Assert.Equal(review.RootElement.GetProperty("failedTrxSha256").GetString(), Digest(await IoFile.ReadAllBytesAsync(
                Path.Combine(root, "TestResults/TonPublicSourceCandidateCurrent/ton-mongo-http.trx"))));
            Assert.Equal(review.RootElement.GetProperty("sourceCatalogueSha256").GetString(), Digest(await IoFile.ReadAllBytesAsync(configPath)));
            Assert.Equal(Owner, originalIntent!.RootElement.GetProperty("Owner").GetString());
            Assert.Equal(Database, originalIntent.RootElement.GetProperty("Database").GetString());
            Assert.Equal(allowed, originalIntent.RootElement.GetProperty("allowed").EnumerateArray().Select(value => value.GetString()).ToArray());
        }
        var actor = continuing ? originalIntent!.RootElement.GetProperty("actor").GetGuid() : Guid.NewGuid();
        var foreignActor = continuing ? originalIntent!.RootElement.GetProperty("foreignActor").GetGuid() : Guid.NewGuid();
        var nonce = continuing ? originalIntent!.RootElement.GetProperty("nonce").GetString()! : Guid.NewGuid().ToString("D");
        Assert.NotEqual(Guid.Empty, actor);
        Assert.NotEqual(actor, foreignActor);
        Assert.NotEqual(Guid.Empty, foreignActor);
        Assert.NotEqual(Guid.Empty, Guid.Parse(nonce));
        var identities = CqlHostedDemoIdentities.Create(new CqlDemoIdentityRequest {
            Provider = "TONOASIS", Keys = allowed.Take(2).ToArray() }, actor);
        var copies = identities.Records.Select(record => record.Id).ToArray();
        var operations = continuing ? originalIntent!.RootElement.GetProperty("operations").EnumerateArray().Select(value => value.GetGuid()).ToArray()
            : new[] { Guid.NewGuid(), Guid.NewGuid() };
        Assert.Equal(2, operations.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, operations);
        if (continuing) Assert.Equal(copies, originalIntent!.RootElement.GetProperty("copies").EnumerateArray().Select(value => value.GetGuid()).ToArray());
        var client = new MongoClient(mongoConnection);
        using (var names = await client.ListDatabaseNamesAsync())
        {
            var databases = await names.ToListAsync();
            if (readOnlyContinuation) Assert.Contains(Database, databases);
            else Assert.DoesNotContain(Database, databases);
        }
        if (readOnlyContinuation)
        {
            // This reserved cohort has two acknowledged writes. Validate it before
            // making remote reads; never save, repair or replace its records.
            var database = client.GetDatabase(Database);
            var cohortMarker = await database.GetCollection<BsonDocument>("CosmicOwnedFixture")
                .Find(new BsonDocument("_id", nonce)).SingleAsync();
            Assert.Equal(Owner, cohortMarker["owner"].AsString);
            Assert.Equal(1, await database.GetCollection<BsonDocument>("CosmicOwnedFixture").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(2, await database.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            var failedBytes = await IoFile.ReadAllBytesAsync(Path.Combine(root,
                "TestResults/TonPublicSourceCandidateCurrent/ton-mongo-http-prepersistence-continuation.trx"));
            Assert.Equal("68043846418e989590925ceceecaaff0b1580152d934e143a6cf934e003d2e49",
                Convert.ToHexString(SHA256.HashData(failedBytes)).ToLowerInvariant());
            if (nativeBudgetContinuation)
            {
                var leafFailure = await IoFile.ReadAllBytesAsync(Path.Combine(root,
                    "TestResults/TonPublicSourceCandidateCurrent/ton-mongo-http-read-continuation.trx"));
                Assert.Equal("9ba60c08e7229f65d676bd885a91bcebad7d10c355e9657e4325c670a5cbc96d",
                    Convert.ToHexString(SHA256.HashData(leafFailure)).ToLowerInvariant());
                using var priorRead = JsonDocument.Parse(await IoFile.ReadAllBytesAsync(Path.Combine(state, "read-continuation.intent.json")));
                Assert.Equal(actor, priorRead.RootElement.GetProperty("actor").GetGuid());
                Assert.Equal(nonce, priorRead.RootElement.GetProperty("nonce").GetString());
                Assert.Equal(copies, priorRead.RootElement.GetProperty("copies").EnumerateArray().Select(value => value.GetGuid()).ToArray());
                Assert.Equal(operations, priorRead.RootElement.GetProperty("operations").EnumerateArray().Select(value => value.GetGuid()).ToArray());
            }
            var acknowledgements = new List<string>();
            for (var index = 0; index < copies.Length; index++)
            {
                var bytes = await IoFile.ReadAllBytesAsync(Path.Combine(state, "save-" + index + ".acknowledged.json"));
                using var ack = JsonDocument.Parse(bytes);
                Assert.Equal(copies[index], ack.RootElement.GetProperty("id").GetGuid());
                Assert.Equal(operations[index], ack.RootElement.GetProperty("operation").GetGuid());
                Assert.Equal(1, ack.RootElement.GetProperty("version").GetInt32());
                acknowledgements.Add(Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
            }
            await using var journal = new FileStream(Path.Combine(state, nativeBudgetContinuation ? "native-budget-continuation.intent.json" : "read-continuation.intent.json"), FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(journal, new { actor, foreignActor, nonce, copies, operations,
                acknowledgements, databaseValidated = true, maximumDurationMilliseconds = 30000 });
            journal.Flush(true);
        }
        else if (continuing)
        {
            // The original failure preceded all DB writes. Resume only those
            // reserved IDs after a fresh DB-absence check; never reseed a DB or
            // repeat a continuation whose outcome is unknown.
            await using var file = new FileStream(continuationPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(file, new { originalIntentSha256 = review!.RootElement.GetProperty("originalIntentSha256").GetString(),
                databaseAbsentBeforeContinuation = true, actor, foreignActor, nonce, copies, operations });
            file.Flush(true);
        }
        else
        {
            await using var file = new FileStream(intent, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(file, new { Owner, nonce, Database, actor, foreignActor, allowed, copies, operations });
            file.Flush(true);
        }

        using var tonSource = TonSource.FromJson(configJson);
        var activated = await tonSource.ActivateQuerySourceAsync(default);
        Assert.False(activated.IsError, activated.Exception?.ToString() ?? activated.ErrorCode);
        var nativeQuery = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(row => allowed.Contains(CosmicQueryMetadata.Value<string>(row, TonSource.AddressMetadata)));
        Assert.False(nativeQuery.IsError, nativeQuery.ErrorCode);
        var plan = CosmicQueryPlanner.Plan(nativeQuery.Result, ProviderType.TONOASIS,
            new[] { new CosmicQueryTarget(ProviderType.TONOASIS, tonSource.QueryCapabilities) });
        Assert.False(plan.IsError, plan.ErrorCode);
        var source = await tonSource.ExecuteCosmicQueryAsync(plan.Result);
        Assert.False(source.IsError, source.Exception?.ToString() ?? source.ErrorCode);
        Assert.Equal(3, source.Result.Count);

        var native = client.GetDatabase(Database);
        if (!readOnlyContinuation)
            await native.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
                new BsonDocument { { "_id", nonce }, { "owner", Owner } });
        var mongo = new MongoSource(mongoConnection, Database);
        Assert.False((await mongo.ActivateProviderAsync()).IsError);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var registry = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        registry.RegisterProvider(mongo);
        var providers = new Sources(mongo, tonSource, dna, registry);
        var manager = providers.CreateManager(mongo);
        for (var index = 0; index < identities.Records.Count; index++)
        {
            var identity = identities.Records[index];
            var original = source.Result.Single(row => (string)row.MetaData[TonSource.AddressMetadata] == identity.NativeKey);
            if (readOnlyContinuation)
            {
                var loaded = await mongo.LoadHolonAsync(identity.Id, loadChildren: false);
                Assert.False(loaded.IsError, loaded.ErrorCode);
                Assert.Equal(identity.Id, loaded.Result.Id);
                Assert.Equal(actor, loaded.Result.CreatedByAvatarId);
                Assert.Equal(1, loaded.Result.Version);
                Assert.False(loaded.Result.IsPublic);
                Assert.Equal(original.Id.ToString("D"), loaded.Result.MetaData["COSMIC.Demo.SourceHolonId"]);
                foreach (var pair in identity.Metadata) Assert.Equal(pair.Value, loaded.Result.MetaData[pair.Key]);
                continue; // No Save or replacement operation in read-only continuation.
            }
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
            [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(new Dictionary<string, Guid[]> { ["TONOASIS"] = source.Result.Select(row => row.Id).ToArray() }) });
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
            var admission = new PreparedStorageExecution(handle, "native-ton-query", new Dictionary<ProviderType, string>(),
                Types, Types.Select(type => new StorageQueryTariff(type, 0, "native-test")),
                null!, false, null!, new[] { ProviderType.TONOASIS });
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
                filters = new[] { new { metadataPath = new[] { TonSource.AddressMetadata }, scalar = "String", @operator = "In", value = selected.ToArray() } } };
            async Task Check(string name, string token, string route, object request, int expected)
            {
                http.DefaultRequestHeaders.Authorization = new("Bearer", token);
                http.DefaultRequestHeaders.Remove("Idempotency-Key");
                http.DefaultRequestHeaders.Add("Idempotency-Key", Guid.NewGuid().ToString("D"));
                using var response = await http.PostAsJsonAsync(route, request);
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                Assert.True(response.IsSuccessStatusCode, name + ": HTTP " + (int)response.StatusCode
                    + " " + Encoding.UTF8.GetString(responseBytes));
                using var body = JsonDocument.Parse(responseBytes);
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
                    if (paired) Assert.Equal(isMongo ? "MongoDBOASIS" : "TONOASIS", selected.GetProperty("provider").GetString());
                    Assert.Equal(!isMongo, row.GetProperty("isPublic").GetBoolean());
                    if (isMongo)
                    {
                        Assert.Equal(ownerToken, token);
                        Assert.Equal(actor, row.GetProperty("createdByAvatarId").GetGuid());
                    }
                }
            }
            foreach (var count in new[] { 3, 2 })
            {
                var selected = allowed.Take(count).ToArray();
                await Check("ton-" + count, ownerToken, "/api/data/query-cql", new { provider = "TONOASIS", maximumDurationMilliseconds = 30000,
                    orderBy = "None", take = 8, includeMetadata = true,
                    filters = new[] { new { metadataPath = new[] { TonSource.AddressMetadata }, scalar = "String", @operator = "In", value = selected } } }, count);
                await Check("paired-" + count, ownerToken, "/api/data/query-cql/federation", new { scope = "Named",
                    providers = new[] { "MongoDBOASIS", "TONOASIS" }, source = Selection(selected),
                    identityMode = "PreserveCopies", maximumProviderAttempts = 2, maximumDurationMilliseconds = 30000, take = 8 }, count + 2);
            }
            await Check("foreign-private", foreignToken, "/api/data/query-cql", new { provider = "MongoDBOASIS",
                orderBy = "Id", take = 8, filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = copies } } }, 0);
            await Check("foreign-public-pair", foreignToken, "/api/data/query-cql/federation", new { scope = "Named",
                providers = new[] { "MongoDBOASIS", "TONOASIS" }, source = Selection(allowed),
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, maximumDurationMilliseconds = 30000, take = 8 }, 3);
            Assert.Equal(configJson, await IoFile.ReadAllTextAsync(configPath));
            Assert.Equal(9, receipts.Count);
            await IoFile.WriteAllTextAsync(Path.Combine(root, "TestResults/TonPublicSourceCandidateCurrent/ton-mongo-native-summary.json"),
                JsonSerializer.Serialize(new { counts, sdkOwnedSaves = 2,
                    receiptCount = receipts.Count, productionBilling = false, replicationAcceptance = false, browserAcceptance = false }));
        }
        finally { await app.StopAsync(); await tonSource.DeActivateProviderAsync(); }
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

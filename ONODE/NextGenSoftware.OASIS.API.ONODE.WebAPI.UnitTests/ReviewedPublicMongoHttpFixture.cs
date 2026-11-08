using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
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
using MongoSource = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using IoFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class ReviewedPublicMongoNativeTheoryAttribute : TheoryAttribute
{
    public ReviewedPublicMongoNativeTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("COSMIC_REVIEWED_PUBLIC_MONGO_NATIVE") != "owned-local-27029")
            Skip = "Explicit public exact-key source/Mongo native gate; reported separately from controlled API tests.";
    }
}

public sealed class ReviewedPublicMongoNativeFactAttribute : FactAttribute
{
    public ReviewedPublicMongoNativeFactAttribute(string? requiredSetting = null)
    {
        if (Environment.GetEnvironmentVariable("COSMIC_REVIEWED_PUBLIC_MONGO_NATIVE") != "owned-local-27029")
            Skip = "Explicit public exact-key source/Mongo native gate; reported separately from controlled API tests.";
        else if (requiredSetting is not null && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(requiredSetting)))
            Skip = "The reviewed source requires its protected native setting.";
    }
}

/// <summary>Shared real HTTP/Mongo fixture with an explicitly supplied native source factory.</summary>
internal static class ReviewedPublicMongoHttpFixture
{
    private const string Owner = "cosmic-cql-reviewed-public-mongo-http-v1";
    private sealed class Sources(IOASISStorageProvider mongo, IOASISStorageProvider source,
        ProviderType sourceType, OASISDNA dna, ProviderManager registry) : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => requested switch {
            null or ProviderType.MongoDBOASIS => mongo,
            _ when requested == sourceType => source,
            _ => throw new InvalidOperationException("Only the two explicitly enrolled native fixture providers are available.") };
        public HolonManager CreateManager(IOASISStorageProvider provider) => new(null, dna, registry);
    }

    private sealed record FixtureIntent(string Owner, string nonce, string database, Guid actor,
        Guid foreignActor, Guid[] allowed, Guid[] copies, Guid[] operations);

    private sealed class HttpStatusTrace : EventListener
    {
        public ConcurrentQueue<int> Statuses { get; } = new();

        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "System.Net.Http")
                EnableEvents(source, EventLevel.Informational);
        }

        protected override void OnEventWritten(EventWrittenEventArgs trace)
        {
            if (trace.EventName == "RequestStop" && trace.Payload is { Count: 1 } && trace.Payload[0] is int status)
                Statuses?.Enqueue(status);
        }
    }

    private sealed class NativeFailureTrace : ILoggerProvider, ILogger
    {
        public ConcurrentQueue<string> Events { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Warning;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id == 4831) Events.Enqueue(formatter(state, null));
        }
        public void Dispose() { }
    }

    public static async Task VerifyHttpAsync(ProviderType provider,
        Func<Guid, (IOASISStorageProvider Source, Guid[] Ids)> sourceFactory, bool resume = false)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        const string mongoConnection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=10000";
        var database = "cosmic_cql_" + provider.ToString().ToLowerInvariant() + "_http";
        var types = new[] { ProviderType.MongoDBOASIS, provider };
        var mongoUrl = new MongoUrl(mongoConnection);
        Assert.Equal("127.0.0.1", Assert.Single(mongoUrl.Servers).Host);
        Assert.Equal(27029, Assert.Single(mongoUrl.Servers).Port);
        Assert.Equal("oasisRecoveryRs", mongoUrl.ReplicaSetName);
        var state = Path.Combine(root, "LocalState", "COSMIC", "ReviewedPublicMongoHttpQualification", provider.ToString());
        Directory.CreateDirectory(state);
        var intent = Path.Combine(state, "fixture.intent.json");
        FixtureIntent? retained = null;
        if (resume)
        {
            retained = JsonSerializer.Deserialize<FixtureIntent>(await IoFile.ReadAllBytesAsync(intent));
            Assert.NotNull(retained);
            Assert.Equal(Owner, retained.Owner);
            Assert.Equal(database, retained.database);
            Assert.True(Guid.TryParseExact(retained.nonce, "D", out _));
            Assert.NotEqual(Guid.Empty, retained.actor);
            Assert.NotEqual(Guid.Empty, retained.foreignActor);
            Assert.NotEqual(retained.actor, retained.foreignActor);
            Assert.False(IoFile.Exists(Path.Combine(state, "fixture.cleaned.json")));
        }
        else
        {
            Assert.False(IoFile.Exists(intent), "Preserve the original fixture outcome; never reseed/replay it.");
            Assert.Empty(Directory.EnumerateFiles(state));
        }

        var actor = retained?.actor ?? Guid.NewGuid();
        var foreignActor = retained?.foreignActor ?? Guid.NewGuid();
        var nonce = retained?.nonce ?? Guid.NewGuid().ToString("D");
        var (storage, allowed) = sourceFactory(actor);
        using var sourceLifetime = (IDisposable)storage;
        var copies = retained?.copies ?? new[] { Guid.NewGuid(), Guid.NewGuid() };
        var operations = retained?.operations ?? new[] { Guid.NewGuid(), Guid.NewGuid() };
        var client = new MongoClient(mongoConnection);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.Equal(resume, (await names.ToListAsync()).Contains(database));
        if (retained is not null)
        {
            Assert.Equal(allowed, retained.allowed);
            Assert.Equal(2, copies.Length);
            Assert.Equal(2, copies.Distinct().Count());
            Assert.DoesNotContain(Guid.Empty, copies);
            Assert.Equal(2, operations.Length);
            Assert.Equal(2, operations.Distinct().Count());
            Assert.DoesNotContain(Guid.Empty, operations);
        }

        var activation = await ((ICosmicReadOnlyQueryActivation)storage).ActivateQuerySourceAsync(default);
        Assert.False(activation.IsError, activation.Exception?.ToString());
        var querySource = (ICosmicQueryStorageProvider)storage;
        var nativeQuery = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, provider, allowed).Result).Result
            .Where(row => allowed.Contains(row.Id));
        Assert.False(nativeQuery.IsError, nativeQuery.ErrorCode);
        var plan = CosmicQueryPlanner.Plan(nativeQuery.Result, provider,
            new[] { new CosmicQueryTarget(provider, querySource.QueryCapabilities) });
        Assert.False(plan.IsError, plan.ErrorCode);
        var source = await querySource.ExecuteCosmicQueryAsync(plan.Result);
        Assert.False(source.IsError, source.Exception?.ToString() ?? source.ErrorCode);
        Assert.Equal(allowed.Order(), source.Result.Select(row => row.Id).Order());

        var native = client.GetDatabase(database);
        var ownership = native.GetCollection<BsonDocument>("CosmicOwnedFixture");
        var markerFilter = new BsonDocument { { "_id", nonce }, { "owner", Owner } };
        if (resume)
        {
            Assert.Equal(1, await ownership.CountDocumentsAsync(markerFilter));
            Assert.Equal(1, await ownership.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.Equal(2, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        else
        {
            await using var file = new FileStream(intent, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
            await JsonSerializer.SerializeAsync(file, new FixtureIntent(Owner, nonce, database, actor, foreignActor, allowed, copies, operations));
            file.Flush(true);
            await ownership.InsertOneAsync(markerFilter);
        }

        var mongo = new MongoSource(mongoConnection, database);
        Assert.False((await mongo.ActivateProviderAsync()).IsError);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2 } };
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var registry = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        registry.RegisterProvider(mongo);
        var providers = new Sources(mongo, storage, provider, dna, registry);
        var manager = providers.CreateManager(mongo);
        for (var index = 0; index < copies.Length; index++)
        {
            if (resume)
            {
                var receiptPath = Path.Combine(state, "save-" + index + ".acknowledged.json");
                using var receipt = JsonDocument.Parse(await IoFile.ReadAllBytesAsync(receiptPath));
                Assert.Equal(copies[index], receipt.RootElement.GetProperty("id").GetGuid());
                Assert.Equal(operations[index], receipt.RootElement.GetProperty("operation").GetGuid());
                Assert.Equal(1, receipt.RootElement.GetProperty("version").GetInt32());
                var existing = await mongo.LoadHolonAsync(copies[index], loadChildren: false, version: 1);
                Assert.False(existing.IsError, existing.ErrorCode);
                Assert.NotNull(existing.Result);
                Assert.Equal(copies[index], existing.Result.Id);
                Assert.Equal(actor, existing.Result.CreatedByAvatarId);
                Assert.False(existing.Result.IsPublic);
                Assert.Equal(1, existing.Result.Version);
                Assert.Equal(allowed[index].ToString("D"), existing.Result.MetaData["COSMIC.Demo.SourceHolonId"]);
                continue;
            }
            var original = source.Result.Single(row => row.Id == allowed[index]);
            var metadata = original.MetaData.ToDictionary(pair => pair.Key, pair => pair.Value);
            metadata["COSMIC.Demo.Dataset"] = "COSMIC.CQL.Demo.v1";
            metadata["COSMIC.Demo.SourceHolonId"] = original.Id.ToString("D");
            var saved = await manager.SaveHolonAsync(new Holon { Id = copies[index], Name = original.Name,
                Description = original.Description, IsNewHolon = true, IsPublic = false, IsActive = true, MetaData = metadata },
                actor, new CosmicWriteOptions(operations[index], new Dictionary<Guid, Guid?> { [copies[index]] = null }),
                saveChildren: false, providerType: ProviderType.MongoDBOASIS);
            Assert.False(saved.IsError, saved.ErrorCode);
            Assert.True(saved.IsSaved);
            Assert.Equal(actor, saved.Result.CreatedByAvatarId);
            Assert.Equal(1, saved.Result.Version);
            await IoFile.WriteAllTextAsync(Path.Combine(state, "save-" + index + ".acknowledged.json"),
                JsonSerializer.Serialize(new { id = copies[index], operation = operations[index], version = saved.Result.Version }));
        }

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = Array.Empty<string>(), ContentRootPath = root });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> {
            [CqlPublicReadCatalogue.ConfigurationKey] = JsonSerializer.Serialize(new Dictionary<string, Guid[]> { [provider.ToString()] = allowed }) });
        builder.Logging.ClearProviders(); builder.WebHost.UseUrls("http://127.0.0.1:0");
        using var failureTrace = provider == ProviderType.PinataOASIS && resume ? new NativeFailureTrace() : null;
        if (failureTrace != null) builder.Logging.AddProvider(failureTrace);
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
            var admission = new PreparedStorageExecution(handle, "native-reviewed-public-query", new Dictionary<ProviderType, string>(),
                types, types.Select(type => new StorageQueryTariff(type, 0, "native-test")),
                null!, false, null!, new[] { provider });
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
        using var statusTrace = resume ? new HttpStatusTrace() : null;
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var http = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(45) };
            var counts = new Dictionary<string, int>();
            object Selection(IEnumerable<Guid> selected) => new { orderBy = "None", take = 8, includeMetadata = true,
                filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = selected.ToArray() } } };
            async Task Check(string name, string token, string route, object request, Guid[] expected)
            {
                http.DefaultRequestHeaders.Authorization = new("Bearer", token);
                http.DefaultRequestHeaders.Remove("Idempotency-Key");
                var requestId = Guid.NewGuid().ToString("D");
                http.DefaultRequestHeaders.Add("Idempotency-Key", requestId);
                statusTrace?.Statuses.Clear();
                failureTrace?.Events.Clear();
                var started = System.Diagnostics.Stopwatch.GetTimestamp();
                using var response = await http.PostAsJsonAsync(route, request);
                using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync());
                if (!response.IsSuccessStatusCode)
                {
                    var error = body.RootElement.TryGetProperty("errorCode", out var field) ? field.GetString() : "missing";
                    var statuses = statusTrace is null ? "disabled" : string.Join(",", statusTrace.Statuses);
                    var nativeFailure = failureTrace == null ? "disabled" : string.Join(", ", failureTrace.Events);
                    var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    Assert.Fail($"{name} returned HTTP {(int)response.StatusCode} ({error}) for {requestId} after {elapsed:F0} ms. " +
                        $"Outbound HTTP status sequence: {statuses}. Native failure evidence: {nativeFailure}.");
                }
                var rows = body.RootElement.GetProperty("result").GetProperty("rows");
                Assert.Equal(expected.Length, rows.GetArrayLength());
                counts.Add(name, rows.GetArrayLength());
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
                    if (paired) Assert.Equal(isMongo ? "MongoDBOASIS" : provider.ToString(), selected.GetProperty("provider").GetString());
                    Assert.Equal(!isMongo, row.GetProperty("isPublic").GetBoolean());
                    if (isMongo)
                    {
                        Assert.Equal(ownerToken, token);
                        Assert.Equal(actor, row.GetProperty("createdByAvatarId").GetGuid());
                    }
                }
                Assert.Equal(expected.Order(), selectedIds.Order());
            }
            foreach (var count in new[] { 3, 2 })
            {
                var selected = allowed.Take(count).ToArray();
                await Check("native-" + count, ownerToken, "/api/data/query-cql", new { provider = provider.ToString(),
                    orderBy = "None", take = 8, includeMetadata = true, maximumDurationMilliseconds = 30000,
                    filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = selected } } }, selected);
                await Check("paired-" + count, ownerToken, "/api/data/query-cql/federation", new { scope = "Named",
                    providers = new[] { "MongoDBOASIS", provider.ToString() }, source = Selection(selected.Concat(copies)),
                    identityMode = "PreserveCopies", maximumProviderAttempts = 2, maximumDurationMilliseconds = 30000, take = 8 },
                    selected.Concat(copies).ToArray());
            }
            await Check("foreign-private", foreignToken, "/api/data/query-cql", new { provider = "MongoDBOASIS",
                orderBy = "Id", take = 8, filters = new[] { new { field = "Id", scalar = "Guid", @operator = "In", value = copies } } },
                Array.Empty<Guid>());
            await Check("foreign-public-pair", foreignToken, "/api/data/query-cql/federation", new { scope = "Named",
                providers = new[] { "MongoDBOASIS", provider.ToString() }, source = Selection(allowed.Concat(copies)),
                identityMode = "PreserveCopies", maximumProviderAttempts = 2, maximumDurationMilliseconds = 30000, take = 8 }, allowed);
            Assert.Equal(9, receipts.Count);
            await IoFile.WriteAllTextAsync(Path.Combine(state, "mongo-http-native-summary.json"),
                JsonSerializer.Serialize(new { counts, sdkOwnedSaves = resume ? 0 : 2, reusedPrivateCopies = resume ? 2 : 0,
                    receiptCount = receipts.Count, productionBilling = false, replicationAcceptance = false, browserAcceptance = false }));
        }
        finally { await app.StopAsync(); await storage.DeActivateProviderAsync(); }
        // Success-only cleanup, after HTTP consumers stop and exact ownership
        // checks. Failed/uncertain writes retain the original DB and intent.
        Assert.Equal(2, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var marker = await native.GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id", nonce)).SingleAsync();
        Assert.Equal(Owner, marker["owner"].AsString);
        await mongo.DeActivateProviderAsync();
        await client.DropDatabaseAsync(database);
        using var remaining = await client.ListDatabaseNamesAsync();
        Assert.DoesNotContain(database, await remaining.ToListAsync());
        await IoFile.WriteAllTextAsync(Path.Combine(state, "fixture.cleaned.json"), JsonSerializer.Serialize(new { nonce, databaseAbsent = true }));
    }

}

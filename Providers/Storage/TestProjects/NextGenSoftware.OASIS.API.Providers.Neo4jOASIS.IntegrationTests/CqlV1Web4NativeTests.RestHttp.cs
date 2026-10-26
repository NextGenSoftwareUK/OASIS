extern alias ShippingWeb4;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using ShippingDataController = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers.DataController;
using ShippingJson = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers.HolonJsonContracts;
using ShippingJwt = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware.OasisJwtValidation;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed class OnlyShippingDataController : IApplicationFeatureProvider<ControllerFeature>
    {
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
        {
            foreach (var controller in feature.Controllers.ToArray())
                if (controller.AsType() != typeof(ShippingDataController)) feature.Controllers.Remove(controller);
        }
    }

    private sealed record RestResponse(string Key, string Payload, string OperationId, JsonElement Body);

    private static async Task<int> VerifyShippingRestAsync(
        Dictionary<ProviderType, IOASISStorageProvider> providers, MongoUsageOutboxStore outbox,
        Authority authority, string secret, Guid actor, Guid[] restIds, Guid foreignId, string tag,
        Func<string, object?, Task> journal, List<string> conditionalOperations)
    {
        var bearer = Token(actor, new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), lifetimeMinutes: 15);
        authority.Tokens.Add(bearer, actor);
        var priorDna = OASISDNAManager.OASISDNA;
        var globals = ProviderManager.Instance;
        var priorRegistryDna = globals.OASISDNA;
        var priorCurrent = globals.CurrentStorageProvider;
        var priorFailover = globals.IsAutoFailOverEnabled;
        var priorReplication = globals.IsAutoReplicationEnabled;
        var priorBalance = globals.IsAutoLoadBalanceEnabled;
        var requests = 0;
        try
        {
            foreach (var pair in providers)
            {
                var manager = new Sources(providers).CreateManager(pair.Value);
                OASISDNAManager.OASISDNA = manager.OASISDNA;
                globals.OASISDNA = manager.OASISDNA;
                globals.IsAutoFailOverEnabled = globals.IsAutoReplicationEnabled = globals.IsAutoLoadBalanceEnabled = false;
                var registered = globals.GetProvider(pair.Key);
                Assert.IsTrue(registered == null || ReferenceEquals(registered, pair.Value), "Never replace an unrelated registered provider.");
                if (registered == null) Assert.IsTrue(globals.RegisterProvider(pair.Value));
                Assert.IsFalse(globals.SetAndActivateCurrentStorageProvider(pair.Value).IsError);

                // These IDs are journalled before any seed; private actor and the
                // late stage keep the existing CQL query fixture expectations intact.
                await journal("before-rest-seed", new { provider = pair.Key.ToString(), actor, restIds });
                var root = new Holon { Id = restIds[0], Name = "REST root", IsActive = true, CreatedByAvatarId = actor };
                root.MetaData["fixture"] = tag;
                var child = new Holon { Id = restIds[2], ParentHolonId = root.Id, Name = "REST child", IsActive = true, CreatedByAvatarId = actor };
                child.MetaData["fixture"] = tag;
                root.Children.Add(child);
                var seed = await manager.SaveHolonAsync(root, actor, providerType: pair.Key);
                Assert.IsFalse(seed.IsError, seed.Message + seed.Exception);
                var second = new Holon { Id = restIds[1], Name = "REST second", IsActive = true, CreatedByAvatarId = actor };
                second.MetaData["fixture"] = tag;
                seed = await manager.SaveHolonAsync(second, actor, saveChildren: false, providerType: pair.Key);
                Assert.IsFalse(seed.IsError, seed.Message + seed.Exception);

                var keyRead = await manager.LoadHolonAsync(restIds[1], loadChildren: false, providerType: pair.Key);
                Assert.IsFalse(keyRead.IsError, keyRead.Message);
                var deleteKey = keyRead.Result.ProviderUniqueStorageKey[pair.Key];
                Assert.IsFalse(string.IsNullOrWhiteSpace(deleteKey));
                var deletePath = "/api/data/delete-holon-by-providerkey/" + Uri.EscapeDataString(deleteKey);
                await using var app = RestHost(pair.Key, pair.Value, outbox, authority, secret, deleteKey: deleteKey);
                await app.StartAsync();
                try
                {
                    var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                    using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
                    using var otherClient = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
                    client.DefaultRequestHeaders.Authorization = otherClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                    var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { ReferenceHandler = ReferenceHandler.IgnoreCycles };
                    jsonOptions.Converters.Add(new JsonStringEnumConverter());
                    ShippingJson.Configure(jsonOptions);

                    async Task<Holon> Read(Guid id, bool children = false)
                    {
                        var read = await manager.LoadHolonAsync(id, loadChildren: children,
                            loadChildrenFromProvider: children, providerType: pair.Key);
                        Assert.IsFalse(read.IsError, read.Message + read.Exception);
                        // A value-only stored-record projection deliberately
                        // excludes navigation. Preserve the actual loaded graph
                        // when asserting child retention and recursive saves.
                        Assert.IsInstanceOfType(read.Result, typeof(Holon));
                        return (Holon)read.Result;
                    }
                    Guid Revision(IHolon value) => CosmicConcurrency.GetProviderRevisionId(value, pair.Key)!.Value;
                    async Task<RestResponse> Send(object payload, Guid intent, HttpStatusCode expected,
                        int attempts, decimal cost, string path = "/api/data/save-holon", HttpClient? caller = null,
                        HttpMethod? method = null, IReadOnlyDictionary<string, string>? headers = null)
                    {
                        var key = "rest-" + Guid.NewGuid().ToString("N");
                        var raw = payload is string text ? text : JsonSerializer.Serialize(payload, jsonOptions);
                        if (intent != Guid.Empty) conditionalOperations.Add(intent.ToString("D"));
                        await journal("before-rest-http", new { provider = pair.Key.ToString(), actor, path, key, intent });
                        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path)
                        {
                            Content = new StringContent(raw, Encoding.UTF8, "application/json")
                        };
                        request.Headers.Add("Idempotency-Key", key);
                        if (headers != null) foreach (var header in headers) request.Headers.Add(header.Key, header.Value);
                        using var response = await (caller ?? client).SendAsync(request);
                        var responseText = await response.Content.ReadAsStringAsync();
                        Assert.AreEqual(expected, response.StatusCode, responseText);
                        Assert.IsTrue(response.Headers.TryGetValues("X-OASIS-Operation-Id", out var operationHeaders));
                        var operationId = operationHeaders!.Single();
                        var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                        Assert.AreEqual("settled", operation.State, "Preserve uncertain operations instead of resetting the fixture.");
                        Assert.AreEqual(actor.ToString("D"), operation.UserId);
                        Assert.AreEqual(attempts, operation.ProviderAttempts.Count);
                        Assert.AreEqual(attempts, operation.Receipts.Count);
                        Assert.AreEqual(cost, authority.Settlements[operationId].ActualCostUsd);
                        Assert.AreEqual((long)attempts, authority.Settlements[operationId].Units);
                        Assert.AreEqual(expected == HttpStatusCode.OK ? "succeeded" : "failed", operation.ObservedOutcome);
                        if (attempts == 0) Assert.IsFalse(string.IsNullOrWhiteSpace(operation.NoProviderExecutionReason));
                        else Assert.IsNull(operation.NoProviderExecutionReason);
                        foreach (var receipt in operation.Receipts)
                        {
                            Assert.AreEqual(pair.Key.ToString(), receipt.Provider);
                            Assert.AreEqual("COSMIC-storage-service-attempt-tariff", receipt.CostSource);
                            Assert.AreEqual("owned-storage-v1", receipt.PricingCatalogueVersion);
                            Assert.AreEqual(receipt.ClientRequestId, receipt.ProviderRequestId);
                            Assert.IsNull(receipt.NativeFeeValuations);
                        }
                        using var body = JsonDocument.Parse(responseText);
                        requests++;
                        return new RestResponse(key, raw, operationId, body.RootElement.Clone());
                    }

                    // Both model rejection and action preflight must release the
                    // priced reservation with affirmative zero-execution evidence.
                    await Send("{", Guid.Empty, HttpStatusCode.BadRequest, 0, 0);
                    await Send(new { holon = new { name = "missing intent" } }, Guid.Empty, HttpStatusCode.BadRequest, 0, 0);

                    var initial = await Read(restIds[0]);
                    var stale = StoredRecordJson.Project<Holon>(initial);
                    initial.MetaData["rest-write"] = "first";
                    var firstIntent = Guid.NewGuid();
                    var first = await Send(new
                    {
                        holon = initial, operationId = firstIntent, saveChildren = false, providerType = pair.Key.ToString(),
                        expectedRevisions = new Dictionary<Guid, Guid?> { [initial.Id] = Revision(initial) }
                    }, firstIntent, HttpStatusCode.OK, 2, 0.004m);
                    Assert.IsTrue(first.Body.GetProperty("result").GetProperty("isSaved").GetBoolean());

                    var beforeReplay = (await outbox.GetAsync(first.OperationId, CancellationToken.None)).ProviderAttempts.Count;
                    var started = authority.Started.Count;
                    using (var duplicate = new HttpRequestMessage(HttpMethod.Post, "/api/data/save-holon")
                    {
                        Content = new StringContent(first.Payload, Encoding.UTF8, "application/json")
                    })
                    {
                        duplicate.Headers.Add("Idempotency-Key", first.Key);
                        using var replay = await otherClient.SendAsync(duplicate);
                        Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
                    }
                    Assert.AreEqual(started, authority.Started.Count);
                    Assert.AreEqual(beforeReplay, (await outbox.GetAsync(first.OperationId, CancellationToken.None)).ProviderAttempts.Count);
                    requests++;

                    stale.Name = "stale overwrite";
                    var staleIntent = Guid.NewGuid();
                    var conflict = await Send(new
                    {
                        holon = stale, operationId = staleIntent, saveChildren = false, providerType = pair.Key.ToString(),
                        expectedRevisions = new Dictionary<Guid, Guid?> { [stale.Id] = Revision(stale) }
                    }, staleIntent, HttpStatusCode.Conflict, 2, 0.004m, caller: otherClient);
                    Assert.AreEqual(CosmicConcurrencyException.Code, conflict.Body.GetProperty("result").GetProperty("errorCode").GetString());
                    Assert.AreEqual("REST root", (await Read(restIds[0])).Name);

                    var foreign = await Read(foreignId);
                    var foreignRevision = Revision(foreign);
                    // A new parent has no persisted membership to contradict.
                    // This isolates selected-child ownership from structural
                    // rejection of an incomplete replacement graph.
                    initial = new Holon { Name = "new parent with forged foreign child" };
                    foreign.ParentHolonId = initial.Id;
                    foreign.CreatedByAvatarId = actor; // Payload ownership must not authorize the stored foreign record.
                    initial.Children = new List<IHolon> { foreign };
                    var deniedIntent = Guid.NewGuid();
                    await Send(new
                    {
                        holon = initial, operationId = deniedIntent, saveChildren = true, providerType = pair.Key.ToString(),
                        expectedRevisions = new Dictionary<Guid, Guid?> { [foreign.Id] = foreignRevision }
                    }, deniedIntent, HttpStatusCode.Forbidden, 1, 0.001m);
                    Assert.AreEqual(foreignRevision, Revision(await Read(foreignId)));

                    initial = await Read(restIds[0]);
                    second = await Read(restIds[1]);
                    var oldBatchRoot = StoredRecordJson.Project<Holon>(initial);
                    initial.MetaData["rest-batch"] = second.MetaData["rest-batch"] = "committed";
                    var batchIntent = Guid.NewGuid();
                    await Send(new
                    {
                        holons = new[] { initial, second }, operationId = batchIntent, saveChildren = false,
                        providerType = pair.Key.ToString(), expectedRevisions = new Dictionary<Guid, Guid?>
                        { [initial.Id] = Revision(initial), [second.Id] = Revision(second) }
                    }, batchIntent, HttpStatusCode.OK, 3, 0.005m, "/api/data/save-holons");
                    second = await Read(restIds[1]);
                    var secondRevision = Revision(second);
                    second.MetaData["rest-batch"] = "must not commit";
                    var staleBatchIntent = Guid.NewGuid();
                    await Send(new
                    {
                        holons = new[] { oldBatchRoot, second }, operationId = staleBatchIntent, saveChildren = false,
                        providerType = pair.Key.ToString(), expectedRevisions = new Dictionary<Guid, Guid?>
                        { [oldBatchRoot.Id] = Revision(oldBatchRoot), [second.Id] = secondRevision }
                    }, staleBatchIntent, HttpStatusCode.Conflict, 3, 0.005m, "/api/data/save-holons", otherClient);
                    Assert.AreEqual(secondRevision, Revision(await Read(restIds[1])));
                    Assert.AreEqual("committed", (await Read(restIds[1])).MetaData["rest-batch"].ToString());

                    for (var selection = 0; selection < 3; selection++)
                    {
                        initial = await Read(restIds[0]);
                        child = await Read(restIds[2]);
                        foreign = await Read(foreignId);
                        foreign.ParentHolonId = child.Id;
                        child.Children = new List<IHolon> { foreign };
                        initial.Children = new List<IHolon> { child };
                        initial.MetaData["rest-depth"] = selection;
                        var expected = new Dictionary<Guid, Guid?> { [initial.Id] = Revision(initial) };
                        if (selection != 0) expected[child.Id] = Revision(child);
                        var intent = Guid.NewGuid();
                        await Send(new
                        {
                            holon = initial, operationId = intent, providerType = pair.Key.ToString(), expectedRevisions = expected,
                            saveChildren = selection != 0, recursive = selection != 2, maxChildDepth = selection == 1 ? 1 : 0
                        }, intent, HttpStatusCode.OK, selection == 0 ? 2 : 3, selection == 0 ? 0.004m : 0.005m);
                        Assert.AreEqual(foreignRevision, Revision(await Read(foreignId)));
                        var graph = await Read(restIds[0], true);
                        CollectionAssert.AreEquivalent(new[] { restIds[2] }, graph.Children.Select(value => value.Id).ToArray());
                    }

                    // Complete recursive wire graphs must retain both levels'
                    // stored membership and update the child's values under CAS.
                    var completeGraph = await Read(restIds[0], true);
                    var completeChild = completeGraph.Children.Single();
                    completeChild.MetaData["rest-child-write"] = "committed";
                    var graphIntent = Guid.NewGuid();
                    await Send(new
                    {
                        holon = completeGraph, operationId = graphIntent, saveChildren = true,
                        recursive = true, maxChildDepth = 0, providerType = pair.Key.ToString(),
                        expectedRevisions = new Dictionary<Guid, Guid?>
                        { [completeGraph.Id] = Revision(completeGraph), [completeChild.Id] = Revision(completeChild) }
                    }, graphIntent, HttpStatusCode.OK, 3, 0.005m);
                    Assert.AreEqual("committed", (await Read(restIds[2])).MetaData["rest-child-write"].ToString());

                    // Reuse only this actor's already journalled records. Exact
                    // endpoint enrollment cannot turn a key into a permission grant.
                    initial = await Read(restIds[0]);
                    second = await Read(restIds[1]);
                    var originalRootRevision = Revision(initial);
                    var originalSecondRevision = Revision(second);
                    var retainedChild = await Read(restIds[2]);
                    var retainedChildRevision = Revision(retainedChild);
                    async Task<RestResponse> Delete(Guid id, Guid revision, bool byKey, HttpStatusCode status,
                        int attempts, bool missingIdentity = false, bool missingOperation = false, bool missingRevision = false)
                    {
                        var intent = missingOperation ? Guid.Empty : Guid.NewGuid();
                        var headers = new Dictionary<string, string>();
                        if (!missingOperation) headers.Add("X-COSMIC-Operation-Id", intent.ToString("D"));
                        if (!missingRevision) headers.Add("X-COSMIC-Expected-Revision", revision.ToString("D"));
                        if (byKey && !missingIdentity) headers.Add("X-COSMIC-Holon-Id", id.ToString("D"));
                        var payload = byKey ? (object)new { } : new { id, softDelete = true };
                        var path = byKey ? deletePath + "?softDelete=false&providerType=" + pair.Key : "/api/data/delete-holon";
                        var cost = attempts == 0 ? 0m : attempts == 1 ? 0.001m : 0.004m;
                        return await Send(payload, intent, status, attempts, cost, path,
                            method: HttpMethod.Delete, headers: headers);
                    }
                    await Delete(second.Id, originalSecondRevision, true, HttpStatusCode.BadRequest, 0, missingIdentity: true);
                    await Delete(second.Id, originalSecondRevision, true, HttpStatusCode.BadRequest, 0, missingOperation: true);
                    await Delete(second.Id, originalSecondRevision, true, HttpStatusCode.BadRequest, 0, missingRevision: true);
                    await Delete(initial.Id, originalSecondRevision, true, HttpStatusCode.BadRequest, 1);
                    await Delete(foreignId, Guid.NewGuid(), false, HttpStatusCode.Forbidden, 1);
                    await Delete(initial.Id, Guid.NewGuid(), false, HttpStatusCode.Conflict, 2);
                    await Delete(second.Id, Guid.NewGuid(), true, HttpStatusCode.Conflict, 2);
                    Assert.AreEqual(originalRootRevision, Revision(await Read(initial.Id)));
                    Assert.AreEqual(originalSecondRevision, Revision(await Read(second.Id)));
                    var soft = await Delete(initial.Id, originalRootRevision, false, HttpStatusCode.OK, 2);
                    Assert.IsTrue(soft.Body.GetProperty("result").GetProperty("isDeleted").GetBoolean());
                    var persistedSoft = await pair.Value.LoadHolonAsync(initial.Id, false, true, 0, true, false, 0);
                    Assert.IsTrue(persistedSoft.Result.IsDeleted);
                    Assert.AreEqual(actor, persistedSoft.Result.DeletedByAvatarId);
                    var hard = await Delete(second.Id, originalSecondRevision, true, HttpStatusCode.OK, 2);
                    Assert.IsTrue(hard.Body.GetProperty("result").GetProperty("isDeleted").GetBoolean());
                    Assert.IsTrue(hard.Body.GetProperty("result").GetProperty("metaData").TryGetProperty("COSMIC.CommittedDelta", out _));
                    var persistedHard = await pair.Value.LoadHolonAsync(second.Id, false, true, 0, true, false, 0);
                    Assert.IsNull(persistedHard.Result);
                    var unchangedChild = await Read(restIds[2]);
                    Assert.IsFalse(unchangedChild.IsDeleted, "Root-only deletion must not delete unselected children.");
                    Assert.AreEqual(retainedChildRevision, Revision(unchangedChild));

                }
                finally { await app.StopAsync(); }
            }
        }
        finally
        {
            globals.IsAutoFailOverEnabled = priorFailover;
            globals.IsAutoReplicationEnabled = priorReplication;
            globals.IsAutoLoadBalanceEnabled = priorBalance;
            if (priorCurrent != null) Assert.IsFalse(globals.SetAndActivateCurrentStorageProvider(priorCurrent).IsError);
            globals.OASISDNA = priorRegistryDna;
            OASISDNAManager.OASISDNA = priorDna;
            // Fixture providers stay registered/active until the existing owner
            // deactivates them once; these globals die with this bounded test process.
        }
        return requests;
    }

    private static WebApplication RestHost(ProviderType provider, IOASISStorageProvider storage,
        MongoUsageOutboxStore outbox, Authority authority, string secret, bool transportMode = false, string? deleteKey = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        if (transportMode)
            builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0,
                endpoint => endpoint.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2));
        else builder.WebHost.UseUrls("http://127.0.0.1:0");
        var paths = transportMode ? new[] { "/graphql", "/oasis.web4.DataService/SaveHolon", "/oasis.web4.DataService/DeleteHolon" }
            : new[] { "/api/data/save-holon", "/api/data/save-holons" };
        var routes = paths.Select(path => (Method: "POST", Path: path)).ToList();
        if (deleteKey != null)
        {
            routes.Add(("DELETE", "/api/data/delete-holon"));
            routes.Add(("DELETE", "/api/data/delete-holon-by-providerkey/" + Uri.EscapeDataString(deleteKey)));
        }
        var deployment = new StorageAdmissionDeployment
        {
            NativeProviders = Array.Empty<NativeStorageAdmissionBinding>(), OrdinaryProviders = new[] { provider },
            StorageTariffs = StorageAttemptTariff.SupportedOperations.Select(operation =>
                new StorageAttemptTariff(provider, operation, operation is "LoadHolon" or "LoadHolonByProviderKey" ? 0.001m : 0.003m, "owned-storage-v1")).ToArray(),
            Routes = routes.Select(route => new StorageAdmissionRoute(route.Method, route.Path, new UsageEndpointPolicy
            {
                MeterCategory = "cosmic.write", PricingCatalogueVersion = "owned-storage-v1",
                ReservedUnits = 6, EstimatedCostUsd = 0.018m, FixedCostUsd = 0,
                RequiresProviderMeasurement = true, MaximumProviderAttempts = 6, MaximumNativeFeeCostUsd = 0,
                UsesGraphQlResponseEnvelope = route.Path == "/graphql"
            })).ToArray()
        };
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        json.Converters.Add(new JsonStringEnumConverter());
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PREPARED_STORAGE_ADMISSION_JSON"] = JsonSerializer.Serialize(deployment, json)
        }).Build();
        var select = builder.Services.AddConfiguredStorageAdmission(config, true, _ => null!, () => new[] { storage });
        builder.Services.AddSingleton<IUsageOutboxStore>(outbox);
        builder.Services.AddSingleton<IUsageReservationAuthority>(authority);
        builder.Services.AddSingleton(new Web4UsageLedgerOptions { Service = "WEB4", EndpointPolicy = select });
        builder.Services.AddSingleton<Web4UsageExecution>();
        if (transportMode) AddShippingTransportServices(builder.Services);
        builder.Services.AddControllers(options => options.Filters.Add<UsageResultFilter>())
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                ShippingJson.Configure(options.JsonSerializerOptions);
            })
            .AddApplicationPart(typeof(ShippingDataController).Assembly)
            .ConfigureApplicationPartManager(parts => parts.FeatureProviders.Add(new OnlyShippingDataController()));
        var app = builder.Build();
        app.UseRouting();
        app.Use(async (http, next) =>
        {
            if (AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization, out var header) && header.Parameter != null)
            {
                http.User = ShippingJwt.Validate(header.Parameter, secret, "owned-cql-v1-fixture");
                http.Items["Avatar"] = new Avatar { Id = Guid.Parse(http.User.FindFirstValue("sub")!) };
            }
            await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();
        app.UseConfiguredStorageAdmission();
        app.MapControllers();
        if (transportMode) MapShippingTransports(app);
        return app;
    }
}

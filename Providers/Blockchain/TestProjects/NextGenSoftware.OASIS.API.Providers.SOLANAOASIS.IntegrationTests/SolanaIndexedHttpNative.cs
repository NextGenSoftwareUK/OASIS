using Microsoft.AspNetCore.Http;
using CosmicCqlDemo;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.TestSupport;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public partial class SolanaIndexedQueryNative
{
    private static async Task<object> VerifyNativeHttpAsync(HolonManager manager, SqliteCosmicChainIndexStore index,
        SolanaRepository repository, CosmicChainQuerySource source, CosmicChainQueryReadRequirement requirement, Seed seed, CosmicChainQueryPosition acknowledgedPosition)
    {
        // The live validator and durable index supply the data. This test owns a
        // controlled validated identity and ledger; it does not certify JWT/Mongo.
        var operations = new[] { CosmicIndexedReadOperation.IndexSqlSnapshot, CosmicIndexedReadOperation.SourceGenesis,
            CosmicIndexedReadOperation.SourceSlot, CosmicIndexedReadOperation.SourceBlock, CosmicIndexedReadOperation.SourceTransaction };
        var tariffs = new CosmicIndexedReadTariffPolicy(source, 100,
            operations.Select(op => new CosmicIndexedReadTariff(op, 0.001m, "native-http-v1")));
        var bindings = new NativeHttpBindings(seed.Actor, new(index, repository, tariffs, requirement.MinimumPosition,
            requirement.MaximumLagSlots, requirement.MaximumHeadAge, CosmicQueryReadAccess.OwnerOrPublic(seed.Actor).Result, manager));
        var outbox = DispatchProxy.Create<IUsageOutboxStore, IndexedHttpLedger>();
        var evidence = (IndexedHttpLedger)(object)outbox;
        evidence.Actor = seed.Actor;
        var authority = DispatchProxy.Create<IUsageReservationAuthority, IndexedHttpLedger>();
        var authorityEvidence = (IndexedHttpLedger)(object)authority;
        authorityEvidence.Actor = seed.Actor;
        var options = new Web4UsageLedgerOptions { Service = "WEB4", EndpointPolicy = _ => new UsageEndpointPolicy {
            RequiresProviderMeasurement = true, ReservedUnits = 1, EstimatedCostUsd = 0,
            MaximumProviderAttempts = 100, MaximumNativeFeeCostUsd = 0,
            MeterCategory = "queries", PricingCatalogueVersion = "native-http-v1" } };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            Args = Array.Empty<string>(), ApplicationName = typeof(CqlV1Controller).Assembly.FullName });
        builder.Logging.ClearProviders();
        var serve = Environment.GetEnvironmentVariable("OASIS_SOLANA_INDEXED_SERVE") == "1";
        builder.WebHost.UseUrls(serve ? "http://127.0.0.1:5089" : "http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlIndexedV1Bindings>(bindings);
        builder.Services.AddCqlIndexedReadAdmission();
        builder.Services.AddSingleton<ICqlV1Providers, NoNativeHttpProviders>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new Web4UsageExecution(authority, outbox, options));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);

        await using var app = builder.Build();
        app.UseRouting();
        var ready = false;
        var ingestionSession = new CosmicChainQueryIngestionSession(repository, index, requirement, acknowledgedPosition);
        using var stopScheduling = new CancellationTokenSource();
        Task<OASISResult<bool>>? ingestion = null;
        var advancements = 0;

        async Task<OASISResult<bool>> AdvanceIndexAsync()
        {
            // Each bounded observation uses the shared complete-range proof and
            // atomic publisher. No query triggers this separate ingestion path.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var advanced = await ingestionSession.AdvanceAsync(timeout.Token);
            if (advanced.IsError) return new() { IsError = true, ErrorCode = advanced.ErrorCode, Message = advanced.Message, Exception = advanced.Exception };
            Interlocked.Increment(ref advancements);
            return new(true);
        }

        async Task<OASISResult<bool>> IngestUntilStoppedAsync()
        {
            while (!stopScheduling.IsCancellationRequested)
            {
                var advanced = await AdvanceIndexAsync();
                if (advanced.IsError) return advanced; // Stop, preserve state and surface the original failure.
                try { await Task.Delay(TimeSpan.FromSeconds(5), stopScheduling.Token); }
                catch (OperationCanceledException) when (stopScheduling.IsCancellationRequested) { break; }
            }
            return new(true);
        }
        var interactiveRequests = 0;
        var shutdown = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        app.Use(async (http, next) => {
            if (ingestionSession.FailureCode != null && http.Request.Path.StartsWithSegments("/api/data/query-cql"))
            {
                http.Response.StatusCode = 503;
                await http.Response.WriteAsJsonAsync(new { code = ingestionSession.FailureCode, message = "Ingestion stopped; reconcile before resuming." });
                return;
            }
            if (Volatile.Read(ref ready) && http.Request.Path.StartsWithSegments("/api/data/query-cql") &&
                Interlocked.Increment(ref interactiveRequests) > 100)
            {
                http.Response.StatusCode = 429;
                await http.Response.WriteAsJsonAsync(new { code = "DEMO_QUERY_BUDGET_EXHAUSTED" });
                return;
            }
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, seed.Actor.ToString("D")) }, "native-test-identity"));
            http.Items["Avatar"] = new Avatar { Id = seed.Actor };
            await next();
        });
        // Enroll only the two explicit storage routes. The teaching page, health
        // and shutdown endpoints do not reserve usage or enter storage admission.
        app.UseWhen(http => HttpMethods.IsPost(http.Request.Method) &&
            (http.Request.Path == "/api/data/query-cql/indexed" || http.Request.Path == "/api/data/query-cql/indexed/scalar"), branch => {
            branch.UseMiddleware<Web4UsageLedgerMiddleware>();
            branch.UseMiddleware<PreparedStorageAdmissionMiddleware>();
        });
        app.MapControllers();
        app.MapGet("/", () => Results.Content(IndexedChainDemoPage.Html, "text/html"));
        app.MapGet("/health", () => Results.Ok(new {
            status = Volatile.Read(ref ready) ? "ready" : "verifying", sourceProvider = source.SourceProvider.ToString(),
            executionProvider = source.ExecutionProvider.ToString(), source.NetworkIdentity,
            source.IndexId, schemaVersion = source.SchemaVersion, coverageStart = seed.CoverageStart.ToString(),
            identity = "controlled local demo", sourceWrites = 0,
            ingestion = new { advancements = Volatile.Read(ref advancements), failureCode = ingestionSession.FailureCode,
                checkpointSlot = ingestionSession.AcknowledgedPosition.Slot.ToString(), intervalSeconds = 5,
                profile = "bounded complete declared-range re-observation; no incremental cursor" },
            remainingInteractiveQueries = Math.Max(0, 100 - interactiveRequests)
        }));
        app.MapPost("/demo/shutdown", () => {
            shutdown.TrySetResult();
            return Results.Accepted(value: new { status = "shutdown requested" });
        });

        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Authorization = new("Bearer", "controlled-native-test");
            var pageHtml = await client.GetStringAsync("/");
            StringAssert.Contains(pageHtml, "CQL over Solana");
            StringAssert.Contains(pageHtml, "/api/data/query-cql/indexed/scalar");
            StringAssert.Contains(pageHtml, "CosmicLinq.CreateIndexed");
            var counts = new List<int>();

            foreach (var projected in new[] { false, true })
            foreach (var empty in new[] { false, true })
            {
                // Only bounded public wire intent crosses HTTP. Endpoints, keys,
                // source proofs, prices and the actor grant remain server-owned.
                var body = JsonSerializer.Serialize(new {
                    alias = "solana-native-demo",
                    columns = projected ? new[] { new { alias = "identity", field = "Id" }, new { alias = "label", field = "Name" }, new { alias = "revision", field = "Version" } } : null,
                    query = new { filters = new[] { empty
                        ? new { field = "Name", scalar = "String", @operator = "Equal", value = "does-not-exist" }
                        : new { field = "Id", scalar = "Guid", @operator = "Equal", value = seed.Id.ToString("D") } }, orderBy = "Name", take = 1 } });
                client.DefaultRequestHeaders.Remove("Idempotency-Key");
                client.DefaultRequestHeaders.Add("Idempotency-Key", (projected ? "native-index-projection-" : "native-index-") + (empty ? "empty" : "rows"));
                using var response = await client.PostAsync("/api/data/query-cql/indexed", new StringContent(body, Encoding.UTF8, "application/json"));
                var text = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
                var operation = evidence.Operations[operationId];
                var settlement = authorityEvidence.Settlements.Single(s => s.OperationId == operationId);
                using var json = JsonDocument.Parse(text);
                var page = json.RootElement.GetProperty("result");
                Assert.AreEqual(empty ? 0 : 1, page.GetProperty("count").GetInt32());
                Assert.AreEqual("SolanaOASIS", page.GetProperty("provider").GetString());
                Assert.AreEqual("SQLLiteDBOASIS", page.GetProperty("executionProvider").GetString());
                Assert.AreEqual("trusted-source-observation", page.GetProperty("assurance").GetString());
                Assert.AreEqual(seed.CoverageStart.ToString(), page.GetProperty("checkpoint").GetProperty("completeFromSlot").GetString());
                Assert.IsTrue(long.Parse(page.GetProperty("finalizedHead").GetProperty("position").GetProperty("slot").GetString()!) >=
                    long.Parse(page.GetProperty("checkpoint").GetProperty("position").GetProperty("slot").GetString()!));
                Assert.AreEqual(projected ? "indexed-projection" : "indexed", page.GetProperty("mode").GetString());
                if (projected)
                {
                    Assert.AreEqual("label", page.GetProperty("columns")[1].GetProperty("alias").GetString());
                    if (!empty)
                    {
                        Assert.AreEqual(seed.Id.ToString("D"), page.GetProperty("rows")[0][0].GetString());
                        Assert.AreEqual("COSMIC native chain coffee", page.GetProperty("rows")[0][1].GetString());
                        Assert.AreEqual(1, page.GetProperty("rows")[0][2].GetInt32());
                        Assert.AreEqual(seed.Signature, page.GetProperty("receipts")[0].GetProperty("commitReference").GetString());
                    }
                }
                if (!empty && !projected)
                {
                    Assert.AreEqual(seed.Id.ToString("D"), page.GetProperty("rows")[0].GetProperty("id").GetString());
                    Assert.AreEqual(seed.Signature, page.GetProperty("receipts")[0].GetProperty("commitReference").GetString());
                    Assert.AreEqual("COSMIC native chain coffee", page.GetProperty("rows")[0].GetProperty("name").GetString());
                }

                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual("succeeded", operation.ObservedOutcome);
                Assert.IsNull(operation.NoProviderExecutionReason);
                Assert.AreEqual(operation.ProviderAttempts.Count, operation.Receipts.Count);
                Assert.AreEqual(1, operation.Receipts.Count(r => r.Provider == ProviderType.SQLLiteDBOASIS.ToString()));
                Assert.IsTrue(operation.Receipts.Count(r => r.Provider == ProviderType.SolanaOASIS.ToString()) >= (empty ? 4 : 7));
                Assert.AreEqual(operation.Receipts.Count, operation.Receipts.Select(r => r.ClientRequestId).Distinct().Count());
                foreach (var receipt in operation.Receipts) StringAssert.Contains(receipt.Model, tariffs.BindingHash);
                Assert.AreEqual((long)operation.Receipts.Count, settlement.Units);
                Assert.AreEqual(operation.Receipts.Count * 0.001m, settlement.ActualCostUsd);
                var reservation = authorityEvidence.Authorizations.Last();
                Assert.AreEqual(100L, reservation.RequestedUnits);
                Assert.AreEqual(0.1m, reservation.EstimatedCostUsd);
                counts.Add(operation.Receipts.Count);

                // An identical idempotency key is rejected before another read.
                var attempts = evidence.Operations.Values.Sum(op => op.ProviderAttempts.Count);
                using var replay = await client.PostAsync("/api/data/query-cql/indexed", new StringContent(body, Encoding.UTF8, "application/json"));
                Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
                Assert.AreEqual(attempts, evidence.Operations.Values.Sum(op => op.ProviderAttempts.Count));
            }

            var scalarCounts = new List<int>();
            foreach (var scalarOperation in new[] { "Count", "LongCount", "Any" })
            foreach (var window in new[] { "whole", "filtered-empty", "zero" })
            {
                var scalarBody = JsonSerializer.Serialize(new {
                    alias = "solana-native-demo",
                    query = new { operation = scalarOperation, orderBy = "Id",
                        filters = new[] { new { field = "Name", scalar = "String", @operator = "Equal",
                            value = window == "filtered-empty" ? "does-not-exist" : "COSMIC native chain coffee" } },
                        take = window == "zero" ? (int?)0 : null }
                });
                client.DefaultRequestHeaders.Remove("Idempotency-Key");
                client.DefaultRequestHeaders.Add("Idempotency-Key", "native-index-scalar-" + scalarOperation + "-" + window);
                using var scalarResponse = await client.PostAsync("/api/data/query-cql/indexed/scalar",
                    new StringContent(scalarBody, Encoding.UTF8, "application/json"));
                var scalarText = await scalarResponse.Content.ReadAsStringAsync();
                Assert.AreEqual(HttpStatusCode.OK, scalarResponse.StatusCode, scalarText);
                using var scalarJson = JsonDocument.Parse(scalarText);
                var scalarResult = scalarJson.RootElement.GetProperty("result");
                Assert.AreEqual("indexed-scalar", scalarResult.GetProperty("mode").GetString());
                Assert.AreEqual("authenticated-index-snapshot", scalarResult.GetProperty("assurance").GetString());
                var expectedCount = window == "whole" ? 1 : 0;
                if (scalarOperation == "Any") Assert.AreEqual(expectedCount != 0, scalarResult.GetProperty("value").GetBoolean());
                else if (scalarOperation == "LongCount") Assert.AreEqual(expectedCount.ToString(), scalarResult.GetProperty("value").GetString());
                else Assert.AreEqual(expectedCount, scalarResult.GetProperty("value").GetInt32());
                Assert.IsFalse(scalarResult.TryGetProperty("receipts", out _));
                Assert.IsFalse(scalarResult.TryGetProperty("rows", out _));
                Assert.IsFalse(scalarText.Contains("historyHash", StringComparison.OrdinalIgnoreCase));
                var scalarId = scalarResponse.Headers.GetValues("X-OASIS-Operation-Id").Single();
                var scalarEvidence = evidence.Operations[scalarId];
                var scalarSettlement = authorityEvidence.Settlements.Single(value => value.OperationId == scalarId);
                Assert.AreEqual("settled", scalarEvidence.State);
                Assert.AreEqual(1, scalarEvidence.Receipts.Count(value => value.Provider == ProviderType.SQLLiteDBOASIS.ToString()));
                Assert.AreEqual(scalarEvidence.ProviderAttempts.Count, scalarEvidence.Receipts.Count);
                Assert.IsTrue(scalarEvidence.Receipts.Count >= 4);
                Assert.AreEqual((long)scalarEvidence.Receipts.Count, scalarSettlement.Units);
                Assert.AreEqual(scalarEvidence.Receipts.Count * 0.001m, scalarSettlement.ActualCostUsd);
                scalarCounts.Add(scalarEvidence.Receipts.Count);
                var scalarAttempts = evidence.Operations.Values.Sum(value => value.ProviderAttempts.Count);
                using var scalarReplay = await client.PostAsync("/api/data/query-cql/indexed/scalar",
                    new StringContent(scalarBody, Encoding.UTF8, "application/json"));
                Assert.AreEqual(HttpStatusCode.Conflict, scalarReplay.StatusCode);
                Assert.AreEqual(scalarAttempts, evidence.Operations.Values.Sum(value => value.ProviderAttempts.Count));
            }

            var metadataAccepted = 0;
            var metadataDenied = 0;
            if (source.SchemaVersion == 3)
            foreach (var metadataCase in new[] { "rows", "LongCount", "Any", "zero", "empty", "projection", "private", "case", "envelope" })
            {
                var deniedRoot = metadataCase is "private" or "case" or "envelope";
                var scalar = metadataCase is "LongCount" or "Any" or "zero" or "empty";
                var root = metadataCase == "private" ? "privateNote" : metadataCase == "case" ? "status" : metadataCase == "envelope" ? "__oasis_enc__" : "Status";
                var filters = new object[] {
                    new { metadataPath = new[] { root, "category" }, scalar = "String", @operator = "Equal", value = metadataCase == "empty" ? "absent" : "coffee" },
                    new { metadataPath = new[] { root, "amount" }, scalar = "Integer", @operator = "GreaterThan", value = 9007199254740992L }
                };
                var body = scalar
                    ? JsonSerializer.Serialize(new { alias = "solana-native-demo", query = new {
                        operation = metadataCase == "Any" ? "Any" : "LongCount", filters, orderBy = "Id", take = metadataCase == "zero" ? (int?)0 : null } })
                    : JsonSerializer.Serialize(new { alias = "solana-native-demo", query = new { filters, orderBy = "Name", take = 1 },
                        columns = metadataCase == "projection" ? new[] { new { alias = "label", field = "Name" } } : null });
                var route = scalar ? "/api/data/query-cql/indexed/scalar" : "/api/data/query-cql/indexed";
                client.DefaultRequestHeaders.Remove("Idempotency-Key");
                client.DefaultRequestHeaders.Add("Idempotency-Key", "native-metadata-" + metadataCase);
                var authorizationsBefore = authorityEvidence.Authorizations.Count;
                var attemptsBefore = evidence.Operations.Values.Sum(value => value.ProviderAttempts.Count);
                using var response = await client.PostAsync(route, new StringContent(body, Encoding.UTF8, "application/json"));
                var text = await response.Content.ReadAsStringAsync();
                if (deniedRoot)
                {
                    Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, text);
                    StringAssert.Contains(text, "CQL_CAPABILITY_UNSUPPORTED");
                    Assert.AreEqual(authorizationsBefore, authorityEvidence.Authorizations.Count);
                    Assert.AreEqual(attemptsBefore, evidence.Operations.Values.Sum(value => value.ProviderAttempts.Count));
                    metadataDenied++;continue;
                }
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
                Assert.IsFalse(text.Contains("privateNote", StringComparison.Ordinal));
                using var json = JsonDocument.Parse(text);var page = json.RootElement.GetProperty("result");
                Assert.AreEqual(3, page.GetProperty("checkpoint").GetProperty("source").GetProperty("schemaVersion").GetInt32());
                if (metadataCase == "Any") Assert.IsTrue(page.GetProperty("value").GetBoolean());
                else if (scalar) Assert.AreEqual(metadataCase is "zero" or "empty" ? "0" : "1", page.GetProperty("value").GetString());
                else Assert.AreEqual(1, page.GetProperty("rows").GetArrayLength());
                var id = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
                var operation = evidence.Operations[id];
                var settlement = authorityEvidence.Settlements.Single(value => value.OperationId == id);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(operation.ProviderAttempts.Count, operation.Receipts.Count);
                Assert.AreEqual((long)operation.Receipts.Count, settlement.Units);
                Assert.AreEqual(operation.Receipts.Count * 0.001m, settlement.ActualCostUsd);
                metadataAccepted++;
                var attempts = evidence.Operations.Values.Sum(value => value.ProviderAttempts.Count);
                using var replay = await client.PostAsync(route, new StringContent(body, Encoding.UTF8, "application/json"));
                Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
                Assert.AreEqual(attempts, evidence.Operations.Values.Sum(value => value.ProviderAttempts.Count));
            }

            var beforeRejected = evidence.Operations.Values.Sum(op => op.ProviderAttempts.Count);
            client.DefaultRequestHeaders.Remove("Idempotency-Key");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "native-index-unsupported");
            using var rejected = await client.PostAsync("/api/data/query-cql/indexed", new StringContent(
                "{\"alias\":\"solana-native-demo\",\"query\":{\"graph\":{},\"take\":1}}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.AreEqual(beforeRejected, evidence.Operations.Values.Sum(op => op.ProviderAttempts.Count));
            Assert.AreEqual(13 + metadataAccepted, evidence.Operations.Count);
            Assert.AreEqual(13 + metadataAccepted, authorityEvidence.Settlements.Count);
            // Native acceptance finishes before optional interactive queries become available.
            // Shutdown releases the index and runner-owned validator without deleting source state.
            var initialAdvance = await AdvanceIndexAsync();
            Assert.IsFalse(initialAdvance.IsError, initialAdvance.ErrorCode + ": " + initialAdvance.Message);
            Volatile.Write(ref ready, true);
            if (serve)
            {
                Console.WriteLine("CQL Solana demo ready: " + address);
                ingestion = IngestUntilStoppedAsync();
                var ended = await Task.WhenAny(shutdown.Task, ingestion);
                if (ended == ingestion)
                {
                    var stoppedIngestion = await ingestion;
                    Assert.IsFalse(stoppedIngestion.IsError, stoppedIngestion.ErrorCode + ": " + stoppedIngestion.Message);
                }
            }
            else
            {
                var secondAdvance = await AdvanceIndexAsync();
                Assert.IsFalse(secondAdvance.IsError, secondAdvance.ErrorCode + ": " + secondAdvance.Message);
                Assert.IsTrue(ingestionSession.AcknowledgedPosition.Slot >= acknowledgedPosition.Slot);
                using var stopped = await client.PostAsync("/demo/shutdown", null);
                Assert.AreEqual(HttpStatusCode.Accepted, stopped.StatusCode);
                await shutdown.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            return new { requests = 27 + metadataAccepted * 2 + metadataDenied, accepted = 13 + metadataAccepted, rejectedReplays = 13 + metadataAccepted,
                metadataAccepted, metadataDenied,
                interactiveRequests, interactive = serve, ingestionAdvancements = advancements,
                ingestionCheckpoint = ingestionSession.AcknowledgedPosition.Slot.ToString(), unsupportedBeforeExecution = 1 + metadataDenied,
                physicalReadCounts = counts, scalarPhysicalReadCounts = scalarCounts, sourceTransport = "live retained local validator", ledger = "controlled fixture",
                authentication = "controlled validated identity", sourceWrites = 0 };
        }
        finally
        {
            stopScheduling.Cancel();
            try
            {
                // Finish any in-flight publication before disposing the index or validator.
                if (ingestion != null)
                {
                    var finalIngestion = await ingestion;
                    Assert.IsFalse(finalIngestion.IsError, finalIngestion.ErrorCode + ": " + finalIngestion.Message);
                }
            }
            finally { await app.StopAsync(); }
        }
    }

    private sealed class NativeHttpBindings(Guid actor, CqlIndexedV1Binding binding) : ICqlIndexedV1Bindings
    {
        public OASISResult<CqlIndexedV1Binding> Resolve(Guid authenticatedActor, string alias)
        {
            Assert.AreEqual(actor, authenticatedActor);
            Assert.AreEqual("solana-native-demo", alias);
            return new(binding);
        }
    }

    private sealed class NoNativeHttpProviders : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => throw new AssertFailedException("Indexed HTTP must use its frozen source/index binding.");
        public HolonManager CreateManager(IOASISStorageProvider provider) => throw new AssertFailedException("Use the frozen server manager.");
    }
}

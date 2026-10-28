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
        SolanaRepository repository, CosmicChainQuerySource source, CosmicChainQueryReadRequirement requirement, Seed seed)
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
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlIndexedV1Bindings>(bindings);
        builder.Services.AddCqlIndexedReadAdmission();
        builder.Services.AddSingleton<ICqlV1Providers, NoNativeHttpProviders>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new Web4UsageExecution(authority, outbox, options));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);

        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (http, next) => {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, seed.Actor.ToString("D")) }, "native-test-identity"));
            http.Items["Avatar"] = new Avatar { Id = seed.Actor };
            await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();
        app.UseMiddleware<PreparedStorageAdmissionMiddleware>();
        app.MapControllers();

        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Authorization = new("Bearer", "controlled-native-test");
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

            var beforeRejected = evidence.Operations.Values.Sum(op => op.ProviderAttempts.Count);
            client.DefaultRequestHeaders.Remove("Idempotency-Key");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "native-index-unsupported");
            using var rejected = await client.PostAsync("/api/data/query-cql/indexed", new StringContent(
                "{\"alias\":\"solana-native-demo\",\"query\":{\"graph\":{},\"take\":1}}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.BadRequest, rejected.StatusCode);
            Assert.AreEqual(beforeRejected, evidence.Operations.Values.Sum(op => op.ProviderAttempts.Count));
            Assert.AreEqual(4, evidence.Operations.Count);
            Assert.AreEqual(4, authorityEvidence.Settlements.Count);
            return new { requests = 9, accepted = 4, rejectedReplays = 4, unsupportedBeforeExecution = 1,
                physicalReadCounts = counts, sourceTransport = "live retained local validator", ledger = "controlled fixture",
                authentication = "controlled validated identity", sourceWrites = 0 };
        }
        finally { await app.StopAsync(); }
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

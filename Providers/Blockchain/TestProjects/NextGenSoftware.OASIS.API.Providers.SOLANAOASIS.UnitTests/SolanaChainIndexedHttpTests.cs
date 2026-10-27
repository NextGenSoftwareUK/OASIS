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
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

public partial class SolanaChainIndexedBillingTests
{
    private static async Task VerifyHttpAsync(HolonManager manager, SqliteCosmicChainIndexStore index,
        SolanaRepository repository, CosmicChainQuerySource source, CosmicChainQueryReadRequirement requirement,
        Guid actor, SolanaChainIndexObservationTests.RangeRpc rpc, bool empty, string mode)
    {
        // This host executes shipping MVC and both stock admission middlewares.
        // SQLite and canonical source decoding are real; identity, RPC and ledger
        // persistence are controlled so this cannot certify production admission.
        var tariffs = new CosmicIndexedReadTariffPolicy(source, 100,
            CosmicIndexedReadTariffPolicy.Required.Select(op => new CosmicIndexedReadTariff(op, 0.001m, "http-index-v1")));
        var bindings = new HttpBindings(actor, new CqlIndexedV1Binding(index, repository, tariffs,
            requirement.MinimumPosition, 0, TimeSpan.FromMinutes(1), CosmicQueryReadAccess.OwnerOrPublic(actor).Result, manager));
        var outbox = DispatchProxy.Create<IUsageOutboxStore, HttpLedger>();
        var ledger = (HttpLedger)(object)outbox;
        ledger.Actor = actor;
        var authority = DispatchProxy.Create<IUsageReservationAuthority, HttpLedger>();
        var authorityState = (HttpLedger)(object)authority;
        authorityState.Actor = actor;
        var options = new Web4UsageLedgerOptions { Service = "WEB4", EndpointPolicy = _ => new UsageEndpointPolicy {
            RequiresProviderMeasurement = true, ReservedUnits = 1, EstimatedCostUsd = 0,
            MaximumProviderAttempts = 100, MaximumNativeFeeCostUsd = 0,
            MeterCategory = "queries", PricingCatalogueVersion = "http-index-v1" } };

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions {
            Args = Array.Empty<string>(), ApplicationName = typeof(CqlV1Controller).Assembly.FullName });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<ICqlIndexedV1Bindings>(bindings);
        builder.Services.AddCqlIndexedReadAdmission();
        builder.Services.AddSingleton<ICqlV1Providers, NoNativeProviders>();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new Web4UsageExecution(authority, outbox, options));
        builder.Services.AddSingleton<CqlV1ResourceFilter>();
        builder.Services.AddControllers().AddApplicationPart(typeof(CqlV1Controller).Assembly);

        await using var app = builder.Build();
        app.UseRouting();
        app.Use(async (http, next) => {
            http.User = new ClaimsPrincipal(new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.NameIdentifier, actor.ToString("D")) }, "controlled-fixture"));
            http.Items["Avatar"] = new Avatar { Id = actor };
            await next();
        });
        app.UseMiddleware<Web4UsageLedgerMiddleware>();
        app.UseMiddleware<PreparedStorageAdmissionMiddleware>();
        app.MapControllers();
        rpc.Calls = 0;
        rpc.Mode = mode == "http-missing" ? "missing" : mode == "http-network" ? "network" : "";
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.Authorization = new("Bearer", "controlled-fixture");
            client.DefaultRequestHeaders.Add("Idempotency-Key", "real-index-http");
            var name = empty ? "missing" : "latest";
            var body = "{\"alias\":\"solana-demo\",\"query\":{\"filters\":[{\"field\":\"Name\",\"operator\":\"Equal\",\"value\":\"" + name + "\"}],\"orderBy\":\"Name\",\"take\":1}}";
            using var response = await client.PostAsync("/api/data/query-cql/indexed", new StringContent(body, Encoding.UTF8, "application/json"));
            var text = await response.Content.ReadAsStringAsync();
            if (mode is "http-missing" or "http-network")
            {
                Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode, text);
                Assert.IsFalse(text.Contains("\"rows\""), text);
                Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
                var failed = ledger.Operations.Values.Single();
                Assert.AreEqual("failed", failed.ObservedOutcome);
                Assert.IsNull(failed.NoProviderExecutionReason);
                Assert.AreEqual(failed.ProviderAttempts.Count, failed.Receipts.Count);
                Assert.AreEqual(1, failed.Receipts.Count(r => r.Provider == ProviderType.SQLLiteDBOASIS.ToString()));
                Assert.AreEqual(rpc.Calls, failed.Receipts.Count(r => r.Provider == ProviderType.SolanaOASIS.ToString()));
                Assert.IsTrue(rpc.Calls > 0);
                Assert.AreEqual((long)failed.Receipts.Count, authorityState.Settlements.Single().Units);
                Assert.AreEqual(failed.Receipts.Count * 0.001m, authorityState.Settlements.Single().ActualCostUsd);
                Assert.AreEqual(0, rpc.Writes);
                return;
            }
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, text);
            Assert.AreEqual("settled", response.Headers.GetValues("X-OASIS-Usage-Settlement").Single());
            using var json = JsonDocument.Parse(text);
            var page = json.RootElement.GetProperty("result");
            Assert.AreEqual(empty ? 0 : 1, page.GetProperty("count").GetInt32());
            Assert.AreEqual("SolanaOASIS", page.GetProperty("provider").GetString());
            Assert.AreEqual("SQLLiteDBOASIS", page.GetProperty("executionProvider").GetString());
            Assert.AreEqual("10", page.GetProperty("checkpoint").GetProperty("position").GetProperty("slot").GetString());
            Assert.AreEqual("trusted-source-observation", page.GetProperty("assurance").GetString());
            if (!empty)
            {
                Assert.AreEqual("latest", page.GetProperty("rows")[0].GetProperty("name").GetString());
                Assert.AreEqual("new", page.GetProperty("receipts")[0].GetProperty("commitReference").GetString());
            }

            var operation = ledger.Operations.Values.Single();
            Assert.AreEqual("settled", operation.State);
            Assert.AreEqual(empty ? 4 : 7, operation.ProviderAttempts.Count);
            Assert.AreEqual(operation.ProviderAttempts.Count, operation.Receipts.Count);
            Assert.AreEqual(1, operation.Receipts.Count(r => r.Provider == ProviderType.SQLLiteDBOASIS.ToString()));
            Assert.AreEqual(rpc.Calls, operation.Receipts.Count(r => r.Provider == ProviderType.SolanaOASIS.ToString()));
            var settlement = authorityState.Settlements.Single();
            Assert.AreEqual(empty ? 4L : 7L, settlement.Units);
            Assert.AreEqual(empty ? 0.004m : 0.007m, settlement.ActualCostUsd);
            Assert.AreEqual(100L, authorityState.Authorizations.Single().RequestedUnits);
            Assert.AreEqual(0.1m, authorityState.Authorizations.Single().EstimatedCostUsd);
            Assert.AreEqual(0, rpc.Writes);

            // Replay is rejected by the stock outbox before another SQLite/RPC read.
            var calls = rpc.Calls;
            using var replay = await client.PostAsync("/api/data/query-cql/indexed", new StringContent(body, Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.Conflict, replay.StatusCode);
            Assert.AreEqual(calls, rpc.Calls);
            Assert.AreEqual(1, ledger.Operations.Count);
            Assert.AreEqual(1, authorityState.Settlements.Count);
        }
        finally { await app.StopAsync(); }
    }

    private sealed class HttpBindings(Guid actor, CqlIndexedV1Binding binding) : ICqlIndexedV1Bindings
    {
        public OASISResult<CqlIndexedV1Binding> Resolve(Guid authenticatedActor, string alias)
        {
            Assert.AreEqual(actor, authenticatedActor);
            Assert.AreEqual("solana-demo", alias);
            return new(binding);
        }
    }

    private sealed class NoNativeProviders : ICqlV1Providers
    {
        public IOASISStorageProvider Resolve(ProviderType? requested) => throw new AssertFailedException("Indexed HTTP must not activate native provider lookup.");
        public HolonManager CreateManager(IOASISStorageProvider provider) => throw new AssertFailedException("Use the frozen server manager.");
    }

    // Strict controlled ledger: unexpected calls fail rather than manufacturing a
    // successful acknowledgement. Real Mongo durability is a separate acceptance gate.
    public class HttpLedger : DispatchProxy
    {
        public Guid Actor;
        public readonly Dictionary<string, UsageOutboxOperation> Operations = new();
        public readonly List<UsageAuthorizationRequest> Authorizations = new();
        public readonly List<UsageSettlementRequest> Settlements = new();

        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            switch (method!.Name)
            {
                case nameof(IUsageReservationAuthority.AuthorizeUsageAsync):
                    var authorization = (UsageAuthorizationRequest)args![1]!;
                    Authorizations.Add(authorization);
                    return Task.FromResult(new SubscriptionAuthorizationResult { Allowed = true, UserId = Actor.ToString("D"),
                        OperationId = authorization.OperationId, Status = "authorized", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5) });
                case nameof(IUsageReservationAuthority.StartUsageAsync):
                    return Task.FromResult(new UsageStartResult { Started = true, OperationId = ((UsageStartRequest)args![0]!).OperationId });
                case nameof(IUsageReservationAuthority.SettleUsageAsync):
                    var settlement = (UsageSettlementRequest)args![0]!;
                    Settlements.Add(settlement);
                    return Task.FromResult(new UsageSettlementResult { Settled = true, OperationId = settlement.OperationId });
                case nameof(IUsageOutboxStore.TryBeginAsync):
                    var operation = (UsageOutboxOperation)args![0]!;
                    return Task.FromResult(Operations.TryAdd(operation.Id, operation));
                case nameof(IUsageOutboxStore.GetAsync):
                    return Task.FromResult(Operations.GetValueOrDefault((string)args![0]!));
            }
            var op = Operations[(string)args![0]!];
            switch (method.Name)
            {
                case nameof(IUsageOutboxStore.BeginProviderAttemptAsync):
                    Assert.AreEqual(op.ExecutionOwner, args[1]);
                    var attempt = (UsageProviderAttempt)args[2]!;
                    Assert.IsFalse(op.ProviderAttempts.Any(p => p.ClientRequestId == attempt.ClientRequestId));
                    op.ProviderAttempts.Add(attempt); op.EvidenceRevision++;
                    return Task.CompletedTask;
                case nameof(IUsageOutboxStore.AppendReceiptAsync):
                    Assert.AreEqual(op.ExecutionOwner, args[1]);
                    var receipt = (UsageProviderReceipt)args[2]!;
                    receipt.Validate();
                    Assert.IsTrue(op.ProviderAttempts.Any(p => p.ClientRequestId == receipt.ClientRequestId && p.Provider == receipt.Provider));
                    op.Receipts.Add(UsageReceiptAccounting.Snapshot(receipt)); op.EvidenceRevision++;
                    return Task.CompletedTask;
                case nameof(IUsageOutboxStore.RecordObservedOutcomeAsync):
                    op.ObservedOutcome = (string)args[2]!;
                    return Task.FromResult(op);
                case nameof(IUsageOutboxStore.EnqueueAsync):
                    Assert.AreEqual(op.EvidenceRevision, (long)args[3]!);
                    op.Settlement = (UsageSettlementRequest)args[2]!; op.State = "pending";
                    return Task.CompletedTask;
                case nameof(IUsageOutboxStore.ClaimOperationAsync): return Task.FromResult(op);
                case nameof(IUsageOutboxStore.AcknowledgeAsync): op.State = "settled"; return Task.CompletedTask;
                default: throw new AssertFailedException("Unexpected ledger call: " + method.Name);
            }
        }
    }
}

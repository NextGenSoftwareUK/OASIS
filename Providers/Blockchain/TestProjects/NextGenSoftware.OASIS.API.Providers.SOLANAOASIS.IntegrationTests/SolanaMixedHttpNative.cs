using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests;

public partial class SolanaIndexedQueryNative
{
    private static async Task VerifyMixedHttpAsync(HolonManager manager, ICosmicQueryStorageProvider mongo,
        ICosmicChainQueryIndexReader index, ICosmicChainQuerySourceObserver observer,
        CosmicChainQueryReadRequirement requirement, Seed seed, CosmicChainQueryPosition acknowledgedPosition)
    {
        Task<OASISResult<CosmicFederationResult>> Execute(bool matching, CancellationToken cancellation)
            => CosmicCqlDemo.MixedFederationWalkthrough.RunAsync(manager, mongo, index, observer,
                requirement, seed.Actor, seed.Id,
                matching ? "COSMIC native chain coffee" : "No matching synthetic coffee", cancellation);

        await using (var app = CosmicCqlDemo.MixedFederationApiHost.Build(Execute, "http://127.0.0.1:0"))
        {
            await app.StartAsync();
            try
            {
                var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
                using var http = new HttpClient { BaseAddress = new Uri(address) };
                StringAssert.Contains(await http.GetStringAsync("/"), "Query matching copies");
                using var invalid = await http.PostAsJsonAsync("/api/cql/mixed", new { preset = "foreign" });
                Assert.AreEqual(HttpStatusCode.BadRequest, invalid.StatusCode);
                using var forged = await http.PostAsJsonAsync("/api/cql/mixed", new { preset = "matching", actorId = Guid.NewGuid() });
                Assert.AreEqual(HttpStatusCode.BadRequest, forged.StatusCode);

                foreach (var preset in new[] { "matching", "empty" })
                {
                    using var response = await http.PostAsJsonAsync("/api/cql/mixed", new { preset });
                    var text = await response.Content.ReadAsStringAsync();
                    Assert.IsTrue(response.IsSuccessStatusCode, text);
                    using var body = JsonDocument.Parse(text);
                    Assert.AreEqual(preset == "matching" ? 2 : 0, body.RootElement.GetProperty("count").GetInt32());
                    var assurance = body.RootElement.GetProperty("indexedReads").EnumerateArray().Single();
                    Assert.AreEqual("SolanaOASIS", assurance.GetProperty("sourceProvider").GetString());
                    Assert.AreEqual("SQLLiteDBOASIS", assurance.GetProperty("executionProvider").GetString());
                    Assert.AreEqual(seed.IndexId, assurance.GetProperty("indexId").GetGuid());
                    Assert.IsTrue(ulong.Parse(assurance.GetProperty("finalizedHead").GetString()!) >=
                        ulong.Parse(assurance.GetProperty("checkpoint").GetString()!));
                    if (preset == "matching")
                    {
                        var rows = body.RootElement.GetProperty("rows").EnumerateArray().ToArray();
                        Assert.AreEqual(1, rows.Count(row => row.GetProperty("provider").GetString() == "MongoDBOASIS"));
                        var chain = rows.Single(row => row.GetProperty("provider").GetString() == "SolanaOASIS");
                        Assert.AreEqual(seed.Signature, chain.GetProperty("receipt").GetString());
                        Assert.AreEqual(seed.VersionId, chain.GetProperty("versionId").GetGuid());
                    }
                }
            }
            finally { await app.StopAsync(); }
        }

        if (Environment.GetEnvironmentVariable("OASIS_SOLANA_MIXED_SERVE") != "1") return;
        Assert.IsInstanceOfType<ICosmicChainQueryIndexPublisher>(index);
        var ingestion = new CosmicChainQueryIngestionSession(observer,
            (ICosmicChainQueryIndexPublisher)index, requirement, acknowledgedPosition);
        using var stop = new CancellationTokenSource();
        OASISResult<CosmicFederationResult> Failed() => new()
        { IsError = true, ErrorCode = ingestion.FailureCode, Message = "Ingestion stopped; reconcile before resuming." };
        await using var interactive = CosmicCqlDemo.MixedFederationApiHost.Build((matching, token) =>
            ingestion.FailureCode == null ? Execute(matching, token) : Task.FromResult(Failed()));

        async Task Advance()
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var advanced = await ingestion.AdvanceAsync(deadline.Token);
            Assert.IsFalse(advanced.IsError, advanced.ErrorCode + ": " + advanced.Message);
        }
        await Advance();
        await interactive.StartAsync();
        async Task Schedule()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
            try { while (await timer.WaitForNextTickAsync(stop.Token)) await Advance(); }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            catch { interactive.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication(); throw; }
        }
        var scheduling = Schedule();
        try
        {
            Console.WriteLine("Mixed Mongo + Solana demo: http://127.0.0.1:5091/ ; page shutdown verifies Mongo cleanup. No chain writes.");
            await interactive.WaitForShutdownAsync();
        }
        finally
        {
            stop.Cancel();
            try { await scheduling; }
            finally { await interactive.StopAsync(); }
        }
    }
}

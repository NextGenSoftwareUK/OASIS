using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Provider = NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [TestMethod]
    public async Task OwnedNativeV2RoutesBalancesReplicatesAndFailsOverWithoutStaleAbsenceFallback()
    {
        const string uri = "bolt://127.0.0.1:17687";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults", "Neo4jCurrent"), StringComparison.OrdinalIgnoreCase));
        var database = Path.Combine(work, "routing.db");
        Assert.IsFalse(File.Exists(database), "Retained SQLite evidence exists; refusing reset.");
        using var raw = GraphDatabase.Driver(uri, AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using (var preflight = raw.AsyncSession())
            Assert.AreEqual(0L, (await (await preflight.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var neo = new Provider(uri, "neo4j", "oasis-local-test-only");
        var sqlite = new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        var id = Guid.NewGuid(); var verified = false;
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await neo.SaveHolonAsync(new Holon { Id = id, Name = "distinct-neo" }, saveChildren:false)).IsError);
            Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon { Id = id, Name = "distinct-sqlite" }, saveChildren:false)).IsError);
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig = new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig { PlanType = "Gold" } } };
            var manager = new Core.Managers.ProviderManager(null, dna) { IsAutoLoadBalanceEnabled = true,
                IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false };
            manager.RegisterProvider(neo); manager.RegisterProvider(sqlite);
            var providers = new[] { new EnumValue<ProviderType>(ProviderType.Neo4jOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            var router = new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var counts = new Dictionary<string, int>();
            for (var index = 0; index < 6; index++)
            {
                var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id, LoadChildren = false };
                var read = index % 2 == 0 ? router.RouteRequest<IHolon>(request, LoadBalancingStrategy.RoundRobin)
                    : await router.RouteRequestAsync<IHolon>(request, LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError, read.Message); counts[read.Result.Name] = counts.GetValueOrDefault(read.Result.Name) + 1;
            }
            Assert.AreEqual(3, counts["distinct-neo"]); Assert.AreEqual(3, counts["distinct-sqlite"]);
            foreach (var strategy in Enum.GetValues<LoadBalancingStrategy>())
            foreach (var asynchronous in new[] { false, true })
            {
                var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id, LoadChildren = false };
                var read = asynchronous ? await router.RouteRequestAsync<IHolon>(request, strategy) : router.RouteRequest<IHolon>(request, strategy);
                Assert.IsFalse(read.IsError, read.Message);
                Assert.AreEqual(strategy, manager.LastProviderSelectionDiagnostic.RequestedStrategy);
                Assert.AreEqual(2, manager.LastProviderSelectionDiagnostic.Candidates.Count);
                Assert.AreEqual(manager.LastProviderSelectionDiagnostic.SelectedProvider == ProviderType.Neo4jOASIS ? "distinct-neo" : "distinct-sqlite", read.Result.Name);
            }
            manager.IsAutoLoadBalanceEnabled = false; manager.IsAutoReplicationEnabled = true;
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            foreach (var primary in providers)
            foreach (var asynchronous in new[] { false, true })
            {
                var value = new Holon { Id = id, Name = "replicated-" + primary.Value + asynchronous };
                value.MetaData["biome"] = "park";
                var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = value, SaveChildren = false, PreferredProvider = primary.Value };
                var saved = asynchronous ? await router.RouteRequestAsync<IHolon>(request) : router.RouteRequest<IHolon>(request);
                Assert.IsFalse(saved.IsError, saved.Message); Assert.IsFalse(saved.IsWarning, saved.Message);
                Assert.AreEqual(value.Name, (await neo.LoadHolonAsync(id, loadChildren:false)).Result.Name);
                Assert.AreEqual(value.Name, (await sqlite.LoadHolonAsync(id, loadChildren:false)).Result.Name);
            }
            manager.IsAutoReplicationEnabled = false; manager.IsAutoFailOverEnabled = true;
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            var missing = Guid.NewGuid();
            Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon { Id = missing, Name = "must-not-return-stale" }, saveChildren:false)).IsError);
            var before = manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests;
            foreach (var asynchronous in new[] { false, true })
            {
                var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = missing, LoadChildren = false, PreferredProvider = ProviderType.Neo4jOASIS };
                var read = asynchronous ? await router.RouteRequestAsync<IHolon>(request) : router.RouteRequest<IHolon>(request);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound, read.ErrorCode); Assert.IsNull(read.Result);
            }
            Assert.AreEqual(before, manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests);
            Assert.IsFalse(manager.SetAndActivateCurrentStorageProvider(neo).IsError);
            var field = typeof(Provider).GetField("_driver", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var healthy = field.GetValue(neo);
            using var unavailable = GraphDatabase.Driver("bolt://127.0.0.1:17688", AuthTokens.Basic("neo4j", "oasis-local-test-only"),
                config => config.WithConnectionTimeout(TimeSpan.FromSeconds(1)).WithMaxTransactionRetryTime(TimeSpan.Zero));
            try
            {
                field.SetValue(neo, unavailable);
                foreach (var asynchronous in new[] { false, true })
                {
                    var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id, LoadChildren = false, PreferredProvider = ProviderType.Neo4jOASIS };
                    var read = asynchronous ? await router.RouteRequestAsync<IHolon>(request) : router.RouteRequest<IHolon>(request);
                    Assert.IsFalse(read.IsError, read.Message);
                    Assert.AreEqual(ProviderType.SQLLiteDBOASIS, manager.LastFailoverDiagnostic.SelectedProvider);
                    Assert.AreEqual((await sqlite.LoadHolonAsync(id, loadChildren:false)).Result.Name, read.Result.Name);
                }
            }
            finally { field.SetValue(neo, healthy); }
            using var cleanup = raw.AsyncSession();
            await (await cleanup.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h", new { id = id.ToString() })).ConsumeAsync();
            verified = true;
        }
        finally
        {
            await sqlite.DeActivateProviderAsync(); await neo.DeActivateProviderAsync();
            if (verified) File.Delete(database); // Exact owned disposable DB only, after provider shutdown.
        }
    }
}

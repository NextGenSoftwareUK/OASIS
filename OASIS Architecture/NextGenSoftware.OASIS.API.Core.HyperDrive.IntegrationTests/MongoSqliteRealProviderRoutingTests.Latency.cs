using System.Diagnostics;
using System.Text.Json;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using Xunit;
using Xunit.Abstractions;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class MongoSqliteRoutingLatencyTests(ITestOutputHelper output)
{
    [Fact]
    public async Task ProfileNativeMongoSqlitePointReadLatency()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The latency check requires the owned MongoDB runner.");

        var database = "hyperdrive_latency_" + Guid.NewGuid().ToString("N");
        var directory = Directory.CreateTempSubdirectory("oasis-routing-latency-");
        var client = new MongoClient(uri);
        var mongo = new MongoDBOASIS(uri, database);
        try
        {
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "point-reads.db")};Pooling=False");
            Assert.False((await mongo.ActivateProviderAsync()).IsError);
            Assert.False((await sqlite.ActivateProviderAsync()).IsError);
            var id = Guid.NewGuid();
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var saved = await provider.SaveHolonAsync(new Holon { Id = id, Name = "Native point-read latency" }, saveChildren: false);
                Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
            }

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(), SubscriptionConfig = new() { PlanType = "Gold" } } };
            var manager = new ProviderManager(null, dna);
            manager.RegisterProvider(mongo);
            manager.RegisterProvider(sqlite);
            var reader = new HolonManager(null, dna, manager);
            var overrides = new ProviderRoutingOverrides(autoFailoverEnabled: false, autoLoadBalancingEnabled: true,
                loadBalancingProviders: [ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS]);

            async Task<OASISResult<IHolon>> ReadRouted(LoadBalancingStrategy strategy)
            {
                var scope = reader.CreateRequestContext(overrides, ProviderType.Default, routeDefaultProvider: true);
                Assert.False(scope.IsError, scope.Message);
                return await scope.Result.Manager.LoadHolonAsync(id, false, false, 0, false, true,
                    HolonType.All, 0, ProviderType.Default, false, strategy);
            }

            var cases = new (string Name, Func<Task<OASISResult<IHolon>>> Read, string? Strategy)[]
            {
                ("MongoDB direct", () => mongo.LoadHolonAsync(id, loadChildren: false), null),
                ("SQLite direct", () => sqlite.LoadHolonAsync(id, loadChildren: false), null),
                ("HyperDrive Auto", () => ReadRouted(LoadBalancingStrategy.Auto), "Auto"),
                ("HyperDrive RoundRobin", () => ReadRouted(LoadBalancingStrategy.RoundRobin), "RoundRobin")
            };
            const int warmupCount = 6;
            const int sampleCount = 40;
            var measurements = cases.ToDictionary(item => item.Name, _ => new List<double>(sampleCount));
            var routing = cases.ToDictionary(item => item.Name, _ => new Dictionary<string, int>());
            for (var iteration = 0; iteration < warmupCount + sampleCount; iteration++)
            {
                // Rotate the first case to distribute short-lived runner delays across paths.
                for (var offset = 0; offset < cases.Length; offset++)
                {
                    var item = cases[(iteration + offset) % cases.Length];
                    var started = Stopwatch.GetTimestamp();
                    var loaded = await item.Read();
                    var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    Assert.True(!loaded.IsError && loaded.Result != null, loaded.Message);
                    Assert.Equal(id, loaded.Result.Id);
                    Assert.Equal("Native point-read latency", loaded.Result.Name);
                    if (iteration < warmupCount) continue;

                    measurements[item.Name].Add(elapsed);
                    if (item.Strategy == null) continue;

                    using var route = JsonDocument.Parse(loaded.MetaData["hyperDriveRoute"]);
                    Assert.Equal(item.Strategy, route.RootElement.GetProperty("RequestedStrategy").GetString());
                    var source = route.RootElement.GetProperty("ServingProvider").GetString()!;
                    Assert.Contains(source, new[] { nameof(ProviderType.MongoDBOASIS), nameof(ProviderType.SQLLiteDBOASIS) });
                    routing[item.Name][source] = routing[item.Name].GetValueOrDefault(source) + 1;
                }
            }

            Assert.Equal(sampleCount / 2, routing["HyperDrive RoundRobin"][nameof(ProviderType.MongoDBOASIS)]);
            Assert.Equal(sampleCount / 2, routing["HyperDrive RoundRobin"][nameof(ProviderType.SQLLiteDBOASIS)]);
            foreach (var item in cases)
            {
                var samples = measurements[item.Name];
                samples.Sort();
                output.WriteLine("HYPERDRIVE_LATENCY " + JsonSerializer.Serialize(new
                {
                    path = item.Name, samples = samples.Count, warmup = warmupCount,
                    p50Ms = Math.Round(samples[(samples.Count + 1) / 2 - 1], 3),
                    p95Ms = Math.Round(samples[(int)Math.Ceiling(samples.Count * 0.95) - 1], 3),
                    maxMs = Math.Round(samples[^1], 3), servingProviders = routing[item.Name]
                }));
            }

            await ProfileDashboardParentCollectionAsync(reader, mongo, sqlite);
        }
        finally
        {
            try { await client.DropDatabaseAsync(database); }
            finally { directory.Delete(recursive: true); }
        }
    }

    [Fact]
    public async Task ColdKarmaReadDoesNotPersistSettings()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The karma check requires the owned MongoDB runner.");

        var database = "hyperdrive_karma_" + Guid.NewGuid().ToString("N");
        var directory = Directory.CreateTempSubdirectory("oasis-routing-karma-");
        var client = new MongoClient(uri);
        var mongo = new MongoDBOASIS(uri, database);
        try
        {
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "karma.db")};Pooling=False");
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var activation = await provider.ActivateProviderAsync();
                Assert.False(activation.IsError, activation.Message);
                var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                    { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
                var providers = new ProviderManager(provider, dna)
                    { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
                providers.RegisterProvider(provider);
                var avatarId = Guid.NewGuid();
                var settingsId = new Guid(System.Security.Cryptography.MD5.HashData(
                    System.Text.Encoding.UTF8.GetBytes($"settings:{avatarId}:karma")));
                var karma = new KarmaManager(provider, dna, providers);
                var balance = karma.GetKarma(avatarId);
                Assert.False(balance.IsError, balance.Message);
                Assert.Equal(0L, balance.Result);
                var stats = await karma.GetKarmaStatsAsync(avatarId);
                Assert.False(stats.IsError, stats.Message);
                Assert.Equal(0L, Assert.IsType<long>(stats.Result["totalKarma"]));

                var persisted = await new HolonManager(provider, dna, providers)
                    .LoadHolonAsync(settingsId, loadChildren: false, allowMissing: true);
                Assert.False(persisted.IsError, persisted.Message);
                Assert.Equal(OASISResultErrorCodes.RecordNotFound, persisted.ErrorCode);
                Assert.Null(persisted.Result);
            }
        }
        finally
        {
            try { await client.DropDatabaseAsync(database); }
            finally { directory.Delete(recursive: true); }
        }
    }

    private async Task ProfileDashboardParentCollectionAsync(HolonManager reader, MongoDBOASIS mongo, SQLLiteDBOASIS sqlite)
    {
        const int parentResults = 8;
        const int childrenPerParent = 4;
        const int grandchildrenPerChild = 2;
        var dashboardParent = Guid.NewGuid();
        Holon CreateHolon(Guid parent) => new()
        {
            Id = Guid.NewGuid(), ParentHolonId = parent, IsActive = true, Name = "Dashboard collection fixture", Children = new List<IHolon>()
        };

        var roots = new List<Holon>();
        var records = new List<Holon>();
        for (var rootIndex = 0; rootIndex < parentResults; rootIndex++)
        {
            var root = CreateHolon(dashboardParent);
            roots.Add(root);
            records.Add(root);
            for (var childIndex = 0; childIndex < childrenPerParent; childIndex++)
            {
                var child = CreateHolon(root.Id);
                root.Children.Add(child);
                records.Add(child);
                for (var leafIndex = 0; leafIndex < grandchildrenPerChild; leafIndex++)
                {
                    var leaf = CreateHolon(child.Id);
                    child.Children.Add(leaf);
                    records.Add(leaf);
                }
            }
        }

        foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
        foreach (var record in roots)
        {
            var saved = await provider.SaveHolonAsync(record, saveChildren: true);
            Assert.True(saved.IsSaved && !saved.IsError, saved.Message);
        }

        var expectedIds = roots.Select(root => root.Id).OrderBy(id => id).ToArray();
        static int CountNodes(IHolon node) => 1 + (node.Children?.Sum(CountNodes) ?? 0);
        foreach (var provider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
        {
            var samples = new Dictionary<bool, List<double>> { [false] = [], [true] = [] };
            for (var iteration = 0; iteration < 24; iteration++)
            {
                foreach (var recursive in new[] { iteration % 2 == 0, iteration % 2 != 0 })
                {
                    var started = Stopwatch.GetTimestamp();
                    var result = await reader.LoadHolonsForParentAsync(dashboardParent, HolonType.All,
                        loadChildren: recursive, recursive: recursive, providerType: provider);
                    var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                    Assert.True(!result.IsError && result.Result != null, result.Message);
                    var rows = result.Result.ToArray();
                    Assert.Equal(expectedIds, rows.Select(row => row.Id).OrderBy(id => id).ToArray());
                    Assert.Equal(recursive ? records.Count : parentResults, rows.Sum(CountNodes));
                    if (iteration >= 4) samples[recursive].Add(elapsed);
                }
            }

            foreach (var (recursive, timings) in samples)
            {
                timings.Sort();
                output.WriteLine("HYPERDRIVE_DASHBOARD_LATENCY " + JsonSerializer.Serialize(new
                {
                    provider = provider.ToString(), recursive, samples = timings.Count,
                    resultCount = parentResults, materializedNodes = recursive ? records.Count : parentResults,
                    p50Ms = Math.Round(timings[(timings.Count + 1) / 2 - 1], 3),
                    p95Ms = Math.Round(timings[(int)Math.Ceiling(timings.Count * 0.95) - 1], 3)
                }));
            }
        }
    }
}

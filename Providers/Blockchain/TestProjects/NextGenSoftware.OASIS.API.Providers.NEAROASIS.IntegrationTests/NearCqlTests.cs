using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class NearCqlTests
{
    [TestMethod]
    public async Task CancellingNativeQueryStopsItsOwnedSdkWorkerWithoutPartialRows()
    {
        var names = new[] { "OASIS_NEAR_SDK_BRIDGE", "OASIS_NEAR_STORAGE_ENCRYPTION_KEY", "OASIS_NEAR_CQL_TEST_PID" };
        var previous = names.ToDictionary(name => name, Environment.GetEnvironmentVariable);
        var results = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../../TestResults/NearCqlCurrent"));
        Directory.CreateDirectory(results);
        var marker = Path.Combine(results, "controlled-query.pid");
        Assert.IsFalse(File.Exists(marker), "Preserve and inspect an earlier worker marker before another run.");
        try
        {
            Environment.SetEnvironmentVariable(names[0], Path.Combine(AppContext.BaseDirectory, "Fixtures", "storage-snapshot.mjs"));
            Environment.SetEnvironmentVariable(names[1], Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));
            Environment.SetEnvironmentVariable(names[2], marker);
            var provider = new NEAROASIS("http://127.0.0.1:1", "controlled", "controlled");
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            var plan = CosmicQueryPlanner.Plan(Query(new[] { Guid.NewGuid() }), ProviderType.NEAROASIS,
                new[] { new CosmicQueryTarget(ProviderType.NEAROASIS, provider.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            using var cancellation = new CancellationTokenSource();
            var running = provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancellation.Token);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!File.Exists(marker) && watch.Elapsed < TimeSpan.FromSeconds(5)) await Task.Delay(20);
            cancellation.Cancel();
            var result = await running.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.IsTrue(File.Exists(marker), "The owned worker must start before cancellation.");
            var pid = int.Parse(await File.ReadAllTextAsync(marker));
            Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
            Assert.IsNull(result.Result);
            Assert.ThrowsException<ArgumentException>(() => System.Diagnostics.Process.GetProcessById(pid));
            File.Delete(marker); // Verified process exit, never evidence-only inference.
            await provider.DeActivateProviderAsync();
        }
        finally { foreach (var name in names) Environment.SetEnvironmentVariable(name, previous[name]); }
    }

    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.NEAROASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.NEAROASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = NearCqlCompiler.Compile(query, ProviderType.NEAROASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(NearCqlCompiler.Compile(query, ProviderType.PolygonOASIS).IsError);
        Assert.AreEqual(0, NearCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.NEAROASIS).Result.Count);
    }

    [DataTestMethod]
    [DataRow("name")]
    [DataRow("metadata")]
    [DataRow("order")]
    [DataRow("graph")]
    [DataRow("bound")]
    public void UnsupportedShapesRejectInsteadOfScanningNativeRecords(string shape)
    {
        var query = Query(Enumerable.Range(0, shape == "bound" ? 101 : 3).Select(_ => Guid.NewGuid()).ToArray());
        query = shape switch
        {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };

        var result = NearCqlCompiler.Compile(query, ProviderType.NEAROASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new NEAROASIS("http://127.0.0.1:1", "controlled", "controlled");
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.NEAROASIS,
            new[] { new CosmicQueryTarget(ProviderType.NEAROASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}

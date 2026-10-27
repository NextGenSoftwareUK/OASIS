using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static HolonManager NativeManager(ICosmicQueryStorageProvider adapter)
    {
        var dna = new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = HyperDriveModes.V2,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        };
        var registry = new ProviderManager(null, dna)
        { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
        return new HolonManager((IOASISStorageProvider)adapter, dna, registry);
    }

    private static async Task VerifyLinqRows(ICosmicQueryStorageProvider adapter, Guid rootId, List<object> checks)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Result;
        var captured = CosmicLinq.Create(NativeManager(adapter), adapter, source, maximumResults: 10);
        Assert.IsFalse(captured.IsError, captured.Message);
        var root = captured.Result;
        var name = "protected source root";
        var query = root.Where(row => row.Name == name)
            .OrderByDescending(row => row.Name).ThenBy(row => row.Name).Skip(0).Take(1);

        var read = await query.ToCosmicListAsync();
        Assert.IsFalse(read.IsError, read.Message + read.Exception);
        Assert.IsTrue(read.IsLoaded); Assert.AreEqual(rootId, read.Result.Single().Id);
        Assert.IsFalse(HolonChangeTracking.HasChanges(read.Result.Single()));
        Passed("deferred native rows");
        name = "changed before next execution";
        var rebound = await query.ToCosmicListAsync();
        Assert.IsFalse(rebound.IsError, rebound.Message); Assert.AreEqual(0, rebound.Result.Count);
        Passed("standard closure binding per execution");
        name = "protected source root";

        Assert.AreEqual(rootId, query.ToList().Single().Id);
        Passed("synchronous standard enumeration");
        var empty = await root.Where(row => row.Name == "absent").ToCosmicListAsync();
        Assert.IsFalse(empty.IsError, empty.Message); Assert.AreEqual(0, empty.Result.Count);
        Passed("native false predicate");
        empty = await root.Skip(-2).Take(-3).ToCosmicListAsync();
        Assert.IsFalse(empty.IsError, empty.Message); Assert.AreEqual(0, empty.Result.Count);
        Passed("standard negative paging semantics");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var cancelled = await query.ToCosmicListAsync(cancellation.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", cancelled.ErrorCode); Assert.IsNull(cancelled.Result);
        Passed("explicit async cancellation");
        var unsupported = Assert.ThrowsException<CosmicLinqException>(() => query.Count());
        Assert.AreEqual("CQL_EXPRESSION_UNSUPPORTED", unsupported.Code);
        Passed("pending terminal fails explicitly");

        void Passed(string label) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), label });
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyEntityStages(ICosmicQueryEntityCompositionStorageProvider adapter, Guid rootId, List<object> checks)
    {
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId).Result;
        var root = CosmicLinq.Create(NativeManager(adapter), adapter, source, 10).Result;
        async Task<IReadOnlyList<Holon>> Rows(IQueryable<Holon> query)
        {
            var read = await query.ToCosmicListAsync();
            Assert.IsFalse(read.IsError, read.Message + read.Exception); Assert.IsTrue(read.IsLoaded); return read.Result;
        }
        void Passed(string name) => checks.Add(new { provider = adapter.QueryProviderType.ToString(), entityStage = name });
        Assert.AreEqual(rootId, (await Rows(root.Take(1).Where(row => row.Name == "protected source root"))).Single().Id);
        Passed("post-page filter");
        Assert.AreEqual(0, (await Rows(root.Take(1).Where(row => row.Name == "missing"))).Count); Passed("post-page negative filter");
        Assert.AreEqual(rootId, (await Rows(root.Take(1).OrderByDescending(row => row.Name).ThenBy(row => row.Version))).Single().Id);
        Passed("independent post-page ordering");
        Assert.AreEqual(0, (await Rows(root.Take(1).OrderBy(row => row.Name).Take(0).Where(row => true))).Count);
        Passed("later empty window");
        var name = "missing"; var deferred = root.Take(1).Where(row => row.Name == name);
        name = "protected source root";
        var typed = (await Rows(deferred)).Single(); Assert.AreEqual(rootId, typed.Id); Passed("deferred rebinding");
        Assert.IsFalse(HolonChangeTracking.HasChanges(typed)); typed.Name = "local";
        Assert.IsTrue(HolonChangeTracking.HasChanges(typed)); Passed("detached typed tracking");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await deferred.ToCosmicListAsync(cancellation.Token)).ErrorCode); Passed("cancellation");
        Assert.AreEqual(rootId, (await Rows(root.Take(1).Where(row => row.Children.Any(child => child.Name == "visible match")))).Single().Id);
        Passed("child predicate stage");
        var sufficient = CosmicQueryTraversalOptions.Create(2, 4).Result;
        var tight = CosmicQueryTraversalOptions.Create(2, 1).Result;
        Assert.AreEqual(rootId, (await Rows(root.Take(1).Where(row => CosmicQueryRelations.AnyDescendant(row,
            child => child.Name == "grandchild one", sufficient)))).Single().Id); Passed("descendant predicate stage");
        var failed = await root.Take(1).Where(row => row.Name == "missing" &&
            CosmicQueryRelations.AnyDescendant(row, child => true, tight)).Take(0).ToCosmicListAsync();
        Assert.AreEqual("CQL_QUERY_LIMIT", failed.ErrorCode, failed.Message + failed.Exception); Passed("false AND protected before later empty");
        failed = await root.Take(1).Where(row => row.Name == "protected source root" ||
            CosmicQueryRelations.AnyDescendant(row, child => true, tight)).Take(0).ToCosmicListAsync();
        Assert.AreEqual("CQL_QUERY_LIMIT", failed.ErrorCode, failed.Message + failed.Exception); Passed("true OR protected before later empty");
        Assert.AreEqual(0, (await Rows(root.Take(1).OrderBy(row => row.Name).Take(0).Where(row =>
            CosmicQueryRelations.AnyDescendant(row, child => true, tight)))).Count); Passed("earlier empty stage visits no roots");
        var protectedSource = source.WhereAnyDescendant(row => true, tight).Take(0).Result;
        failed = await CosmicLinq.Create(NativeManager(adapter), adapter, protectedSource).Result.Where(row => true).ToCosmicListAsync();
        Assert.AreEqual("CQL_QUERY_LIMIT", failed.ErrorCode, failed.Message + failed.Exception); Passed("protected source before empty page");
        var candidates = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId || row.ParentHolonId == rootId).Take(3).Result;
        var pipeline = candidates.Compose().Result.Where(row => false).Result.Take(0).Result;
        foreach (var budget in new[] { new CosmicEntityCompositionOptions(maximumInputRows: 1),
            new CosmicEntityCompositionOptions(maximumInputPayloadBytes: 1), new CosmicEntityCompositionOptions(maximumStageRows: 1) })
        {
            var plan = CosmicEntityCompositionPlanner.Plan(pipeline, adapter.QueryProviderType,
                new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities), adapter.EntityCompositionCapabilities, budget);
            Assert.IsFalse(plan.IsError, plan.Message);
            var rejected = await NativeManager(adapter).ExecuteTypedEntityCompositionQueryAsync(plan.Result, new ICosmicQueryStorageProvider[] { adapter });
            Assert.AreEqual("CQL_QUERY_LIMIT", rejected.ErrorCode, rejected.Message + rejected.Exception);
            Passed("source/stage budget " + budget.MaximumInputRows + "/" + budget.MaximumInputPayloadBytes + "/" + budget.MaximumStageRows);
        }
        var consistency = adapter.QueryProviderType == ProviderType.Neo4jOASIS
            ? CosmicQueryGraphConsistency.ProviderDefault : CosmicQueryGraphConsistency.ProviderSnapshot;
        if (adapter.QueryProviderType == ProviderType.Neo4jOASIS)
        {
            var unsupported = source.WithChildren(CosmicQueryGraphOptions.Create(CosmicQueryGraphConsistency.ProviderSnapshot).Result).Result;
            var denied = await CosmicLinq.Create(NativeManager(adapter), adapter, unsupported).Result.Take(1).Where(row => true).ToCosmicListAsync();
            Assert.AreEqual("CQL_CONSISTENCY_UNSUPPORTED", denied.ErrorCode); Passed("snapshot request explicitly rejected");
        }
        var graph = CosmicQueryGraphOptions.Create(consistency, maxChildDepth: 1, maximumNodes: 2).Result;
        var visibleSource = source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).WithChildren(graph).Result;
        var visible = (await Rows(CosmicLinq.Create(NativeManager(adapter), adapter, visibleSource).Result.Take(1).Where(row => true))).Single();
        Assert.AreEqual("visible match", visible.Children.Single().Name); Assert.AreEqual(0, visible.Children.Single().Children.Count());
        Passed("snapshot graph visibility/depth");
        var tinyGraph = CosmicQueryGraphOptions.Create(consistency, maximumNodes: 1).Result;
        // Graph options must be bound before the source page; the graph budget
        // applies to selected output, never truncates earlier source candidates.
        var graphCandidates = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(adapter.QueryProviderType).Result)
            .Where(row => row.Id == rootId || row.ParentHolonId == rootId).WithChildren(tinyGraph).Take(3).Result;
        var winner = (await Rows(CosmicLinq.Create(NativeManager(adapter), adapter, graphCandidates).Result
            .Where(row => row.Name == "visible match"))).Single();
        Assert.AreEqual("visible match", winner.Name); Passed("final graph budget does not truncate source");
    }
}

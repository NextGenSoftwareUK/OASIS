using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private static async Task VerifyAuthoritativeAccess(Dictionary<ProviderType, IOASISStorageProvider> providers,
        Guid actor, Guid[] ids, Guid[] childIds)
    {
        foreach (var pair in providers)
        {
            var adapter = (ICosmicQueryStorageProvider)pair.Value;
            var manager = new Sources(providers).CreateManager(pair.Value);
            var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, pair.Key, new[] { ids[0], ids[2] });
            Assert.IsFalse(access.IsError, access.Message);
            var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(pair.Key).Result)
                .WithReadAccess(access.Result).OrderBy(row => row.Name).Result;

            var plan = CosmicQueryPlanner.Plan(source.Skip(1).Take(1).Result, pair.Key,
                new[] { new CosmicQueryTarget(pair.Key, adapter.QueryCapabilities) });
            Assert.IsFalse(plan.IsError, plan.Message);
            var page = await manager.ExecuteQueryAsync(plan.Result, new[] { adapter });
            Assert.IsFalse(page.IsError, page.Message + page.Exception);
            Assert.AreEqual(ids[2], page.Result.Providers.Single().Rows.Single().Id,
                "Native access must precede paging and admit the authorized private row.");

            var nativeComposition = (ICosmicQueryProjectionCompositionStorageProvider)adapter;
            var composed = source.Select(row => row.Id).Where(id => id != Guid.Empty).Result;
            var compositionPlan = CosmicProjectionCompositionPlanner.Plan(composed, pair.Key,
                new[] { new CosmicProjectionCompositionTarget(pair.Key, adapter.QueryCapabilities,
                    nativeComposition.ProjectionCapabilities, nativeComposition.CompositionCapabilities) });
            Assert.IsFalse(compositionPlan.IsError, compositionPlan.Message);
            var composition = await manager.ExecuteProjectionCompositionQueryAsync(compositionPlan.Result, new[] { adapter });
            Assert.IsFalse(composition.IsError, composition.Message + composition.Exception);
            CollectionAssert.AreEquivalent(new[] { ids[0], ids[2] }, composition.Result.Providers.Single().Rows.ToArray());

            var nativeJoin = (ICosmicQueryJoinStorageProvider)adapter;
            var joined = source.Join(source, left => left.Id, right => right.Id)
                .Select((left, right) => left.Id).Result;
            var joinPlan = CosmicJoinPlanner.Plan(joined, pair.Key,
                new[] { new CosmicJoinTarget(pair.Key, adapter.QueryCapabilities, nativeJoin.JoinCapabilities) });
            Assert.IsFalse(joinPlan.IsError, joinPlan.Message);
            var join = await manager.ExecuteJoinQueryAsync(joinPlan.Result, new[] { adapter });
            Assert.IsFalse(join.IsError, join.Message + join.Exception);
            CollectionAssert.AreEquivalent(new[] { ids[0], ids[2] }, join.Result.Providers.Single().Rows.Select(row => row.Value).ToArray());

            // Even public descendants must belong to the trusted tenant scope.
            var graphAccess = CosmicQueryReadAccess.AuthoritativeRecords(actor, pair.Key, new[] { ids[3], childIds[0] }).Result;
            var graphRoot = ids[3];
            var graph = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(pair.Key).Result)
                .WithReadAccess(graphAccess).Where(row => row.Id == graphRoot)
                .WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 2).Result);
            Assert.IsFalse(graph.IsError, graph.Message);
            var graphPlan = CosmicQueryPlanner.Plan(graph.Result, pair.Key,
                new[] { new CosmicQueryTarget(pair.Key, adapter.QueryCapabilities) });
            Assert.IsFalse(graphPlan.IsError, graphPlan.Message);
            var graphResult = await manager.ExecuteQueryAsync(graphPlan.Result, new[] { adapter });
            Assert.IsFalse(graphResult.IsError, graphResult.Message + graphResult.Exception);
            CollectionAssert.AreEqual(new[] { childIds[0] }, graphResult.Result.Providers.Single().Rows.Single().Children.Select(row => row.Id).ToArray());
        }
    }
}

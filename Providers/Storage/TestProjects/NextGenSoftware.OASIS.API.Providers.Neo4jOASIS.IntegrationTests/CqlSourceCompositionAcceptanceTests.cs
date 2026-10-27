using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifySourceMatrix(ICosmicQueryProjectedAllStorageProvider[] adapters,
        Func<ProviderType, CosmicQuery<Holon>> sourceFor, Guid actor, List<object> checks)
    {
        var enough = CosmicQueryTraversalOptions.Create(2, 4).Result;
        var shallow = CosmicQueryTraversalOptions.Create(1, 2).Result;
        var tight = CosmicQueryTraversalOptions.Create(2, 1).Result;
        foreach (var adapter in adapters)
        {
            var source = sourceFor(adapter.QueryProviderType);
            var secure = source.WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
            var cases = new (string Label, CosmicQuery<Holon> Query, int Count)[]
            {
                ("child-child", source.Where(row => row.Children.Any(child => child.Children.Any(leaf => leaf.Name == "grandchild one"))).Result, 1),
                ("child-descendant", source.WhereAnyChild(child => CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "grandchild one", enough)).Result, 1),
                ("descendant-child", source.WhereAnyDescendant(child => child.Children.Any(leaf => leaf.Name == "grandchild one"), shallow).Result, 1),
                ("descendant-descendant", source.WhereAnyDescendant(child => CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "grandchild one", enough), shallow).Result, 1),
                ("child-child absent", source.Where(row => row.Children.Any(child => child.Children.Any(leaf => leaf.Name == "absent"))).Result, 0),
                ("child-descendant absent", source.WhereAnyChild(child => CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "absent", enough)).Result, 0),
                ("descendant-child absent", source.WhereAnyDescendant(child => child.Children.Any(leaf => leaf.Name == "absent"), shallow).Result, 0),
                ("descendant-descendant absent", source.WhereAnyDescendant(child => CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "absent", enough), shallow).Result, 0),
                ("root AND", source.Where(row => row.Name == "protected source root" && CosmicQueryRelations.AnyDescendant(row, child => child.Name == "grandchild one", enough)).Result, 1),
                ("root OR", source.Where(row => row.Name == "absent" || CosmicQueryRelations.AnyDescendant(row, child => child.Name == "grandchild one", enough)).Result, 1),
                ("root NOT absent", source.Where(row => !CosmicQueryRelations.AnyDescendant(row, child => child.Name == "absent", enough)).Result, 1),
                ("root NOT present", source.Where(row => !CosmicQueryRelations.AnyDescendant(row, child => child.Name == "grandchild one", enough)).Result, 0),
                ("private intermediate child", secure.Where(row => row.Children.Any(child => child.Children.Any(leaf => leaf.Name == "grandchild one"))).Result, 0),
                ("private intermediate descendant", secure.WhereAnyChild(child => CosmicQueryRelations.AnyDescendant(child, leaf => leaf.Name == "grandchild one", enough)).Result, 0)
            };
            foreach (var item in cases) await Check(item.Label, item.Query, item.Count);

            var errors = new (string Label, CosmicQuery<Holon> Query)[]
            {
                ("NOT traversal limit", source.Where(row => !CosmicQueryRelations.AnyDescendant(row, child => true, tight)).Result),
                ("child NOT traversal limit", source.WhereAnyChild(child => !CosmicQueryRelations.AnyDescendant(child, leaf => true, tight)).Result),
                ("AND traversal limit", source.Where(row => row.Name == "absent" && CosmicQueryRelations.AnyDescendant(row, child => true, tight)).Result),
                ("OR traversal limit", source.Where(row => row.Name == "protected source root" || CosmicQueryRelations.AnyDescendant(row, child => true, tight)).Result),
                ("child OR traversal limit", source.WhereAnyChild(child => child.Name == "visible match" || CosmicQueryRelations.AnyDescendant(child, leaf => true, tight)).Result)
            };
            foreach (var item in errors)
            {
                await Check(item.Label, item.Query, 0, "CQL_QUERY_LIMIT");
                await Check(item.Label + " empty source", item.Query.Take(0).Result, 0, "CQL_QUERY_LIMIT");
            }

            async Task Check(string label, CosmicQuery<Holon> query, int count, string? error = null)
            {
                Assert.IsNotNull(query, label + ": source capture failed.");
                var rowPlan = CosmicQueryPlanner.Plan(query, adapter.QueryProviderType,
                    new[] { new CosmicQueryTarget(adapter.QueryProviderType, adapter.QueryCapabilities) });
                Assert.IsFalse(rowPlan.IsError, label + rowPlan.Message);
                var rows = await adapter.ExecuteCosmicQueryAsync(rowPlan.Result, maximumResults: 10);
                Outcome("rows", rows.IsError, rows.ErrorCode, rows.IsLoaded);
                if (error == null)
                {
                    Assert.AreEqual(count, rows.Result.Count, label);
                    foreach (var row in rows.Result) Assert.AreEqual("protected source root", row.Name, label);
                }
                var scalar = (ICosmicQueryScalarStorageProvider)adapter;
                var countPlan = CosmicScalarPlanner.Plan(query.LongCount().Result, adapter.QueryProviderType,
                    new[] { new CosmicScalarTarget(adapter.QueryProviderType, adapter.QueryCapabilities, scalar.ScalarCapabilities) });
                Assert.IsFalse(countPlan.IsError, label + countPlan.Message);
                var counted = await scalar.ExecuteCosmicScalarAsync(countPlan.Result);
                Outcome("count", counted.IsError, counted.ErrorCode, counted.IsLoaded);
                if (error == null) Assert.AreEqual((long)count, counted.Result, label);
                var projection = (ICosmicQueryProjectionStorageProvider)adapter;
                var plan = CosmicProjectionPlanner.Plan(query.Select(row => row.Name).Result, adapter.QueryProviderType,
                    new[] { new CosmicProjectionTarget(adapter.QueryProviderType, adapter.QueryCapabilities, projection.ProjectionCapabilities) });
                Assert.IsFalse(plan.IsError, label + plan.Message);
                var projected = await projection.ExecuteCosmicProjectionAsync(plan.Result, maximumResults: 10);
                Outcome("projection", projected.IsError, projected.ErrorCode, projected.IsLoaded);
                if (error == null) Assert.AreEqual(count, projected.Result.Count, label);

                void Outcome(string consumer, bool failed, string? code, bool loaded)
                {
                    Assert.AreEqual(error, code, adapter.QueryProviderType + ": " + label + " / " + consumer);
                    Assert.AreEqual(error != null, failed, label + " / " + consumer);
                    Assert.AreEqual(error == null, loaded, label + " / " + consumer);
                    checks.Add(new { label, consumer, provider = adapter.QueryProviderType.ToString(), error, count = error == null ? (int?)count : null });
                }
            }
        }
    }
}

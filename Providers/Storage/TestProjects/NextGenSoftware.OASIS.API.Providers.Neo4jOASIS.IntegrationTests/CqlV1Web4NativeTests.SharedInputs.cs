using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    [TestMethod]
    public Task NeoGroupedSharedInputsPreserveIndependentReducersAndPaths()
        => RunGroupedDistinctNativeFixture("Neo4jOASIS", false, sharedInputs: true);

    private static async Task<int> VerifyNeoGroupedSharedInputs(IOASISStorageProvider provider, Guid[] selected)
    {
        var adapter = (ICosmicQueryGroupedStorageProvider)provider;
        var grouping = CosmicQuery<Holon>.Create().Where(h => selected.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category")).Result;
        var bindings = new[]
        {
            CosmicGroupedBinding<Holon, string>.Key(grouping, "Category", 0).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Present",
                h => CosmicQueryMetadata.Value<int?>(h, "stock")).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Unique",
                h => CosmicQueryMetadata.Value<int?>(h, "stock"), true).Result,
            CosmicGroupedBinding<Holon, string>.Aggregate(grouping, "Total",
                grouping.Source.Sum(h => CosmicQueryMetadata.Value<int?>(h, "stock")).Result).Result,
            CosmicGroupedBinding<Holon, string>.Aggregate(grouping, "Minimum",
                grouping.Source.Min(h => CosmicQueryMetadata.Value<int?>(h, "stock")).Result).Result,
            CosmicGroupedBinding<Holon, string>.Aggregate(grouping, "Maximum",
                grouping.Source.Max(h => CosmicQueryMetadata.Value<int?>(h, "stock")).Result).Result,
            CosmicGroupedBinding<Holon, string>.Aggregate(grouping, "Mean",
                grouping.Source.Average(h => CosmicQueryMetadata.Value<int?>(h, "stock")).Result).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "CanonicalName", h => h.Name, true).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "MetadataName",
                h => CosmicQueryMetadata.Value<string>(h, "Name"), true).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "MetadataCase",
                h => CosmicQueryMetadata.Value<int?>(h, "Stock"), true).Result
        };
        Assert.IsTrue(bindings.All(binding => binding != null));
        var selection = grouping.SelectColumns(bindings);
        Assert.IsFalse(selection.IsError, selection.Message);
        var plan = CosmicGroupedPlanner.Plan(selection.Result, ProviderType.Neo4jOASIS,
            new[] { new CosmicGroupedTarget(ProviderType.Neo4jOASIS, adapter.QueryCapabilities, adapter.GroupedCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        var result = await CosmicGroupedExecutor.ExecuteAsync(plan.Result,
            new[] { (ICosmicQueryStorageProvider)provider });
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        var row = result.Result.Providers.Single().Rows.Single();
        Assert.AreEqual("a", row["Category"]);
        Assert.AreEqual(3L, row["Present"]);
        Assert.AreEqual(2L, row["Unique"]);
        Assert.AreEqual(8, row["Total"]);
        Assert.AreEqual(2, row["Minimum"]);
        Assert.AreEqual(4, row["Maximum"]);
        Assert.AreEqual(8m / 3m, row["Mean"]);
        Assert.AreEqual(1L, row["CanonicalName"]);
        Assert.AreEqual(0L, row["MetadataName"]);
        Assert.AreEqual(0L, row["MetadataCase"]);

        var invalid = grouping.SelectColumns(new[]
        {
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Nullable",
                h => CosmicQueryMetadata.Value<int?>(h, "stock")).Result,
            CosmicGroupedBinding<Holon, string>.CountValues(grouping, "Required",
                h => CosmicQueryMetadata.Value<int>(h, "stock")).Result
        }).Result.Take(0).Result;
        var invalidPlan = CosmicGroupedPlanner.Plan(invalid, ProviderType.Neo4jOASIS,
            new[] { new CosmicGroupedTarget(ProviderType.Neo4jOASIS, adapter.QueryCapabilities, adapter.GroupedCapabilities) });
        Assert.IsFalse(invalidPlan.IsError, invalidPlan.Message);
        var invalidResult = await CosmicGroupedExecutor.ExecuteAsync(invalidPlan.Result,
            new[] { (ICosmicQueryStorageProvider)provider });
        Assert.IsTrue(invalidResult.IsError);
        Assert.AreEqual("CQL_DATA_INVALID", invalidResult.ErrorCode);
        return 2;
    }
}

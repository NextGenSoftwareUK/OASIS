extern alias ShippingWeb4;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using CqlGroupedRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlGroupedV1Request;
using CqlGroupedColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlGroupedV1Column;
using CqlProjectionColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlProjectionV1Column;
using CqlRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Request;
using CqlFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Filter;
using CqlOperator = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Operator;
using CqlScalar = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Scalar;
using CqlCompiler = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlV1Compiler;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyMetadataPresenceWire(ICosmicQueryStorageProvider adapter,
        Guid rootId, List<object> checks)
    {
        var provider = adapter.QueryProviderType;
        var target = new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) };
        var id = new CqlFilter
        {
            Field = "Id", Operator = CqlOperator.Equal, Value = JsonSerializer.SerializeToElement(rootId)
        };
        var actor = Guid.NewGuid();

        async Task AssertMatch(string key, CqlOperator operation, bool matches, string? integer = null)
        {
            var filter = new CqlFilter { MetadataPath = new[] { key }, Operator = operation };
            if (integer != null)
            {
                filter.Scalar = CqlScalar.Integer;
                filter.Value = JsonSerializer.SerializeToElement(integer);
            }

            var request = new CqlRequest { Filters = new[] { id, filter }, OrderBy = "Id", Take = 2 };
            var query = CqlCompiler.Compile(request, actor, provider);
            var plan = CosmicQueryPlanner.Plan(query, provider, target);
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await CosmicQueryExecutor.ExecuteAsync(plan.Result, new[] { adapter });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            var expected = matches ? new[] { rootId } : Array.Empty<Guid>();
            CollectionAssert.AreEqual(expected, result.Result.Providers.Single().Rows.Select(row => row.Id).ToArray());
        }

        await AssertMatch("missing", CqlOperator.IsMissing, true);
        await AssertMatch("presentNull", CqlOperator.IsMissing, false);
        await AssertMatch("text", CqlOperator.IsNotNull, true);
        await AssertMatch("presentNull", CqlOperator.IsNotNull, false);
        await AssertMatch("missing", CqlOperator.IsNotNull, false);
        await AssertMatch("i64", CqlOperator.Equal, true, "9007199254740993");
        await AssertMatch("i64", CqlOperator.Equal, false, "9007199254740992");
        checks.Add(new { provider = provider.ToString(), metadataWire = "missing, null, populated and exact signed Int64" });
    }

    private static async Task VerifyDistinctWire(ICosmicQueryGroupedStorageProvider adapter,
        Guid[] ids, List<object> checks)
    {
        var provider = adapter.QueryProviderType;
        var target = new[] { new CosmicGroupedTarget(provider, adapter.QueryCapabilities, adapter.GroupedCapabilities) };
        var actor = Guid.NewGuid();
        var request = new CqlGroupedRequest
        {
            Filters = new[] { new CqlFilter
            {
                Field = "Id", Operator = CqlOperator.In, Value = JsonSerializer.SerializeToElement(ids.Take(2).ToArray())
            } },
            Keys = new[] { new CqlProjectionColumn { Field = "Version", Alias = "Revision" } },
            Columns = new[]
            {
                new CqlGroupedColumn { Alias = "Revision", KeyAlias = "Revision" },
                new CqlGroupedColumn { Alias = "Total", Operation = "LongCount" }
            },
            MaximumGroups = 10
        };
        var counted = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(request, actor, provider), provider, target);
        Assert.IsFalse(counted.IsError, counted.Message);
        var count = await CosmicGroupedExecutor.ExecuteAsync(counted.Result, new[] { adapter });
        Assert.IsFalse(count.IsError, count.Message + count.Exception);
        var original = count.Result.Providers.Single().Rows.Single();
        CollectionAssert.AreEqual(new object[] { 1, 2L }, original.Values.ToArray());

        request.OrderBy = "None";
        Assert.ThrowsException<ArgumentException>(() => CqlCompiler.CompileGrouped(request, actor, provider));
        request.Columns = new[] { request.Columns[0] };
        request.Take = 1;
        Assert.ThrowsException<ArgumentException>(() => CqlCompiler.CompileGrouped(request, actor, provider));
        request.Take = null;
        request.ResultTake = 1;
        var intent = CqlCompiler.CompileGrouped(request, actor, provider);
        Assert.AreEqual(0, intent.Source.Orderings.Count);
        var distinct = CosmicGroupedPlanner.Plan(intent, provider, target);
        Assert.IsFalse(distinct.IsError, distinct.Message);
        var selected = await CosmicGroupedExecutor.ExecuteAsync(distinct.Result, new[] { adapter });
        Assert.IsFalse(selected.IsError, selected.Message + selected.Exception);
        CollectionAssert.AreEqual(new object[] { 1 }, selected.Result.Providers.Single().Rows.Single().Values.ToArray());
        var first = ids[0];
        var second = ids[1];
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .Where(row => row.Id == first || row.Id == second).Result;
        var root = CosmicLinq.Create(NativeManager(adapter), adapter, source).Result;
        var versions = root.OrderBy(row => row.Id).Select(row => row.Version);
        Assert.AreEqual(2, versions.Count());
        var reduced = await versions.Distinct().Where(version => version >= 1).Take(2).ToCosmicListAsync();
        Assert.IsFalse(reduced.IsError, reduced.Message + reduced.Exception);
        CollectionAssert.AreEqual(new[] { 1 }, reduced.Result.ToArray());
        Assert.AreEqual(1, versions.Distinct().Count());
        Assert.ThrowsException<CosmicLinqException>(() => versions.Distinct(EqualityComparer<int>.Default));
        checks.Add(new { provider = provider.ToString(), distinctWire = "two visible rows collapse to one native scalar key in SQL and LINQ" });
    }
}

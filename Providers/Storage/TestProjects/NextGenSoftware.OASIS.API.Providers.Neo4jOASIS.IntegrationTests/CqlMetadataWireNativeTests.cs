extern alias ShippingWeb4;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
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
        request.Filters[0].Value = JsonSerializer.SerializeToElement(new[] { ids[0], ids[1], ids[3] });
        request.Keys = new[]
        {
            new CqlProjectionColumn { MetadataPath = new[] { "i64" }, ValueType = "Int64", Nullable = true, Alias = "ExactValue" }
        };
        request.Columns = new[] { new CqlGroupedColumn { Alias = "ExactValue", KeyAlias = "ExactValue" } };
        request.ResultTake = 5;
        var metadataDistinct = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(request, actor, provider), provider, target);
        Assert.IsFalse(metadataDistinct.IsError, metadataDistinct.Message);
        var metadataRows = await CosmicGroupedExecutor.ExecuteAsync(metadataDistinct.Result, new[] { adapter });
        Assert.IsFalse(metadataRows.IsError, metadataRows.Message + metadataRows.Exception);
        CollectionAssert.AreEquivalent(new object[] { 9007199254740992L, 9007199254740993L },
            metadataRows.Result.Providers.Single().Rows.Select(row => row.Values.Single()).ToArray());

        request.OrderBy = "Name";
        request.Columns = new[] { new CqlGroupedColumn { Alias = "Count", Operation = "LongCount" } };
        var metadataCount = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(request, actor, provider), provider, target);
        Assert.IsFalse(metadataCount.IsError, metadataCount.Message);
        var metadataCounts = await CosmicGroupedExecutor.ExecuteAsync(metadataCount.Result, new[] { adapter });
        Assert.IsFalse(metadataCounts.IsError, metadataCounts.Message + metadataCounts.Exception);
        CollectionAssert.AreEquivalent(new object[] { 1L, 2L },
            metadataCounts.Result.Providers.Single().Rows.Select(row => row.Values.Single()).ToArray());

        request.Filters[0].Value = JsonSerializer.SerializeToElement(new[] { ids[0] });
        request.Columns = new[]
        {
            new CqlGroupedColumn
            {
                Alias = "Total",
                Operation = "Sum",
                Selection = new ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlScalarV1Selection
                    { MetadataPath = new[] { "decimal" }, ValueType = "Decimal", Nullable = true }
            }
        };
        var decimalPlan = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(request, actor, provider), provider, target);
        Assert.IsFalse(decimalPlan.IsError, decimalPlan.Message);
        var decimalRows = await CosmicGroupedExecutor.ExecuteAsync(decimalPlan.Result, new[] { adapter });
        Assert.IsFalse(decimalRows.IsError, decimalRows.Message + decimalRows.Exception);
        CollectionAssert.AreEqual(new object[] { 1.0000000000000000000000000001m },
            decimalRows.Result.Providers.Single().Rows.Single().Values.ToArray());

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
        var flags = await root.Select(row => row.IsPublic).Distinct().ToCosmicListAsync();
        Assert.IsFalse(flags.IsError, flags.Message + flags.Exception);
        CollectionAssert.AreEqual(new[] { true }, flags.Result.ToArray());

        var identifiers = await root.OrderBy(row => row.Id).Select(row => row.Id).Distinct().ToCosmicListAsync();
        Assert.IsFalse(identifiers.IsError, identifiers.Message + identifiers.Exception);
        CollectionAssert.AreEqual(new[] { first, second }.OrderBy(id => id).ToArray(), identifiers.Result.ToArray());

        var fixtureIds = ids;
        var fixtureSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .Where(row => fixtureIds.Contains(row.Id)).Result;
        var fixtureRows = CosmicLinq.Create(NativeManager(adapter), adapter, fixtureSource).Result;
        var labels = await fixtureRows.OrderBy(row => row.Name).Select(row => row.Description)
            .Distinct().ToCosmicListAsync();
        var exactIntegers = await fixtureRows.OrderBy(row => row.Name)
            .Select(row => CosmicQueryMetadata.Value<long?>(row, "i64"))
            .Distinct().ToCosmicListAsync();
        Assert.IsFalse(exactIntegers.IsError, exactIntegers.Message + exactIntegers.Exception);
        CollectionAssert.AreEqual(new long?[] { 9007199254740993L, null, 9007199254740992L },
            exactIntegers.Result.ToArray());
        var exactTimes = await fixtureRows.OrderBy(row => row.Name)
            .Select(row => CosmicQueryMetadata.Value<DateTime?>(row, "distinctUtc"))
            .Distinct().ToCosmicListAsync();
        Assert.IsFalse(exactTimes.IsError, exactTimes.Message + exactTimes.Exception);
        CollectionAssert.AreEqual(new DateTime?[]
        {
            new DateTime(638700000000000001L, DateTimeKind.Utc),
            null,
            new DateTime(638700000000000002L, DateTimeKind.Utc)
        }, exactTimes.Result.ToArray());
        Assert.IsTrue(exactTimes.Result.Where(value => value.HasValue)
            .All(value => value.Value.Kind == DateTimeKind.Utc));
        var unsignedIntegers = await fixtureRows.OrderBy(row => row.Name)
            .Select(row => CosmicQueryMetadata.Value<ulong?>(row, "u64"))
            .Distinct().ToCosmicListAsync();
        Assert.IsFalse(unsignedIntegers.IsError, unsignedIntegers.Message + unsignedIntegers.Exception);
        CollectionAssert.AreEqual(new ulong?[] { ulong.MaxValue, null, ulong.MaxValue - 1 },
            unsignedIntegers.Result.ToArray());

        var decimals = await fixtureRows.OrderBy(row => row.Name)
            .Select(row => CosmicQueryMetadata.Value<decimal?>(row, "decimal"))
            .Distinct().ToCosmicListAsync();
        Assert.IsFalse(decimals.IsError, decimals.Message + decimals.Exception);
        CollectionAssert.AreEqual(new decimal?[] { 1.0000000000000000000000000001m, 1m, null },
            decimals.Result.ToArray());
        async Task AssertScalarDistinct<T>(string key, T firstValue, T secondValue) where T : struct
        {
            var values = await fixtureRows.OrderBy(row => row.Name)
                .Select(row => CosmicQueryMetadata.Value<T?>(row, key))
                .Distinct().ToCosmicListAsync();
            Assert.IsFalse(values.IsError, values.Message + values.Exception);
            CollectionAssert.AreEqual(new T?[] { firstValue, null, secondValue }, values.Result.ToArray());
        }

        await AssertScalarDistinct("narrow", (byte)1, (byte)2);
        await AssertScalarDistinct("narrow", (sbyte)1, (sbyte)2);
        await AssertScalarDistinct("narrow", (short)1, (short)2);
        await AssertScalarDistinct("narrow", (ushort)1, (ushort)2);
        await AssertScalarDistinct("u32", uint.MaxValue, uint.MaxValue - 1);
        await AssertScalarDistinct("float", 1f + 1.1920929e-7f, 1f);
        await AssertScalarDistinct("double", 1d + 2.220446049250313e-16d, 1d);
        await AssertScalarDistinct("distinctHolonType", HolonType.Avatar, HolonType.Quest);
        await AssertScalarDistinct("distinctProviderType", ProviderType.MongoDBOASIS, ProviderType.Neo4jOASIS);
        Assert.IsFalse(labels.IsError, labels.Message + labels.Exception);
        CollectionAssert.AreEqual(new string?[] { "case", null, "Case" }, labels.Result.ToArray());

        Assert.IsInstanceOfType<ICosmicQueryJoinStorageProvider>(adapter);
        var joinAdapter = (ICosmicQueryJoinStorageProvider)adapter;
        var joinTargets = new[] { new CosmicJoinTarget(provider, adapter.QueryCapabilities, joinAdapter.JoinCapabilities) };
        var scope = CosmicQueryScope.Named(provider).Result;
        var leftIds = new[] { ids[0], ids[4] };
        var rightIds = new[] { ids[1], ids[2], ids[3] };
        var left = CosmicQuery<Holon>.Create(scope).Where(row => leftIds.Contains(row.Id));
        var right = CosmicQuery<Holon>.Create(scope).Where(row => rightIds.Contains(row.Id));

        var inner = left.Join(right, row => new { row.Version, row.Description },
            row => new { row.Version, row.Description }).Select((leftRow, rightRow) => rightRow.Id);
        Assert.IsFalse(inner.IsError, inner.Message);
        var innerPlan = CosmicJoinPlanner.Plan(inner.Result, provider, joinTargets);
        Assert.IsFalse(innerPlan.IsError, innerPlan.Message);
        var innerRows = await CosmicJoinExecutor.ExecuteAsync(innerPlan.Result, new[] { joinAdapter });
        Assert.IsFalse(innerRows.IsError, innerRows.Message + innerRows.Exception);
        Assert.AreEqual(ids[1], innerRows.Result.Providers.Single().Rows.Single().Value);

        var outer = left.LeftJoin(right, row => new { row.Version, row.Description },
            row => new { row.Version, row.Description })
            .Select((leftRow, rightRow) => new { LeftId = leftRow.Id, Present = rightRow != null });
        Assert.IsFalse(outer.IsError, outer.Message);
        var outerPlan = CosmicJoinPlanner.Plan(outer.Result, provider, joinTargets);
        Assert.IsFalse(outerPlan.IsError, outerPlan.Message);
        var outerRows = await CosmicJoinExecutor.ExecuteAsync(outerPlan.Result, new[] { joinAdapter });
        Assert.IsFalse(outerRows.IsError, outerRows.Message + outerRows.Exception);
        var presence = outerRows.Result.Providers.Single().Rows.ToDictionary(row => row.Value.LeftId, row => row.Value.Present);
        Assert.AreEqual(2, presence.Count);
        Assert.IsTrue(presence[ids[0]]);
        Assert.IsFalse(presence[ids[4]]);

        var cross = left.Result.CrossJoin(right.Result).Select((leftRow, rightRow) =>
            new { LeftId = leftRow.Id, RightId = rightRow.Id });
        Assert.IsFalse(cross.IsError, cross.Message);
        var crossPlan = CosmicJoinPlanner.Plan(cross.Result, provider, joinTargets);
        Assert.IsFalse(crossPlan.IsError, crossPlan.Message);
        var crossRows = await CosmicJoinExecutor.ExecuteAsync(crossPlan.Result, new[] { joinAdapter });
        Assert.IsFalse(crossRows.IsError, crossRows.Message + crossRows.Exception);
        var expectedCross = leftIds.SelectMany(l => rightIds.Select(r => (l, r))).ToArray();
        var actualCross = crossRows.Result.Providers.Single().Rows.Select(row => (row.Value.LeftId, row.Value.RightId)).ToArray();
        CollectionAssert.AreEquivalent(expectedCross, actualCross);

        checks.Add(new { provider = provider.ToString(), distinctWire = "native SQL and LINQ Int32, nullable Int64, Boolean, Guid, ordinal nullable String exact nullable UTC timestamps, nullable UInt64, exact nullable Decimal all narrower integral domains, Single, Double, HolonType and ProviderType; composite nullable join keys exclude null and retain unmatched left rows" });
    }
}

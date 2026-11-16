extern alias ShippingWeb4;

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using CqlGroupedRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlGroupedV1Request;
using CqlGroupedFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlGroupedV1Filter;
using CqlGroupedColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlGroupedV1Column;
using CqlProjectionColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlProjectionV1Column;
using CqlRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Request;
using CqlFilter = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Filter;
using CqlOperator = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Operator;
using CqlScalar = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlV1Scalar;
using CqlJoinRequest = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Request;
using CqlJoinSource = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Source;
using CqlJoinColumn = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Column;
using CqlJoinKey = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlJoinV1Key;
using CqlJoinScalar = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlScalarV1Selection;
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

        async Task AssertMatch(string key, CqlOperator operation, bool matches, string? integer = null, string? text = null)
        {
            var filter = new CqlFilter { MetadataPath = new[] { key }, Operator = operation };
            if (integer != null)
            {
                filter.Scalar = CqlScalar.Integer;
                filter.Value = JsonSerializer.SerializeToElement(integer);
            }

            if (text != null) filter.Value = JsonSerializer.SerializeToElement(text);
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
        await AssertMatch("text", CqlOperator.NotContains, false, text: "Café");
        await AssertMatch("text", CqlOperator.NotContains, true, text: "tea");
        await AssertMatch("text", CqlOperator.NotStartsWith, false, text: "Caf");
        await AssertMatch("text", CqlOperator.NotStartsWith, true, text: "caf");
        await AssertMatch("text", CqlOperator.NotEndsWith, false, text: "😀");
        await AssertMatch("text", CqlOperator.NotEndsWith, true, text: "tea");
        await AssertMatch("presentNull", CqlOperator.NotContains, false, text: "tea");
        await AssertMatch("missing", CqlOperator.NotContains, false, text: "tea");
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
        var nullFilterRequest = new CqlRequest
        {
            Filters = new[]
            {
                new CqlFilter { Field = "Id", Operator = CqlOperator.In,
                    Value = JsonSerializer.SerializeToElement(new[] { ids[0], ids[4] }) },
                new CqlFilter { Field = "Description", Operator = CqlOperator.IsNull }
            },
            OrderBy = "Id",
            Take = 2
        };
        foreach (var (operation, expected) in new[] { (CqlOperator.IsNull, ids[4]), (CqlOperator.IsNotNull, ids[0]) })
        {
            nullFilterRequest.Filters[1].Operator = operation;
            var filterPlan = CosmicQueryPlanner.Plan(CqlCompiler.Compile(nullFilterRequest, actor, provider), provider,
                new[] { new CosmicQueryTarget(provider, adapter.QueryCapabilities) });
            Assert.IsFalse(filterPlan.IsError, filterPlan.Message);
            var filtered = await CosmicQueryExecutor.ExecuteAsync(filterPlan.Result, new[] { adapter });
            Assert.IsFalse(filtered.IsError, filtered.Message + filtered.Exception);
            Assert.AreEqual(expected, filtered.Result.Providers.Single().Rows.Single().Id);
        }

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
        var decimalSelection = new ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlScalarV1Selection
            { MetadataPath = new[] { "decimal" }, ValueType = "Decimal", Nullable = true };
        request.Columns = new[]
        {
            new CqlGroupedColumn { Alias = "Total", Operation = "Sum", Selection = decimalSelection },
            new CqlGroupedColumn { Alias = "Mean", Operation = "Average", Selection = decimalSelection }
        };
        var decimalPlan = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(request, actor, provider), provider, target);
        Assert.IsFalse(decimalPlan.IsError, decimalPlan.Message);
        var decimalRows = await CosmicGroupedExecutor.ExecuteAsync(decimalPlan.Result, new[] { adapter });
        Assert.IsFalse(decimalRows.IsError, decimalRows.Message + decimalRows.Exception);
        CollectionAssert.AreEqual(new object[] { 1.0000000000000000000000000001m, 1.0000000000000000000000000001m },
            decimalRows.Result.Providers.Single().Rows.Single().Values.ToArray());

        var exactPaths = new CqlGroupedRequest
        {
            Filters = new[] { new CqlFilter
            {
                Field = "Id", Operator = CqlOperator.Equal, Value = JsonSerializer.SerializeToElement(ids[0])
            } },
            Keys = new[]
            {
                new CqlProjectionColumn { MetadataPath = new[] { "catalog.v1" }, ValueType = "String", Nullable = true, Alias = "Literal" },
                new CqlProjectionColumn { MetadataPath = new[] { "catalog", "v1" }, ValueType = "String", Nullable = true, Alias = "Nested" }
            },
            Columns = new[]
            {
                new CqlGroupedColumn { Alias = "Literal", KeyAlias = "Literal" },
                new CqlGroupedColumn { Alias = "Nested", KeyAlias = "Nested" }
            },
            OrderBy = "None",
            MaximumGroups = 10
        };
        var exactDistinct = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(exactPaths, actor, provider), provider, target);
        Assert.IsFalse(exactDistinct.IsError, exactDistinct.Message);
        var exactRows = await CosmicGroupedExecutor.ExecuteAsync(exactDistinct.Result, new[] { adapter });
        Assert.IsFalse(exactRows.IsError, exactRows.Message + exactRows.Exception);
        CollectionAssert.AreEqual(new object[] { "literal", "nested" },
            exactRows.Result.Providers.Single().Rows.Single().Values.ToArray());

        exactPaths.OrderBy = "Name";
        exactPaths.Columns = exactPaths.Columns.Append(new CqlGroupedColumn { Alias = "Total", Operation = "LongCount" }).ToArray();
        var exactGrouped = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(exactPaths, actor, provider), provider, target);
        Assert.IsFalse(exactGrouped.IsError, exactGrouped.Message);
        var exactCounts = await CosmicGroupedExecutor.ExecuteAsync(exactGrouped.Result, new[] { adapter });
        Assert.IsFalse(exactCounts.IsError, exactCounts.Message + exactCounts.Exception);
        CollectionAssert.AreEqual(new object[] { "literal", "nested", 1L },
            exactCounts.Result.Providers.Single().Rows.Single().Values.ToArray());

        var selectedCount = new CqlGroupedRequest
        {
            Filters = new[] { new CqlFilter
            {
                Field = "Id", Operator = CqlOperator.In,
                Value = JsonSerializer.SerializeToElement(new[] { ids[0], ids[1], ids[3], ids[4] })
            } },
            Keys = new[] { new CqlProjectionColumn { Field = "Version", Alias = "Revision" } },
            Columns = new[]
            {
                new CqlGroupedColumn { Alias = "Rows", Operation = "LongCount" },
                new CqlGroupedColumn { Alias = "Descriptions", Operation = "LongCount",
                    Selection = new CqlJoinScalar { Field = "Description" } },
                new CqlGroupedColumn { Alias = "Labels", Operation = "LongCount",
                    Selection = new CqlJoinScalar { MetadataPath = new[] { "catalog.v1" }, ValueType = "String", Nullable = true } }
            },
            ResultFilters = new[] { new CqlGroupedFilter
            {
                Alias = "Descriptions", Operator = CqlOperator.Equal, Value = JsonSerializer.SerializeToElement("3")
            } },
            ResultOrder = new[] { new ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data.CqlGroupedV1Ordering
                { Alias = "Descriptions", Descending = true } },
            OrderBy = "Name",
            MaximumGroups = 10
        };
        var selectedCountPlan = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(selectedCount, actor, provider), provider, target);
        Assert.IsFalse(selectedCountPlan.IsError, selectedCountPlan.Message);
        var selectedCounts = await CosmicGroupedExecutor.ExecuteAsync(selectedCountPlan.Result, new[] { adapter });
        Assert.IsFalse(selectedCounts.IsError, selectedCounts.Message + selectedCounts.Exception);
        CollectionAssert.AreEqual(new object[] { 4L, 3L, 1L },
            selectedCounts.Result.Providers.Single().Rows.Single().Values.ToArray());

        var floatingRequest = new CqlGroupedRequest
        {
            Filters = new[] { new CqlFilter
            {
                Field = "Id", Operator = CqlOperator.In, Value = JsonSerializer.SerializeToElement(ids.Take(2).ToArray())
            } },
            Keys = new[] { new CqlProjectionColumn { Field = "Version", Alias = "Revision" } },
            Columns = new[]
            {
                new CqlGroupedColumn { Alias = "Revision", KeyAlias = "Revision" },
                new CqlGroupedColumn { Alias = "Total", Operation = "Sum", Selection = new CqlJoinScalar
                    { MetadataPath = new[] { "float" }, ValueType = "Single", Nullable = true } },
                new CqlGroupedColumn { Alias = "Mean", Operation = "Average", Selection = new CqlJoinScalar
                    { MetadataPath = new[] { "double" }, ValueType = "Double", Nullable = true } }
            },
            ResultFilters = new[]
            {
                new CqlGroupedFilter { Alias = "Total", Operator = CqlOperator.Equal, Value = JsonSerializer.SerializeToElement("2") },
                new CqlGroupedFilter { Alias = "Mean", Operator = CqlOperator.Equal, Value = JsonSerializer.SerializeToElement("1") }
            },
            MaximumGroups = 10
        };
        var floatingPlan = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(floatingRequest, actor, provider), provider, target);
        Assert.IsFalse(floatingPlan.IsError, floatingPlan.Message);
        var floatingRows = await CosmicGroupedExecutor.ExecuteAsync(floatingPlan.Result, new[] { adapter });
        Assert.IsFalse(floatingRows.IsError, floatingRows.Message + floatingRows.Exception);
        CollectionAssert.AreEqual(new object[] { 1, 2f, 1d },
            floatingRows.Result.Providers.Single().Rows.Single().Values.ToArray());

        var first = ids[0];
        var second = ids[1];
        var source = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .Where(row => row.Id == first || row.Id == second).Result;
        var root = CosmicLinq.Create(NativeManager(adapter), adapter, source).Result;
        var distinctCountSource = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.OwnerOrPublic(actor).Result)
            .Where(row => ids.Contains(row.Id)).Result;
        var countAdapter = (ICosmicQueryProjectionCompositionStorageProvider)adapter;
        var countTarget = new CosmicProjectionCompositionTarget(provider, countAdapter.QueryCapabilities,
            countAdapter.ProjectionCapabilities, countAdapter.CompositionCapabilities);
        async Task<long> SelectedCount<TValue>(CosmicQuery<Holon> selectedSource,
            System.Linq.Expressions.Expression<Func<Holon, TValue>> selector, bool distinct)
        {
            var query = CqlCompiler.CompileSelectedCount(selectedSource, selector, distinct);
            var plan = CosmicProjectedCardinalityPlanner.Plan<Holon, TValue, long>(query,
                CosmicScalarOperation.LongCount, provider, new[] { countTarget });
            Assert.IsFalse(plan.IsError, plan.Message);
            var result = await CosmicProjectedCardinalityExecutor.ExecuteAsync(plan.Result,
                new ICosmicQueryStorageProvider[] { countAdapter });
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            return result.Result.Providers.Single().Value;
        }
        Assert.AreEqual(2L, await SelectedCount(distinctCountSource, row => row.Description, distinct: true));
        Assert.AreEqual(0L, await SelectedCount(distinctCountSource.Take(0).Result, row => row.Description, distinct: true));
        Assert.AreEqual(3L, await SelectedCount(distinctCountSource, row => row.Description, distinct: false));
        Assert.AreEqual(4L, await SelectedCount(distinctCountSource, row => row.Id, distinct: false));
        Assert.AreEqual(0L, await SelectedCount(distinctCountSource.Take(0).Result, row => row.Description, distinct: false));

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

        foreach (var (operation, pattern, expected) in new[]
        {
            (CqlOperator.NotContains, "Case", new[] { "case" }),
            (CqlOperator.NotStartsWith, "Ca", new[] { "case" }),
            (CqlOperator.NotEndsWith, "ase", Array.Empty<string>()),
            (CqlOperator.NotContains, "", Array.Empty<string>())
        })
        {
            var groupedText = new CqlGroupedRequest
            {
                Filters = new[] { new CqlFilter { Field = "Id", Operator = CqlOperator.In,
                    Value = JsonSerializer.SerializeToElement(ids) } },
                Keys = new[] { new CqlProjectionColumn { Field = "Description", Alias = "Label" } },
                Columns = new[] { new CqlGroupedColumn { Alias = "Label", KeyAlias = "Label" } },
                ResultFilters = new[] { new CqlGroupedFilter { Alias = "Label", Operator = operation,
                    Value = JsonSerializer.SerializeToElement(pattern) } },
                MaximumGroups = 10
            };
            var groupedTextPlan = CosmicGroupedPlanner.Plan(CqlCompiler.CompileGrouped(groupedText, actor, provider), provider, target);
            Assert.IsFalse(groupedTextPlan.IsError, groupedTextPlan.Message);
            var remaining = await CosmicGroupedExecutor.ExecuteAsync(groupedTextPlan.Result, new[] { adapter });
            Assert.IsFalse(remaining.IsError, remaining.Message + remaining.Exception);
            CollectionAssert.AreEqual(expected, remaining.Result.Providers.Single().Rows
                .Select(result => (string)result.Values.Single()).ToArray());
        }


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

        CqlJoinSource CrossSource(Guid[] inputs) => new()
        {
            OrderBy = "Id",
            Take = inputs.Length,
            Filters = new[] { new CqlFilter
            {
                Field = "Id", Operator = CqlOperator.In, Value = JsonSerializer.SerializeToElement(inputs)
            } }
        };
        var visibleRightIds = new[] { ids[0], ids[1], ids[3] };
        var crossRequest = new CqlJoinRequest
        {
            Provider = provider.ToString(), Kind = "Cross", Left = CrossSource(leftIds), Right = CrossSource(visibleRightIds),
            MaximumLeftRows = 2, MaximumRightRows = 3, MaximumPairs = 6, MaximumResults = 6,
            Columns = new[]
            {
                new CqlJoinColumn { Alias = "LeftId", Source = "Left", Selection = new CqlJoinScalar { Field = "Id" } },
                new CqlJoinColumn { Alias = "RightId", Source = "Right", Selection = new CqlJoinScalar { Field = "Id" } }
            }
        };
        crossRequest.Kind = "Inner";
        Assert.ThrowsException<ArgumentException>(() => CqlCompiler.CompileJoin(crossRequest, actor, provider));
        crossRequest.Kind = "Cross";
        crossRequest.Keys = new[] { new CqlJoinKey
        {
            Alias = "Id", Left = new CqlJoinScalar { Field = "Id" }, Right = new CqlJoinScalar { Field = "Id" }
        } };
        Assert.ThrowsException<ArgumentException>(() => CqlCompiler.CompileJoin(crossRequest, actor, provider));
        crossRequest.Keys = Array.Empty<CqlJoinKey>();

        var crossPlan = CosmicJoinPlanner.Plan(CqlCompiler.CompileJoin(crossRequest, actor, provider), provider, joinTargets);
        Assert.IsFalse(crossPlan.IsError, crossPlan.Message);
        var crossRows = await CosmicJoinExecutor.ExecuteAsync(crossPlan.Result, new[] { joinAdapter });
        Assert.IsFalse(crossRows.IsError, crossRows.Message + crossRows.Exception);
        var expectedCross = leftIds.SelectMany(l => visibleRightIds.Select(r => (l, r))).ToArray();
        var actualCross = crossRows.Result.Providers.Single().Rows
            .Select(row => ((Guid)row.Value["LeftId"], (Guid)row.Value["RightId"])).ToArray();
        CollectionAssert.AreEquivalent(expectedCross, actualCross);

        crossRequest.Right = CrossSource(rightIds);
        var protectedCross = CosmicJoinPlanner.Plan(CqlCompiler.CompileJoin(crossRequest, actor, provider), provider, joinTargets);
        Assert.IsFalse(protectedCross.IsError, protectedCross.Message);
        var protectedRows = await CosmicJoinExecutor.ExecuteAsync(protectedCross.Result, new[] { joinAdapter });
        Assert.IsFalse(protectedRows.IsError, protectedRows.Message + protectedRows.Exception);
        var expectedVisible = leftIds.SelectMany(l => new[] { ids[1], ids[3] }.Select(r => (l, r))).ToArray();
        var actualVisible = protectedRows.Result.Providers.Single().Rows
            .Select(row => ((Guid)row.Value["LeftId"], (Guid)row.Value["RightId"])).ToArray();
        CollectionAssert.AreEquivalent(expectedVisible, actualVisible);

        crossRequest.Right = CrossSource(visibleRightIds);
        crossRequest.MaximumPairs = 5;
        var boundedCross = CosmicJoinPlanner.Plan(CqlCompiler.CompileJoin(crossRequest, actor, provider), provider, joinTargets);
        Assert.IsFalse(boundedCross.IsError, boundedCross.Message);
        var boundedRows = await CosmicJoinExecutor.ExecuteAsync(boundedCross.Result, new[] { joinAdapter });
        Assert.IsTrue(boundedRows.IsError, "Exceeding pair work must never return partial Cartesian results.");

        var fullRequest = new CqlJoinRequest
        {
            Provider = provider.ToString(), Kind = "Full", Left = CrossSource(leftIds),
            Right = CrossSource(new[] { ids[0], ids[1], ids[2], ids[3] }),
            MaximumLeftRows = 2, MaximumRightRows = 4, MaximumPairs = 8, MaximumResults = 8,
            Keys = new[] { new CqlJoinKey
            {
                Alias = "Id", Left = new CqlJoinScalar { Field = "Id" }, Right = new CqlJoinScalar { Field = "Id" }
            } },
            Columns = new[]
            {
                new CqlJoinColumn { Alias = "LeftId", Source = "Left", AllowMissing = true, Selection = new CqlJoinScalar { Field = "Id" } },
                new CqlJoinColumn { Alias = "RightId", Source = "Right", AllowMissing = true, Selection = new CqlJoinScalar { Field = "Id" } }
            }
        };
        var fullPlan = CosmicJoinPlanner.Plan(CqlCompiler.CompileJoin(fullRequest, actor, provider), provider, joinTargets);
        Assert.IsFalse(fullPlan.IsError, fullPlan.Message);
        var fullRows = await CosmicJoinExecutor.ExecuteAsync(fullPlan.Result, new[] { joinAdapter });
        Assert.IsFalse(fullRows.IsError, fullRows.Message + fullRows.Exception);
        var expectedFull = leftIds.OrderBy(id => id).Select(id => ((Guid?)id, id == ids[0] ? (Guid?)id : null))
            .Concat(new[] { ids[1], ids[3] }.OrderBy(id => id).Select(id => ((Guid?)null, (Guid?)id))).ToArray();
        var actualFull = fullRows.Result.Providers.Single().Rows.ToArray();
        CollectionAssert.AreEqual(expectedFull, actualFull
            .Select(row => ((Guid?)row.Value["LeftId"], (Guid?)row.Value["RightId"])).ToArray());
        Assert.IsTrue(actualFull.All(row => row.HasLeft == (row.Value["LeftId"] != null)
            && row.HasRight == (row.Value["RightId"] != null)));

        fullRequest.MaximumPairs = 3;
        var fullLimited = CosmicJoinPlanner.Plan(CqlCompiler.CompileJoin(fullRequest, actor, provider), provider, joinTargets);
        Assert.IsFalse(fullLimited.IsError, fullLimited.Message);
        Assert.IsTrue((await CosmicJoinExecutor.ExecuteAsync(fullLimited.Result, new[] { joinAdapter })).IsError,
            "Full joins cannot publish a partial pair budget result.");

        fullRequest.MaximumPairs = 8;
        fullRequest.Left = CrossSource(new[] { Guid.NewGuid() });
        var emptyLeft = CosmicJoinPlanner.Plan(CqlCompiler.CompileJoin(fullRequest, actor, provider), provider, joinTargets);
        Assert.IsFalse(emptyLeft.IsError, emptyLeft.Message);
        var rightOnly = await CosmicJoinExecutor.ExecuteAsync(emptyLeft.Result, new[] { joinAdapter });
        Assert.IsFalse(rightOnly.IsError, rightOnly.Message + rightOnly.Exception);
        CollectionAssert.AreEqual(visibleRightIds.OrderBy(id => id).ToArray(), rightOnly.Result.Providers.Single().Rows
            .Select(row => { Assert.IsFalse(row.HasLeft); Assert.IsTrue(row.HasRight); return (Guid)row.Value["RightId"]; }).ToArray());

        checks.Add(new { provider = provider.ToString(), distinctWire = "native SQL and LINQ Int32, nullable Int64, Boolean, Guid, ordinal nullable String exact nullable UTC timestamps, nullable UInt64, exact nullable Decimal all narrower integral domains, Single, Double, HolonType and ProviderType; composite nullable join keys exclude null and retain unmatched left rows" });
    }
}

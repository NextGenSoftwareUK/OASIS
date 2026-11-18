using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyNativeDoubleProjection(INativeDataProjectionQueryProvider provider,
        string source, NativeRecordFilter first, string integerField, bool storesNaN, List<object> checks)
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var expected = new[] { 3.25d, -double.MaxValue, double.MaxValue, double.Epsilon, 0d };
        var names = new[] { "rate", "low", "high", "tiny", "zero" };
        var columns = names.Select((name, index) => new NativeProjectionColumn(name, "D" + index, typeof(double))).ToArray();
        var projected = await provider.QueryNativeProjectionRecordsAsync(source, new[] { first }, columns, 2);
        Assert.IsFalse(projected.IsError, projected.Message + projected.Exception);
        var row = projected.Result.Single();
        for (var i = 0; i < columns.Length; i++)
        {
            Assert.IsInstanceOfType(row.Cells[i].Value, typeof(double));
            Assert.AreEqual(expected[i], (double)row.Cells[i].Value);
        }

        var operators = new[] { CosmicQueryComparisonOperator.Equal, CosmicQueryComparisonOperator.NotEqual,
            CosmicQueryComparisonOperator.GreaterThan, CosmicQueryComparisonOperator.GreaterThanOrEqual,
            CosmicQueryComparisonOperator.LessThan, CosmicQueryComparisonOperator.LessThanOrEqual };
        foreach (var op in operators)
        {
            var result = await provider.QueryNativeProjectionRecordsAsync(source,
                new[] { new NativeRecordFilter("rate", op, 3.25d) }, new[] { columns[0] }, 10);
            Assert.IsFalse(result.IsError, result.Message + result.Exception);
            Assert.AreEqual(op is CosmicQueryComparisonOperator.Equal or CosmicQueryComparisonOperator.GreaterThanOrEqual
                or CosmicQueryComparisonOperator.LessThanOrEqual ? 1 : 0, result.Result.Count);
            if (result.Result.Count == 1) Assert.AreEqual(row.NativeKey, result.Result[0].NativeKey);
        }
        foreach (var field in storesNaN ? new[] { "badNaN", "badInf", "badNegativeInf" } : new[] { "badInf", "badNegativeInf" })
        {
            var invalid = await provider.QueryNativeProjectionRecordsAsync(source, new[] { first },
                new[] { new NativeProjectionColumn(field, "Bad", typeof(double), true) }, 1);
            Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", invalid.ErrorCode, field);
            Assert.IsNull(invalid.Result);
            foreach (var op in operators)
            {
                var filtered = await provider.QueryNativeProjectionRecordsAsync(source,
                    new[] { new NativeRecordFilter(field, op, 0d) }, new[] { columns[0] }, 10);
                Assert.IsFalse(filtered.IsError, filtered.Message + filtered.Exception);
                Assert.AreEqual(0, filtered.Result.Count, field + " " + op);
            }
        }
        var wrongDomain = await provider.QueryNativeProjectionRecordsAsync(source, new[] { first },
            new[] { new NativeProjectionColumn(integerField, "Wrong", typeof(double)) }, 1);
        Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", wrongDomain.ErrorCode);
        Assert.IsNull(wrongDomain.Result);
        var optional = await provider.QueryNativeProjectionRecordsAsync(source, new[] { first },
            new[] { new NativeProjectionColumn("optionalDouble", "Optional", typeof(double), true) }, 1);
        Assert.IsFalse(optional.IsError, optional.Message);
        Assert.AreNotEqual(NativeProjectionValueState.Present, optional.Result.Single().Cells[0].State);
        var required = await provider.QueryNativeProjectionRecordsAsync(source, new[] { first },
            new[] { new NativeProjectionColumn("optionalDouble", "Required", typeof(double)) }, 1);
        Assert.AreEqual("NATIVE_PROJECTION_DATA_INVALID", required.ErrorCode);

        var identities = await provider.QueryNativeProjectionRecordsAsync(source, new[] { first },
            Array.Empty<NativeProjectionColumn>(), 2);
        Assert.IsFalse(identities.IsError, identities.Message + identities.Exception);
        Assert.AreEqual(row.NativeKey, identities.Result.Single().NativeKey);
        Assert.AreEqual(0, identities.Result.Single().Cells.Count);
        var identityBudget = NativeProjectionResults.Capture(source, Array.Empty<NativeProjectionColumn>(),
            identities.Result, maximumRows: 2, maximumPayloadBytes: 256);
        Assert.IsFalse(identityBudget.IsError, identityBudget.Message);
        checks.Add(new { provider = provider.NativeDataProviderType.ToString(),
            nativeProjection = "native identity without selecting any field, including invalid and oversized unused values",
            identityProjectionVerified = true });
        var actor = Guid.NewGuid();
        var mapping = new VirtualHolonMapping(Guid.NewGuid(), 1, actor, provider.NativeDataProviderType,
            source, VirtualHolonMapping.SandboxConnectionName, provider.DescribeVirtualQueryConnection(null),
            new Dictionary<string, string> { ["score"] = "rate" });
        var request = new CqlProjectionV1Request
        {
            OrderBy = "None", VirtualMappingId = mapping.Id, Take = 2,
            SelectedOrder = new[] { new CqlGroupedV1Ordering { Alias = "Score", Descending = true } },
            Filters = new[] { new CqlV1Filter { MetadataPath = new[] { "score" }, Scalar = CqlV1Scalar.Double,
                Operator = CqlV1Operator.GreaterThanOrEqual, Value = JsonSerializer.SerializeToElement("3.25") } },
            Columns = new[] { new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                new CqlProjectionV1Column { MetadataPath = new[] { "score" }, ValueType = "Double", Alias = "Score" } }
        };
        var compiled = CqlV1Compiler.CompileVirtualProjection(request);
        var mapped = await mapping.QueryProjectionAsync(actor, provider, null, compiled.Matches, compiled.Columns, 100);
        Assert.IsFalse(mapped.IsError, mapped.Message + mapped.Exception);
        var virtualRow = mapped.Result.Single();
        Assert.AreEqual(3.25d, compiled.Select(virtualRow)[1]);
        Assert.AreEqual(actor, virtualRow.OwnerAvatarId);
        var denied = await mapping.QueryProjectionAsync(Guid.NewGuid(), provider, null, compiled.Matches, compiled.Columns, 100);
        Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN", denied.ErrorCode);
        var aggregateIntent = CqlV1Compiler.CompileProjection(request, actor, provider.NativeDataProviderType,
            CosmicQueryReadAccess.OwnerOrPublic(actor).Result, 100);
        var storedAggregate = CosmicProjectionResults.Materialize(aggregateIntent,
            new[] { new CosmicProjectionRow(Guid.NewGuid(), 3.75d) });
        Assert.IsFalse(storedAggregate.IsError, storedAggregate.Message);
        foreach (var operation in new[] { CosmicScalarOperation.Min, CosmicScalarOperation.Max,
            CosmicScalarOperation.Sum, CosmicScalarOperation.Average })
        {
            var aggregate = CosmicMixedProjectionResults.Aggregate(aggregateIntent, actor, mapping.Id,
                storedAggregate.Result, mapped.Result, operation);
            Assert.IsFalse(aggregate.IsError, aggregate.Message);
            Assert.AreEqual(operation == CosmicScalarOperation.Min ? 3.25d
                : operation == CosmicScalarOperation.Max ? 3.75d
                : operation == CosmicScalarOperation.Sum ? 7d : 3.5d, aggregate.Result.Value);
        }
        foreach (var distinct in new[] { false, true })
        {
            var counted = CosmicMixedProjectionResults.CountValues(aggregateIntent, actor, mapping.Id,
                storedAggregate.Result, mapped.Result, distinct);
            Assert.IsFalse(counted.IsError, counted.Message);
            Assert.AreEqual(2L, counted.Result);
        }
        var groupSource = aggregateIntent.Source;
        var mixedKey = CosmicProjectionSelection<Holon>.Create("Score", h => CosmicQueryMetadata.Value<double>(h, new[] { "score" }));
        Assert.IsFalse(mixedKey.IsError, mixedKey.Message);
        var grouping = groupSource.GroupByColumns(new[] { mixedKey.Result }).Result;
        var groupedSelection = grouping.SelectColumns(new[] {
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Key(grouping,"Score",0).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(grouping,"Rows",groupSource.LongCount().Result).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(grouping,"Sum",groupSource.Sum(h=>CosmicQueryMetadata.Value<double>(h, new[] { "score" })).Result).Result,
            CosmicGroupedBinding<Holon,CosmicProjectionRecord>.Aggregate(grouping,"Mean",groupSource.Average(h=>CosmicQueryMetadata.Value<double>(h, new[] { "score" })).Result).Result
        }).Where(row => (long)row["Rows"] == 1)
          .OrderByDescending(row => (double)row["Score"]).Skip(1).Take(1);
        Assert.IsFalse(groupedSelection.IsError, groupedSelection.Message);
        var groupedPlan = CosmicMixedProjectionResults.PrepareGrouping(aggregateIntent, groupedSelection.Result);
        Assert.IsFalse(groupedPlan.IsError, groupedPlan.Message);
        var staleStoredGroups = CosmicProjectionResults.Materialize(aggregateIntent, new[] {
            new CosmicProjectionRow(virtualRow.Id,3.75d), new CosmicProjectionRow(Guid.NewGuid(),3.75d) });
        Assert.IsFalse(staleStoredGroups.IsError, staleStoredGroups.Message);
        var mixedGroups = CosmicMixedProjectionResults.Group(groupedPlan.Result, actor, mapping.Id,
            staleStoredGroups.Result, mapped.Result);
        Assert.IsFalse(mixedGroups.IsError, mixedGroups.Message);
        Assert.AreEqual(1,mixedGroups.Result.Count);
        Assert.AreEqual(3.25d,mixedGroups.Result[0]["Score"]);
        Assert.AreEqual(1L,mixedGroups.Result[0]["Rows"]);
        Assert.AreEqual(3.25d,mixedGroups.Result[0]["Sum"]);
        Assert.AreEqual(3.25d,mixedGroups.Result[0]["Mean"]);
        checks.Add(new { provider = provider.NativeDataProviderType.ToString(),
            mixedGroupingSdkVerified = true,
            grouping = "native Double, changed duplicate group membership, HAVING, selected order and one page" });
        checks.Add(new { provider = provider.NativeDataProviderType.ToString(),
            nativeSelectedCount = "real native Double and materialized values share exact selected counts",
            selectedCountVerified = true });
        checks.Add(new { provider = provider.NativeDataProviderType.ToString(),
            nativeAggregation = "real native Double and materialized values share four exact reductions",
            numericAggregationVerified = true });
        checks.Add(new { provider = provider.NativeDataProviderType.ToString(),
            nativeProjection = "finite Double extrema/subnormal/zero, six native comparisons, invalid-domain refusal and mapped compiler",
            doubleProjectionVerified = true, elapsedMilliseconds = watch.ElapsedMilliseconds });
    }
}

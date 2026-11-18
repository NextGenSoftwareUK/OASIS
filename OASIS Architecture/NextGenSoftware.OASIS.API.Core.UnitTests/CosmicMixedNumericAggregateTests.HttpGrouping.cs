using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicMixedNumericAggregateTests
{
    private static CqlMixedGroupingV1Request MixedGroupDescription() => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "Key", MetadataPath = new[] { "amount" },
            ValueType = "Int32", Nullable = true } },
        Columns = new[]
        {
            new CqlGroupedV1Column { Alias = "Key", KeyAlias = "Key" },
            new CqlGroupedV1Column { Alias = "Rows", Operation = "LongCount" },
            new CqlGroupedV1Column { Alias = "Sum", Operation = "Sum",
                Selection = new CqlScalarV1Selection { MetadataPath = new[] { "amount" }, ValueType = "Int32", Nullable = true } }
        },
        ResultFilters = new[] { new CqlGroupedV1Filter { Alias = "Rows",
            Operator = CqlV1Operator.GreaterThanOrEqual, Value = JsonSerializer.SerializeToElement(2L) } },
        ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = "Key", Descending = true } },
        ResultTake = 1
    };

    [Fact]
    public async Task TransportGroupingUsesTheOriginalMixedSourceAndLiveWinnerBeforeHaving()
    {
        var f = await Create(typeof(int), true, new object[] { 99, 2, 3 },
            new object[] { 2, 4 }, overlap: 1);
        var plan = CqlV1Compiler.CompileMixedGrouping(f.Query, MixedGroupDescription());
        Assert.Same(f.Query.Source, plan.Selection.Grouping.Source);
        var result = CosmicMixedProjectionResults.Group(plan, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message);
        var row = Assert.Single(result.Result);
        Assert.Equal(2, row["Key"]); Assert.Equal(2L, row["Rows"]); Assert.Equal(4, row["Sum"]);
    }

    [Fact]
    public async Task CompiledTransportGroupingRetainsItsCapturedPathsHavingAndPage()
    {
        var f = await Create(typeof(int), true, new object[] { 2 }, new object[] { 2, 3 });
        var description = MixedGroupDescription();
        var plan = CqlV1Compiler.CompileMixedGrouping(f.Query, description);
        description.Keys[0].MetadataPath[0] = "other";
        description.Columns[2].Selection.MetadataPath[0] = "other";
        description.ResultFilters[0].Value = JsonSerializer.SerializeToElement(100L);
        description.ResultOrder[0].Alias = "unknown";
        description.ResultSkip = 100;
        var result = CosmicMixedProjectionResults.Group(plan, Actor, f.Mapping, f.Stored, f.Live);
        Assert.False(result.IsError, result.Message);
        Assert.Equal(2, Assert.Single(result.Result)["Key"]);
    }

    [Theory]
    [InlineData("key")]
    [InlineData("selector")]
    [InlineData("having")]
    [InlineData("order")]
    [InlineData("budget")]
    [InlineData("computed-aggregate")]
    public async Task InvalidTransportGroupingIsRejectedBeforeAnyMixedRead(string invalid)
    {
        var f = await Create(typeof(int), true, new object[] { 2 }, new object[] { 3 });
        var description = MixedGroupDescription();
        switch (invalid)
        {
            case "key": description.Keys[0].MetadataPath[0] = "not-projected"; break;
            case "selector": description.Columns[2].Selection.MetadataPath[0] = "not-projected"; break;
            case "having": description.ResultFilters[0].Alias = "unknown"; break;
            case "order": description.ResultOrder[0].Alias = "unknown"; break;
            case "budget": description.MaximumGroups = 101; break;
            case "computed-aggregate": description.Columns[2].Selection.Function = CqlV1Function.Utf16Length; break;
        }
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileMixedGrouping(f.Query, description));
    }
}

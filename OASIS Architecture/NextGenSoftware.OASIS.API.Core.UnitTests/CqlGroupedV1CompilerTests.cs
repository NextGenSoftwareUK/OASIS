using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlGroupedV1CompilerTests
{
    private static CqlGroupedV1Request Request() => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "category", Field = "Name" } },
        Columns = new[]
        {
            new CqlGroupedV1Column { Alias = "Category", KeyAlias = "category" },
            new CqlGroupedV1Column { Alias = "Rows", Operation = "LongCount" },
            new CqlGroupedV1Column { Alias = "Mean", Operation = "Average",
                Selection = new CqlScalarV1Selection { MetadataPath = new[] { "stock" }, ValueType = "Int64", Nullable = true } }
        }
    };

    [Fact]
    public void BoundedNamedShapeRetainsExactAverageDomainAndUnboundedSourceIntent()
    {
        var result = CqlV1Compiler.CompileGrouped(Request(), Guid.NewGuid(), ProviderType.MongoDBOASIS);
        Assert.Null(result.Source.Limit);
        Assert.NotNull(result.Source.ReadAccess);
        Assert.Equal(25, result.Grouping.Options.MaximumGroups);
        Assert.Equal(0, result.Columns[0].KeyIndex);
        Assert.Equal(typeof(long), result.Columns[1].ValueType);
        Assert.Equal(typeof(decimal?), result.Columns[2].ValueType);
    }

    [Theory]
    [InlineData("Any")] [InlineData("count")] [InlineData("Sum")]
    public void UnknownOrIncompleteAggregatesCannotBecomeIntents(string operation)
    {
        var request = Request(); request.Columns = new[] { new CqlGroupedV1Column { Alias = "Bad", Operation = operation } };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void KeyAndAggregateAreMutuallyExclusiveAndAliasesMustResolve()
    {
        var request = Request(); request.Columns[0].Operation = "Count";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
        request = Request(); request.Columns[0].KeyAlias = "missing";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
        request = Request(); request.Columns[0].Alias = "invalid alias";
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void SourceWindowAndGroupBudgetsRemainIndependent()
    {
        var request = Request(); request.Skip = 3; request.Take = 137; request.MaximumGroups = 1;
        var intent = CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null);
        Assert.Equal(3, intent.Source.Offset); Assert.Equal(137, intent.Source.Limit);
        Assert.Equal(1, intent.Grouping.Options.MaximumGroups);
        request.MaximumGroups = 101;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void UnknownWireMembersCannotSupplyActorOrPolicyOverrides()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<CqlGroupedV1Request>("{\"Actor\":\"spoof\"}"));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(Request(), Guid.Empty, null));
    }

    [Fact]
    public void ResultOrderingUsesSelectedTypesAndPreservesIndependentSourceWindow()
    {
        var request = Request(); request.OrderBy = "Version"; request.Skip = 2; request.Take = 10;
        request.ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = "Rows", Descending = true },
            new CqlGroupedV1Ordering { Alias = "Category" } };
        var result = CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null);
        Assert.Equal(2, result.Source.Offset); Assert.Equal(10, result.Source.Limit);
        Assert.Equal(2, result.Orderings.Count);
        Assert.Equal(1, result.Orderings[0].ColumnIndex); Assert.True(result.Orderings[0].Descending);
        Assert.Equal(0, result.Orderings[1].ColumnIndex); Assert.False(result.Orderings[1].Descending);
    }

    [Theory]
    [InlineData("category")] [InlineData("missing")] [InlineData(null)]
    public void ResultOrderingRejectsUnselectedOrInexactAliases(string alias)
    {
        var request = Request(); request.ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = alias } };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void ResultOrderingRejectsNullAndExcessiveIntent()
    {
        var request = Request(); request.ResultOrder = null;
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
        request.ResultOrder = new CqlGroupedV1Ordering[] { null };
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
        request.ResultOrder = new CqlGroupedV1Ordering[33];
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Theory]
    [InlineData("Boolean")] [InlineData("Int32")] [InlineData("Int64")]
    [InlineData("Byte")] [InlineData("SByte")] [InlineData("Int16")]
    [InlineData("UInt16")] [InlineData("UInt32")] [InlineData("Guid")]
    [InlineData("DateTime")] [InlineData("HolonType")] [InlineData("ProviderType")]
    public void ResultOrderingBindsEveryPublicMetadataDomainIncludingNullable(string type)
    {
        foreach (var nullable in new[] { false, true })
        {
            var request = Request(); request.Keys = new[] { new CqlProjectionV1Column {
                Alias = "key", MetadataPath = new[] { "key" }, ValueType = type, Nullable = nullable } };
            request.Columns = new[] { new CqlGroupedV1Column { Alias = "Key", KeyAlias = "key" } };
            request.ResultOrder = new[] { new CqlGroupedV1Ordering { Alias = "Key" } };
            var result = CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null);
            Assert.Single(result.Orderings);
        }
    }
}

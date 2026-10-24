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
}

using System;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlGroupedDistinctV1CompilerTests
{
    private static CqlGroupedV1Request Request(CqlGroupedV1Column column) => new()
    {
        Keys = new[] { new CqlProjectionV1Column { Alias = "category", Field = "Name" } },
        Columns = new[] { column }
    };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedCountRetainsExplicitDistinctIntent(bool distinct)
    {
        var request = Request(new CqlGroupedV1Column { Alias = "Values", Operation = "LongCount",
            Distinct = distinct, Selection = new CqlScalarV1Selection { Field = "Description" } });
        var result = CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null);
        Assert.Equal(distinct, Assert.Single(result.Columns).Distinct);
        Assert.Equal(typeof(long), result.Columns[0].ValueType);
    }

    [Theory]
    [InlineData("Count")]
    [InlineData("LongCount")]
    [InlineData("Min")]
    [InlineData("Sum")]
    public void DistinctWithoutSelectedCountIsRejected(string operation)
    {
        var request = Request(new CqlGroupedV1Column { Alias = "Values", Operation = operation, Distinct = true });
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Fact]
    public void DistinctGroupKeyIsRejected()
    {
        var request = Request(new CqlGroupedV1Column { Alias = "Category", KeyAlias = "category", Distinct = true });
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }

    [Theory]
    [InlineData("Min")]
    [InlineData("Max")]
    public void DistinctOtherSelectedAggregateIsRejected(string operation)
    {
        var request = Request(new CqlGroupedV1Column { Alias = "Values", Operation = operation,
            Distinct = true, Selection = new CqlScalarV1Selection { Field = "Description" } });
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileGrouped(request, Guid.NewGuid(), null));
    }
}

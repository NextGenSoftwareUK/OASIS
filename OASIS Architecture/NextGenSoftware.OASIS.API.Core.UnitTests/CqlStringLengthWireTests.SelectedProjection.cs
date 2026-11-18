using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlStringLengthWireTests_SelectedProjection
{
    private static readonly Guid Actor = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private static CqlProjectionV1Request Request(bool metadata = false, bool nullable = false) => new()
    {
        Columns = new[] { new CqlProjectionV1Column
        {
            Alias = "Units", Field = metadata ? null : "Name",
            MetadataPath = metadata ? new[] { "text" } : null,
            ValueType = metadata ? "String" : null,
            Function = CqlV1Function.Utf16Length, Nullable = nullable
        } }
    };

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void SelectedLengthKeepsStringInputAndExactComputedOutput(bool metadata, bool nullable)
    {
        var query = CqlV1Compiler.CompileProjection(Request(metadata, nullable), Actor, null);
        var column = Assert.Single(query.Columns);
        Assert.Equal(typeof(string), column.Path.ValueType);
        Assert.Equal(nullable ? typeof(int?) : typeof(int), column.ValueType);
        Assert.Equal(CosmicProjectionFunction.Utf16Length, column.Function);
        Assert.Equal(metadata, column.Path.IsMetadata);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void SelectedLengthRejectsUnknownFunctionsAndNonStringInputs(int invalid)
    {
        var request = Request();
        if (invalid == 0) request.Columns[0].Function = (CqlV1Function)777;
        if (invalid == 1) request.Columns[0].Field = "Version";
        if (invalid == 2)
        {
            request.Columns[0].Field = null;
            request.Columns[0].MetadataPath = new[] { "number" };
            request.Columns[0].ValueType = "Int32";
        }
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.CompileProjection(request, Actor, null));
    }

    [Fact]
    public void ComputedAliasSortCapturesCompleteSourceBeforeSelectedPaging()
    {
        var request = Request();
        request.OrderBy = "None";
        request.Skip = 2;
        request.Take = 1;
        request.SelectedOrder = new[] { new CqlGroupedV1Ordering { Alias = "Units", Descending = true } };
        var query = CqlV1Compiler.CompileProjection(request, Actor, null);
        Assert.Null(query.Source.Limit);
        Assert.Equal(3, query.ResultStages.Count);
        var ordering = Assert.IsType<CosmicSelectedOrderStage>(query.ResultStages[0]);
        Assert.True(Assert.Single(ordering.Orderings).Descending);
        Assert.Equal(CosmicSelectedWindowKind.Skip,
            Assert.IsType<CosmicSelectedWindowStage>(query.ResultStages[1]).Kind);
        Assert.Equal(CosmicSelectedWindowKind.Take,
            Assert.IsType<CosmicSelectedWindowStage>(query.ResultStages[2]).Kind);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void SelectedCompositionFiltersAndSortsComputedIntegerAlias(bool nullable)
    {
        var query = CqlV1Compiler.CompileComposition(new CqlCompositionV1Request
        {
            Source = Request(nullable: nullable),
            Stages = new[]
            {
                new CqlCompositionV1Stage { Operation = "Where", Filters = new[]
                {
                    new CqlGroupedV1Filter { Alias = "Units", Operator = CqlV1Operator.GreaterThan,
                        Value = JsonSerializer.SerializeToElement(2) }
                } },
                new CqlCompositionV1Stage { Operation = "OrderBy", Order = new[]
                {
                    new CqlGroupedV1Ordering { Alias = "Units" }
                } }
            }
        }, Actor, null);
        Assert.Equal(2, query.ResultStages.Count);
        Assert.Equal(nullable ? typeof(int?) : typeof(int), query.Columns[0].ValueType);
    }
}

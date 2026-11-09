using System.Linq.Expressions;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed partial class CosmicEntityProjectionTests
{
    [Fact]
    public void ExplicitReselectionRetainsBothSequencesAndAllNativeColumns()
    {
        var initial=Plan(new Native()).Query.Take(1).Result;
        var selected=initial.Select(row=>row.Name);
        Assert.False(selected.IsError,selected.Message);
        Assert.Same(initial.Source,selected.Result.Source);
        Assert.Same(initial.Selection.Source,selected.Result.Selection.Source);
        Assert.Equal(2,selected.Result.Selection.Columns.Count);
        Assert.Equal(new[]{0},selected.Result.Selection.OutputColumnIndices);
        Assert.Same(initial.Selection.ResultStages[0],selected.Result.Selection.ResultStages[0]);
        Assert.Equal(2,initial.Selection.OutputColumnIndices.Count);
        Assert.Equal("CQL_PROJECTION_UNSUPPORTED",initial.Select(row=>row.Revision+1).ErrorCode);
    }
    [Fact]
    public async Task RepeatedEntitySelectionConstructsOnlyTheFinalShape()
    {
        var native=new Native();ProjectionDto.Constructors=0;
        var selected=LinqRoot(native).Take(3).Where(row=>row.Name=="coffee")
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version})
            .Select(row=>row.Name).Select(name=>new ProjectionDto{Name=name});
        Assert.Equal(0,native.Calls);Assert.Equal(0,ProjectionDto.Constructors);
        var read=await selected.ToCosmicListAsync();Assert.False(read.IsError,read.Message+read.Exception);
        Assert.Equal("coffee",Assert.Single(read.Result).Name);Assert.Equal(0,read.Result.Single().Revision);
        Assert.Equal(1,ProjectionDto.Constructors);Assert.Equal(1,native.Calls);
        Assert.Equal(2,native.Seen!.Selection.Query.Columns.Count);
    }
    [Fact]
    public async Task ReselectionAfterSelectedStagesUsesCombinedNativeProof()
    {
        var native=new EntitySelectedNative{Packet=CombinedPacket()};ProjectionDto.Constructors=0;
        var selected=LinqRoot(native).Take(3).Reverse()
            .Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version}).Take(1).Select(row=>row.Name);
        var read=await selected.ToCosmicListAsync();Assert.False(read.IsError,read.Message+read.Exception);
        Assert.Equal("high",Assert.Single(read.Result));Assert.Equal(1,native.CombinedCalls);
        Assert.Equal(0,native.Calls);Assert.Equal(0,ProjectionDto.Constructors);
        native.Packet=CombinedPacket(true);
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",(await selected.ToCosmicListAsync()).ErrorCode);
        Assert.Equal(0,ProjectionDto.Constructors);
    }
    [Fact]
    public void NonGenericReselectionPreservesNativeExecutionAndCqlErrors()
    {
        var native=new Native();
        var selected=LinqRoot(native).Take(3).Reverse().Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        var expression=selected.Select(row=>row.Name).Expression;
        var repeated=(IQueryable<string>)selected.Provider.CreateQuery(expression);
        Assert.Equal("coffee",Assert.Single(repeated.ToArray()));
        Expression<Func<ProjectionDto,string>> invalid=row=>row.Name.ToUpper();
        var call=Expression.Call(typeof(Queryable),nameof(Queryable.Select),new[]{typeof(ProjectionDto),typeof(string)},
            selected.Expression,Expression.Quote(invalid));
        Assert.Equal("CQL_PROJECTION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>selected.Provider.CreateQuery(call)).Code);
    }
    [Fact]
    public void RepeatedEntitySelectionIsBoundedWithoutIo()
    {
        var native=new Native();
        var selected=LinqRoot(native).Take(3).Reverse().Select(row=>new ProjectionDto{Name=row.Name,Revision=row.Version});
        for(var index=0;index<32;index++)selected=selected.Select(row=>row);
        Assert.Equal("CQL_EXPRESSION_UNSUPPORTED",Assert.Throws<CosmicLinqException>(()=>selected.Select(row=>row)).Code);
        Assert.Equal(0,native.Calls);
    }
}

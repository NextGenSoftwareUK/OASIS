using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlStagedSetV1CompilerTests
{
    private static CqlSetV1Request Request()=>new(){
        Provider="MongoDBOASIS",Kind="Concat",
        Left=new(){Take=3,Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="name"},new CqlProjectionV1Column{Field="Version",Alias="version"}},
            Stages=new[]{
                new CqlCompositionV1Stage{Operation="Where",Filters=new[]{new CqlGroupedV1Filter{Alias="version",Operator=CqlV1Operator.GreaterThan,Value=JsonSerializer.SerializeToElement(1)}}},
                new CqlCompositionV1Stage{Operation="OrderBy",Order=new[]{new CqlGroupedV1Ordering{Alias="name",Descending=true}}},
                new CqlCompositionV1Stage{Operation="Skip",Count=1},
                new CqlCompositionV1Stage{Operation="Take",Count=1},
                new CqlCompositionV1Stage{Operation="Select",Columns=new[]{new CqlCompositionV1Column{Alias="label",SourceAlias="name"}}}}},
        Right=new(){Skip=1,Take=2,Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="other"}}},Take=2};
    [Fact]
    public void EachSelectedInputKeepsItsStagesBeforeVisibleTupleEquality()
    {
        var actor=Guid.NewGuid();var query=CqlV1Compiler.CompileProjectedSet(Request(),actor,ProviderType.MongoDBOASIS);
        Assert.Equal(3,query.Left.Source.Limit);Assert.Equal(2,query.Right.Source.Limit);
        Assert.Equal(actor,query.Left.Source.ReadAccess.ActorId);Assert.Equal(actor,query.Right.Source.ReadAccess.ActorId);
        Assert.Collection(query.Left.ResultStages,s=>Assert.IsType<CosmicSelectedFilterStage>(s),
            s=>Assert.IsType<CosmicSelectedOrderStage>(s),s=>Assert.IsType<CosmicSelectedWindowStage>(s),
            s=>Assert.IsType<CosmicSelectedWindowStage>(s));
        Assert.Equal("label",Assert.Single(query.Left.OutputAliases));Assert.Empty(query.Right.ResultStages);
        Assert.Equal(typeof(string),Assert.Single(query.ValueTypes));Assert.Single(query.Output.ResultStages);
    }
    [Fact]
    public void CaptureDetachesEveryMutableSelectedStageDescriptor()
    {
        var request=Request();var captured=CqlRequestCapture.Capture(request);
        request.Left.Stages![0].Filters![0].Value=JsonSerializer.SerializeToElement(99);
        request.Left.Stages[1].Order![0].Alias="hidden";
        request.Left.Stages[4].Columns![0].SourceAlias="hidden";
        var query=CqlV1Compiler.CompileProjectedSet(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        var filter=Assert.IsType<CosmicQueryComparison>(Assert.IsType<CosmicSelectedFilterStage>(query.Left.ResultStages[0]).Predicate);
        Assert.Equal(1L,filter.Literal.Value);
        Assert.Equal(0,Assert.Single(Assert.IsType<CosmicSelectedOrderStage>(query.Left.ResultStages[1]).Orderings).ColumnIndex);
        Assert.Equal("label",Assert.Single(query.Left.OutputAliases));
    }
    [Fact]
    public void HiddenAliasReentryOversizedStagesAndEntityStageLossAreRefused()
    {
        var request=Request();request.Left.Stages=new[]{request.Left.Stages![4],
            new CqlCompositionV1Stage{Operation="Where",Filters=new[]{new CqlGroupedV1Filter{Alias="version",Value=JsonSerializer.SerializeToElement(1)}}}};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        request=Request();request.Left.Stages=Enumerable.Repeat(new CqlCompositionV1Stage{Operation="Take",Count=1},17).ToArray();
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        request=Request();request.Left.Columns=null;request.Right.Columns=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
}

using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlEntitySetV1CompilerTests
{
    private static CqlSetV1Request Request()=>new(){
        Provider="MongoDBOASIS",Kind="Concat",Left=new(){Take=2},Right=new(){Take=2},
        Skip=1,Take=2,OutputColumns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}},
        EntityStages=new[]{
            new CqlSetV1EntityStage{Operation="Where",Where=new(){Kind="Filter",Filter=new(){
                Field="Version",Scalar=CqlV1Scalar.Int32,Operator=CqlV1Operator.GreaterThanOrEqual,
                Value=JsonSerializer.SerializeToElement(1)}}},
            new CqlSetV1EntityStage{Operation="OrderBy",Ordering=new[]{new CqlV1Ordering{Field="Name",Descending=true}}},
            new CqlSetV1EntityStage{Operation="Skip",Count=1},
            new CqlSetV1EntityStage{Operation="Take",Count=1}}};
    [Fact]
    public void EntityStagesRetainTheEarlierWindowAndLateColumnBoundary()
    {
        var actor=Guid.NewGuid();var q=CqlV1Compiler.CompileSetProjection(Request(),actor,ProviderType.MongoDBOASIS);
        Assert.Equal(CosmicSetQueryKind.Concat,q.Source.Kind);
        Assert.Equal(actor,q.Source.Left.ReadAccess.ActorId);
        Assert.Equal(new[]{1,2,1,1},q.Source.Output.Stages.OfType<CosmicEntityWindowStage>().Select(s=>s.Count));
        Assert.IsType<CosmicEntityFilterStage<NextGenSoftware.OASIS.API.Core.Holons.Holon>>(q.Source.Output.Stages[2]);
        Assert.Equal("label",Assert.Single(q.Selection.Columns).Alias);
        Assert.Same(q.Source.Output.Source,q.Selection.Source);
    }
    [Fact]
    public void StageCaptureDetachesPredicateCellsAndOrdering()
    {
        var input=Request();var captured=CqlRequestCapture.Capture(input);
        input.EntityStages![0].Where!.Filter!.Field="Name";
        input.EntityStages[1].Ordering![0].Field="Description";input.EntityStages[2].Count=99;
        Assert.Equal("Version",captured.EntityStages![0].Where!.Filter!.Field);
        Assert.Equal("Name",captured.EntityStages[1].Ordering![0].Field);
        Assert.Equal(1,captured.EntityStages[2].Count);
        Assert.Equal(1,captured.EntityStages[0].Where!.Filter!.Value.GetInt32());
    }
    [Fact]
    public void EntityStagesRefuseAmbiguousFieldsAndSelectedTupleModes()
    {
        var input=Request();var actor=Guid.NewGuid();
        input.EntityStages![0].Count=1;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(input,actor,ProviderType.MongoDBOASIS));
        input=Request();input.EntityStages![1].Ordering![0].Field="metadata.hidden";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(input,actor,ProviderType.MongoDBOASIS));
        input=Request();input.OutputColumns=null;
        input.Left.Columns=input.Right.Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(input,actor,ProviderType.MongoDBOASIS));
    }
}

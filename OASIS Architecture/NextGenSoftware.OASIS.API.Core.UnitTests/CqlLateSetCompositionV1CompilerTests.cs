using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlLateSetCompositionV1CompilerTests
{
    private static CqlSetV1Request Request()=>new(){
        Provider="MongoDBOASIS",Kind="Concat",Left=new(){Take=1},Right=new(){Take=1},Take=2,
        OutputColumns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}},
        SelectedStages=new[]{
            new CqlCompositionV1Stage{Operation="Where",Filters=new[]{new CqlGroupedV1Filter{
                Alias="label",Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement("native-set-http")}}},
            new CqlCompositionV1Stage{Operation="OrderBy",Order=new[]{new CqlGroupedV1Ordering{Alias="label",Descending=true}}},
            new CqlCompositionV1Stage{Operation="Take",Count=1},
            new CqlCompositionV1Stage{Operation="Reverse"},
            new CqlCompositionV1Stage{Operation="Select",Columns=new[]{new CqlCompositionV1Column{Alias="kept",SourceAlias="label"}}}}};
    [Fact]
    public void SelectedStagesRetainEntityMembershipEarlierWindowAndVisibleOutput()
    {
        var actor=Guid.NewGuid();var query=CqlV1Compiler.CompileSetProjection(Request(),actor,ProviderType.MongoDBOASIS);
        Assert.Equal(CosmicSetQueryKind.Concat,query.Source.Kind);
        Assert.Equal(2,Assert.Single(query.Source.Output.Stages.OfType<CosmicEntityWindowStage>()).Count);
        Assert.Equal(actor,query.Source.Left.ReadAccess.ActorId);Assert.Equal(actor,query.Source.Right.ReadAccess.ActorId);
        Assert.Same(query.Source.Output.Source,query.Selection.Source);
        Assert.Equal(4,query.Selection.ResultStages.Count);
        Assert.IsType<CosmicSelectedFilterStage>(query.Selection.ResultStages[0]);
        Assert.IsType<CosmicSelectedOrderStage>(query.Selection.ResultStages[1]);
        Assert.IsType<CosmicSelectedWindowStage>(query.Selection.ResultStages[2]);
        Assert.IsType<CosmicSelectedReverseStage>(query.Selection.ResultStages[3]);
        Assert.Equal("label",Assert.Single(query.Selection.Columns).Alias);
        Assert.Equal("kept",Assert.Single(query.Selection.OutputAliases));
    }
    [Fact]
    public void CaptureDetachesSelectedCellsOrderingAndReselectionAliases()
    {
        var request=Request();CqlSetV1Request captured;
        using(var cell=JsonDocument.Parse("\"native-set-http\""))
        {
            request.SelectedStages![0].Filters![0].Value=cell.RootElement;
            captured=CqlRequestCapture.Capture(request);
        }
        request.SelectedStages![0].Filters![0].Alias="hidden";
        request.SelectedStages[1].Order![0].Alias="hidden";
        request.SelectedStages[2].Count=99;
        request.SelectedStages[4].Columns![0].SourceAlias="hidden";
        var query=CqlV1Compiler.CompileSetProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(4,query.Selection.ResultStages.Count);
        Assert.Equal("native-set-http",captured.SelectedStages![0].Filters![0].Value.GetString());
        Assert.Equal(1,captured.SelectedStages[2].Count);Assert.Equal("label",captured.SelectedStages[4].Columns![0].SourceAlias);
    }
    [Fact]
    public void SelectedStagesRefuseAmbiguousModesHiddenAliasesAndEmptyPrograms()
    {
        var actor=Guid.NewGuid();var request=Request();request.OutputColumns=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,actor,ProviderType.MongoDBOASIS));
        request.Left.Columns=request.Right.Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.SelectedStages=Array.Empty<CqlCompositionV1Stage>();
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.SelectedStages![4].Columns![0].SourceAlias="hidden";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.SelectedStages![3].Count=1;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
    }
}

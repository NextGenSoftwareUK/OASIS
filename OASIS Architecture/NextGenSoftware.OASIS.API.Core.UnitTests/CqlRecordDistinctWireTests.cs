using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlRecordDistinctWireTests
{
    [Fact]
    public void DistinctCapturesCurrentTupleAndKeepsEarlierStageBindingsAfterReselection()
    {
        var request=new CqlCompositionV1Request {
            Source=new(){Take=10,Columns=new[]{
                new CqlProjectionV1Column{Alias="name",Field="Name"},
                new CqlProjectionV1Column{Alias="description",Field="Description"},
                new CqlProjectionV1Column{Alias="hidden",Field="Id"}}},
            Stages=new[]{
                new CqlCompositionV1Stage{Operation="Select",Columns=new[]{
                    new CqlCompositionV1Column{Alias="Label",SourceAlias="name"},
                    new CqlCompositionV1Column{Alias="Detail",SourceAlias="description"}}},
                new CqlCompositionV1Stage{Operation="Distinct"},
                new CqlCompositionV1Stage{Operation="Select",Columns=new[]{
                    new CqlCompositionV1Column{Alias="Display",SourceAlias="Label"}}},
                new CqlCompositionV1Stage{Operation="Distinct"}}};
        var actor=Guid.NewGuid();
        var query=CqlV1Compiler.CompileComposition(request,actor,ProviderType.SQLLiteDBOASIS);
        Assert.Equal(actor,query.Source.ReadAccess.ActorId);
        Assert.Equal(new[]{"Display"},query.OutputAliases);
        Assert.Equal(3,query.Columns.Count);
        var stages=query.ResultStages.OfType<CosmicSelectedDistinctStage>().ToArray();
        Assert.Equal(2,stages.Length);
        Assert.Equal(new[]{0,1},stages[0].ColumnIndices);
        Assert.Equal(new[]{0},stages[1].ColumnIndices);
        request.Stages[1].Count=1;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileComposition(request,actor,ProviderType.SQLLiteDBOASIS));
    }

    [Fact]
    public void ScalarDistinctRetainsItsSingleColumnContract()
    {
        var scalar=CosmicQuery<Holon>.Create().Select(h=>h.Name).Result.Distinct();
        Assert.False(scalar.IsError,scalar.Message);
        var stage=Assert.IsType<CosmicSelectedDistinctStage>(Assert.Single(scalar.Result.ResultStages));
        Assert.Equal(0,stage.ColumnIndex);
        Assert.Equal(new[]{0},stage.ColumnIndices);
    }
}

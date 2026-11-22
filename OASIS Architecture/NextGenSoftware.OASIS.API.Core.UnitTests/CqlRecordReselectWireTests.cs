using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlRecordReselectWireTests
{
    [Fact]
    public void RuntimeReselectionRetainsHiddenNativeValidationAndCannotReintroduceDroppedAliases()
    {
        var source=CosmicQuery<Holon>.Create().SelectColumns(new[]{
            CosmicProjectionSelection<Holon>.Create("name",h=>h.Name).Result,
            CosmicProjectionSelection<Holon>.Create("version",h=>h.Version).Result}).Result;
        var bindings=new[]{new CosmicProjectionOutputBinding("Label","name")};
        var selected=source.SelectOutputColumns(bindings).Result;
        bindings[0]=new("Other","version");
        Assert.Equal(new[]{"Label"},selected.OutputAliases);
        Assert.Equal(2,selected.Columns.Count);
        var again=selected.SelectOutputColumns(new[]{
            new CosmicProjectionOutputBinding("Text","Label"),
            new CosmicProjectionOutputBinding("Copy","Label")}).Result;
        Assert.Equal(new[]{0,0},again.OutputColumnIndices);
        var rows=CosmicProjectionResults.Materialize(again,new[]{new CosmicProjectionRow("coffee",7)});
        Assert.False(rows.IsError,rows.Message);
        Assert.Equal(new[]{"Text","Copy"},rows.Result.Single().Aliases);
        Assert.Equal(new object[]{"coffee","coffee"},rows.Result.Single().Values);
        Assert.True(selected.SelectOutputColumns(new[]{new CosmicProjectionOutputBinding("Leaked","version")}).IsError);
        var corrupt=CosmicProjectionResults.Materialize(again,new[]{new CosmicProjectionRow("coffee","invalid hidden Int32")});
        Assert.Equal("CQL_PROVIDER_RESULT_INVALID",corrupt.ErrorCode);
        Assert.Null(corrupt.Result);
    }

    [Fact]
    public void TypedCompositionResolvesCurrentAliasesAndKeepsEarlierSourceAndResultWindows()
    {
        var actor=Guid.NewGuid();
        var request=new CqlCompositionV1Request{
            Source=new(){Take=3,Columns=new[]{
                new CqlProjectionV1Column{Alias="name",Field="Name"},
                new CqlProjectionV1Column{Alias="version",Field="Version"}}},
            Stages=new[]{
                new CqlCompositionV1Stage{Operation="Take",Count=2},
                new CqlCompositionV1Stage{Operation="Select",Columns=new[]{
                    new CqlCompositionV1Column{Alias="label",SourceAlias="name"},
                    new CqlCompositionV1Column{Alias="revision",SourceAlias="version"}}},
                new CqlCompositionV1Stage{Operation="Where",Filters=new[]{
                    new CqlGroupedV1Filter{Alias="revision",Operator="GreaterThan",Value=JsonSerializer.SerializeToElement(1)}}},
                new CqlCompositionV1Stage{Operation="OrderBy",Order=new[]{new CqlGroupedV1Ordering{Alias="label",Descending=true}}},
                new CqlCompositionV1Stage{Operation="Select",Columns=new[]{new CqlCompositionV1Column{Alias="Display",SourceAlias="label"}}}}};
        var query=CqlV1Compiler.CompileComposition(request,actor,ProviderType.SQLLiteDBOASIS);
        Assert.Equal(actor,query.Source.ReadAccess.ActorId);
        Assert.Equal(3,query.Source.Limit);
        Assert.Equal(new[]{"Display"},query.OutputAliases);
        Assert.Equal(new[]{0},query.OutputColumnIndices);
        Assert.Equal(2,query.Columns.Count);
        Assert.Collection(query.ResultStages,
            stage=>Assert.IsType<CosmicSelectedWindowStage>(stage),
            stage=>Assert.IsType<CosmicSelectedFilterStage>(stage),
            stage=>Assert.IsType<CosmicSelectedOrderStage>(stage));
        request.Stages[2].Filters[0].Alias="version";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileComposition(request,actor,ProviderType.SQLLiteDBOASIS));
    }
}

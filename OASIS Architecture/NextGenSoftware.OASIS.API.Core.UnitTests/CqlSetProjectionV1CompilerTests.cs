using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlSetProjectionV1CompilerTests
{
    private static CqlSetV1Request Request()=>new(){
        Provider="MongoDBOASIS",Kind="Union",Left=new(){Take=2},Right=new(){Skip=1,Take=3},
        OutputColumns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}},
        Ordering=new[]{new CqlV1Ordering{Field="Name",Descending=true}},Skip=1,Take=1};

    [Fact]
    public void LateColumnsRetainIdentityInputsEntityWindowsAndServerActor()
    {
        var actor=Guid.NewGuid();var request=Request();
        var query=CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS);
        Assert.Equal(CosmicSetQueryKind.Union,query.Source.Kind);
        Assert.Equal(2,query.Source.Left.Limit);Assert.Equal(1,query.Source.Right.Offset);
        Assert.Equal(3,query.Source.Right.Limit);
        Assert.Equal(actor,query.Source.Left.ReadAccess.ActorId);Assert.Equal(actor,query.Source.Right.ReadAccess.ActorId);
        Assert.Same(query.Source.Output.Source,query.Selection.Source);
        Assert.Equal(new[]{1,1},query.Source.Output.Stages.OfType<CosmicEntityWindowStage>().Select(stage=>stage.Count).ToArray());
        var column=Assert.Single(query.Selection.Columns);Assert.Equal("label",column.Alias);
        Assert.Equal(typeof(string),column.ValueType);Assert.Equal("Name",Assert.Single(column.Path.Segments));
    }

    [Fact]
    public void CaptureDetachesLateColumnsAndPreservesUnboundedOperandIntent()
    {
        var request=Request();request.Left.Take=null;var path=new[]{"catalogue","title"};
        request.OutputColumns=new[]{new CqlProjectionV1Column{Alias="label",MetadataPath=path,ValueType="String"}};
        var captured=CqlRequestCapture.Capture(request);
        path[0]="changed";request.OutputColumns[0].Alias="changed";
        Assert.Null(captured.Left.Take);Assert.Equal("catalogue",captured.OutputColumns![0].MetadataPath![0]);
        Assert.Equal("label",captured.OutputColumns[0].Alias);
        var query=CqlV1Compiler.CompileSetProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.True(Assert.Single(query.Selection.Columns).Path.IsMetadata);
        Assert.Equal(new[]{"catalogue","title"},query.Selection.Columns[0].Path.Segments.ToArray());
    }

    [Fact]
    public void AmbiguousEmptyAndMetadataExpansionModesAreRefused()
    {
        var actor=Guid.NewGuid();var request=Request();
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,actor,ProviderType.MongoDBOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,actor,ProviderType.MongoDBOASIS));
        request.Left.Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="x"}};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.Right.Stages=Array.Empty<CqlCompositionV1Stage>();
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.OutputColumns=Array.Empty<CqlProjectionV1Column>();
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.IncludeMetadata=true;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
    }
}

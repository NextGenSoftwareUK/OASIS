using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlProjectedSetV1CompilerTests
{
    private static CqlSetV1Request Request()=>new(){
        Provider="MongoDBOASIS",Kind="Union",Left=new(){Take=2,Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}}},
        Right=new(){Skip=1,Take=3,Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="other"}}},
        Ordering=new[]{new CqlV1Ordering{Field="label",Descending=true}},Skip=1,Take=2};
    [Fact]
    public void SelectedAliasesKeepBothNativeInputWindowsAndGlobalValueStages()
    {
        var actor=Guid.NewGuid();var request=Request();
        var query=CqlV1Compiler.CompileProjectedSet(request,actor,ProviderType.MongoDBOASIS);
        Assert.Equal(2,query.Left.Source.Limit);Assert.Equal(1,query.Right.Source.Offset);
        Assert.Equal(3,query.Right.Source.Limit);Assert.Equal(actor,query.Left.Source.ReadAccess.ActorId);
        Assert.Equal(actor,query.Right.Source.ReadAccess.ActorId);
        Assert.Equal("label",Assert.Single(query.Output.Columns).Alias);
        Assert.Equal(typeof(string),Assert.Single(query.ValueTypes));Assert.Equal(3,query.Output.ResultStages.Count);
        Assert.True(Assert.Single(Assert.IsType<CosmicSelectedOrderStage>(query.Output.ResultStages[0]).Orderings).Descending);
    }
    [Fact]
    public void CaptureDetachesSelectedMetadataPathsAndDoesNotInventAnInputLimit()
    {
        var request=Request();var path=new[]{"catalogue","title"};
        request.Left.Take=null;request.Left.Columns=new[]{new CqlProjectionV1Column{Alias="label",MetadataPath=path,ValueType="String"}};
        var captured=CqlRequestCapture.Capture(request);
        path[0]="changed";request.Left.Columns[0].Alias="changed";request.Ordering![0].Field="changed";
        Assert.Null(captured.Left.Take);Assert.Equal("catalogue",captured.Left.Columns![0].MetadataPath![0]);
        Assert.Equal("label",captured.Left.Columns[0].Alias);Assert.Equal("label",captured.Ordering![0].Field);
    }
    [Fact]
    public void IncompleteOrIncompatibleSelectionsNeverBecomeEntitySets()
    {
        var request=Request();var actor=Guid.NewGuid();
        request.Right.Columns=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,actor,ProviderType.MongoDBOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.Right.Columns![0].Field="Version";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.Ordering![0].Field="Id";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,actor,ProviderType.MongoDBOASIS));
    }
}

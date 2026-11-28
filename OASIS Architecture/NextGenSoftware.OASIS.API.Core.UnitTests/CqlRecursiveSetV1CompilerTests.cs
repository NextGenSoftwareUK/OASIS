using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlRecursiveSetV1CompilerTests
{
    private static CqlSetV1Source Leaf()=>new(){Take=2,Columns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}}};
    private static CqlSetV1Request Tree()=>new(){Provider="MongoDBOASIS",Kind="Concat",Left=new(){Set=new(){Kind="Concat",Left=Leaf(),Right=Leaf(),Take=1}},Right=Leaf()};
    [Fact] public void CaptureDetachesNestedSourcesAndPreservesWindows()
    {
        var request=Tree();var copy=CqlRequestCapture.Capture(request);
        request.Left.Set!.Left.Columns![0].Alias="mutated";request.Left.Set.Take=0;
        Assert.Equal("label",copy.Left.Set!.Left.Columns![0].Alias);Assert.Equal(1,copy.Left.Set.Take);
        var actor=Guid.NewGuid();var query=CqlV1Compiler.CompileProjectedSet(copy,actor,ProviderType.MongoDBOASIS);
        Assert.NotNull(query.LeftSet);Assert.Equal(actor,query.Output.Source.ReadAccess.ActorId);
        Assert.Equal(2,query.LeftSet!.Left.Source.Limit);
    }
    [Fact] public void NestedEnvelopeCannotSmuggleLeafSettingsOrMixSequenceKinds()
    {
        var request=Tree();request.Left.Take=1;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        request=Tree();request.Left.Set!.Left.Columns=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
    [Fact] public void RecursiveDepthIsRejectedBeforeCompilation()
    {
        var request=Tree();for(var i=0;i<10;i++)request.Left=new(){Set=new(){Kind="Concat",Left=request.Left,Right=Leaf()}};
        Assert.Throws<ArgumentException>(()=>CqlRequestCapture.Capture(request));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjectedSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
}

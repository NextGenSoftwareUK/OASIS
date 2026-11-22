using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlSetCompilerTests
{
    [Theory]
    [InlineData("Union",CosmicSetQueryKind.Union)]
    [InlineData("Concat",CosmicSetQueryKind.Concat)]
    [InlineData("Intersect",CosmicSetQueryKind.Intersect)]
    [InlineData("Except",CosmicSetQueryKind.Except)]
    public void SetCompilerBindsOneActorAndIndependentWindows(string operation,CosmicSetQueryKind kind)
    {
        var actor=Guid.NewGuid();
        var request=new CqlSetV1Request {Kind=operation,
            Left=new(){Skip=1,Take=3},Right=new(){Skip=2,Take=4},Skip=1,Take=2};
        var query=CqlV1Compiler.CompileSet(request,actor,ProviderType.MongoDBOASIS);
        Assert.Equal(kind,query.Kind);
        Assert.Equal(actor,query.Left.ReadAccess.ActorId);Assert.Equal(actor,query.Right.ReadAccess.ActorId);
        Assert.Equal(1,query.Left.Offset);Assert.Equal(3,query.Left.Limit);
        Assert.Equal(2,query.Right.Offset);Assert.Equal(4,query.Right.Limit);
        Assert.Equal(2,query.Output.Stages.Count);
    }
    [Fact]
    public void SetCaptureDetachesBothSourcesBeforeCallbacks()
    {
        var request=new CqlSetV1Request {Left=new(){Skip=1,Take=3},Right=new(){Skip=2,Take=4}};
        var captured=CqlRequestCapture.Capture(request);
        request.Left.Skip=99;request.Right.Take=99;request.Kind="Except";
        Assert.Equal(1,captured.Left.Skip);Assert.Equal(4,captured.Right.Take);Assert.Equal("Union",captured.Kind);
        Assert.NotSame(request.Left,captured.Left);Assert.NotSame(request.Right,captured.Right);
    }
    [Fact]
    public void SetCompilerRejectsUnknownOperationsAndUnboundedInputs()
    {
        var request=new CqlSetV1Request {Kind="Unknown"};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        request.Kind="Union";request.MaximumRightRows=1001;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
}

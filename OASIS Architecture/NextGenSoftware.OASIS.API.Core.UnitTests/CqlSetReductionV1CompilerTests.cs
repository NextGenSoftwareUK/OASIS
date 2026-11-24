using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlSetReductionV1CompilerTests
{
    private static CqlSetV1Request Request()=>new(){
        Provider="MongoDBOASIS",Kind="Concat",Left=new(){Take=1},Right=new(){Take=1},Take=1,Reduction="LongCount",
        ReductionWhere=new(){Kind="Filter",Filter=new(){Field="Name",Scalar=CqlV1Scalar.String,
            Operator=CqlV1Operator.Equal,Value=JsonSerializer.SerializeToElement("native-set-http")}}};
    [Fact]
    public void ReductionsKeepIdentityPolicyAndPredicateAfterCombinedWindow()
    {
        var actor=Guid.NewGuid();
        foreach(var operation in new[]{"Count","LongCount","Any","All"})
        {
            var request=Request();request.Reduction=operation;
            var query=CqlV1Compiler.CompileSetReduction(request,actor,ProviderType.MongoDBOASIS);
            Assert.Equal(CosmicSetQueryKind.Concat,query.Kind);
            Assert.Equal(actor,query.Left.ReadAccess.ActorId);Assert.Equal(actor,query.Right.ReadAccess.ActorId);
            Assert.Equal(2,query.Output.Stages.Count);
            Assert.Equal(1,Assert.IsType<CosmicEntityWindowStage>(query.Output.Stages[0]).Count);
            Assert.IsType<CosmicEntityFilterStage<Holon>>(query.Output.Stages[1]);
        }
    }
    [Fact]
    public void CaptureDetachesReductionModePredicateAndJsonLifetime()
    {
        var request=Request();CqlSetV1Request captured;
        using(var cell=JsonDocument.Parse("\"native-set-http\""))
        {
            request.ReductionWhere!.Filter!.Value=cell.RootElement;
            captured=CqlRequestCapture.Capture(request);
        }
        request.Reduction="Any";request.ReductionWhere!.Filter!.Field="hidden";
        request.Take=99;
        var query=CqlV1Compiler.CompileSetReduction(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal("LongCount",captured.Reduction);Assert.Equal("Name",captured.ReductionWhere!.Filter!.Field);
        Assert.Equal("native-set-http",captured.ReductionWhere.Filter.Value.GetString());
        Assert.Equal(1,Assert.IsType<CosmicEntityWindowStage>(query.Output.Stages[0]).Count);
    }
    [Fact]
    public void ScalarAndRowModesCannotBeMixedOrSilentlyDowngraded()
    {
        var actor=Guid.NewGuid();var request=Request();
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSet(request,actor,ProviderType.MongoDBOASIS));
        request.Reduction="invalid";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetReduction(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.Reduction="All";request.ReductionWhere=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetReduction(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.OutputColumns=new[]{new CqlProjectionV1Column{Field="Name",Alias="label"}};
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetReduction(request,actor,ProviderType.MongoDBOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetProjection(request,actor,ProviderType.MongoDBOASIS));
        request=Request();request.Reduction=null;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileSetReduction(request,actor,ProviderType.MongoDBOASIS));
    }
}

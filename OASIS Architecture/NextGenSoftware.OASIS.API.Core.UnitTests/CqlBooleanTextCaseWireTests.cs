using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlBooleanTextCaseWireTests
{
    [Fact]
    public void CapturesLiteralBranchesAndPreservesSeparateFormulaIdentity()
    {
        var request=new CqlProjectionV1Request{OrderBy="Id",Columns=new[]{
            new CqlProjectionV1Column{Alias="Status",Field="IsActive",Function=CqlV1Function.BooleanTextCase,WhenTrue="active'); DELETE --",WhenFalse=""},
            new CqlProjectionV1Column{Alias="Visibility",Field="IsPublic",Function=CqlV1Function.BooleanTextCase,WhenTrue="public",WhenFalse="private"},
            new CqlProjectionV1Column{Alias="Flag",MetadataPath=new[]{"flag"},ValueType="Boolean",Nullable=false,Function=CqlV1Function.BooleanTextCase,WhenTrue="yes",WhenFalse="no"}}};
        var captured=CqlRequestCapture.Capture(request);
        request.Columns[0].WhenTrue="changed";request.Columns[0].WhenFalse="changed";
        var query=CqlV1Compiler.CompileProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(CosmicProjectionFunction.BooleanTextCase,query.Columns[0].Function);
        Assert.Equal("active'); DELETE --",query.Columns[0].WhenTrue);Assert.Equal("",query.Columns[0].WhenFalse);
        Assert.Equal("public",query.Columns[1].WhenTrue);Assert.Equal("private",query.Columns[1].WhenFalse);
        Assert.True(query.Columns[2].Path.IsMetadata);Assert.Equal(typeof(bool),query.Columns[2].Path.ValueType);
        Assert.All(query.Columns,column=>Assert.Equal(typeof(string),column.ValueType));
    }
    [Fact]
    public void RefusesIncompleteOversizedAndUnusedBranches()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",Field="IsPublic",Function=CqlV1Function.BooleanTextCase,WhenTrue="yes"},
            new CqlProjectionV1Column{Alias="Bad",Field="IsPublic",Function=CqlV1Function.BooleanTextCase,WhenTrue=new string('x',513),WhenFalse="no"},
            new CqlProjectionV1Column{Alias="Bad",Field="Name",WhenTrue="unused",WhenFalse="unused"}})
        {
            var request=new CqlProjectionV1Request{Columns=new[]{column}};
            Assert.Throws<ArgumentException>(()=>CqlRequestCapture.Capture(request));
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        }
    }
    [Fact]
    public void RefusesNonBooleanAndMappedConditions()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",Field="Name",Function=CqlV1Function.BooleanTextCase,WhenTrue="yes",WhenFalse="no"},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"flag"},ValueType="String",Function=CqlV1Function.BooleanTextCase,WhenTrue="yes",WhenFalse="no"}})
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(
                new CqlProjectionV1Request{Columns=new[]{column}},Guid.NewGuid(),ProviderType.MongoDBOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileVirtualProjection(
            new CqlProjectionV1Request{OrderBy="Id",Columns=new[]{
                new CqlProjectionV1Column{Alias="Status",Field="IsActive",Function=CqlV1Function.BooleanTextCase,WhenTrue="yes",WhenFalse="no"}}}));
    }
}

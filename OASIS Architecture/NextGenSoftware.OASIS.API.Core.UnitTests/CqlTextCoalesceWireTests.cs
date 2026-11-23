using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlTextCoalesceWireTests
{
    [Fact]
    public void CapturesCanonicalAndMetadataReplacementWithoutMutableDescriptorAliasing()
    {
        var path=new[]{"label"};
        var request=new CqlProjectionV1Request {OrderBy="Id",Columns=new[]{
            new CqlProjectionV1Column{Alias="Display",Field="Description",Function=CqlV1Function.TextCoalesce,NullReplacement="unknown'); DELETE --"},
            new CqlProjectionV1Column{Alias="Label",MetadataPath=path,ValueType="String",Nullable=true,Function=CqlV1Function.TextCoalesce,NullReplacement=""}}};
        var captured=CqlRequestCapture.Capture(request);
        path[0]="changed";request.Columns[0].NullReplacement="changed";
        var query=CqlV1Compiler.CompileProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(CosmicProjectionFunction.TextCoalesce,query.Columns[0].Function);
        Assert.Equal("unknown'); DELETE --",query.Columns[0].NullReplacement);
        Assert.Equal("",query.Columns[1].NullReplacement);
        Assert.Equal("label",Assert.Single(query.Columns[1].Path.Segments));
        Assert.All(query.Columns,column=>Assert.Equal(typeof(string),column.ValueType));
    }
    [Fact]
    public void RefusesMissingOversizedOrUnusedReplacementRatherThanIgnoringIt()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",Field="Name",Function=CqlV1Function.TextCoalesce},
            new CqlProjectionV1Column{Alias="Bad",Field="Name",Function=CqlV1Function.TextCoalesce,NullReplacement=new string('x',513)},
            new CqlProjectionV1Column{Alias="Bad",Field="Name",NullReplacement="unused"}})
        {
            var request=new CqlProjectionV1Request {Columns=new[]{column}};
            Assert.Throws<ArgumentException>(()=>CqlRequestCapture.Capture(request));
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        }
    }
    [Fact]
    public void RefusesNonStringInputsAndUnenrolledMappedCoalescing()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",Field="Version",Function=CqlV1Function.TextCoalesce,NullReplacement="unknown"},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"number"},ValueType="Int64",Function=CqlV1Function.TextCoalesce,NullReplacement="unknown"}})
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(
                new CqlProjectionV1Request{Columns=new[]{column}},Guid.NewGuid(),ProviderType.MongoDBOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileVirtualProjection(new CqlProjectionV1Request {
            OrderBy="Id",Columns=new[]{
                new CqlProjectionV1Column{Alias="Id",Field="Id",NullReplacement="must not be ignored"},
                new CqlProjectionV1Column{Alias="Label",MetadataPath=new[]{"label"},ValueType="String"}}}));
    }
}

using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlInt32CoalesceWireTests
{
    [Fact]
    public void CapturesExactIntegerLiteralAndMetadataWithoutMutableAliasing()
    {
        var path=new[]{"stock"};
        var request=new CqlProjectionV1Request{OrderBy="Id",Columns=new[]{
            new CqlProjectionV1Column{Alias="Stock",MetadataPath=path,ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=int.MinValue},
            new CqlProjectionV1Column{Alias="Count",MetadataPath=new[]{"count"},ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0}}};
        var captured=CqlRequestCapture.Capture(request);path[0]="changed";request.Columns[0].Int32Replacement=42;
        var query=CqlV1Compiler.CompileProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(CosmicProjectionFunction.Int32Coalesce,query.Columns[0].Function);
        Assert.Equal(int.MinValue,query.Columns[0].Int32Replacement);Assert.Equal(0,query.Columns[1].Int32Replacement);
        Assert.Equal("stock",Assert.Single(query.Columns[0].Path.Segments));
        Assert.All(query.Columns,column=>{Assert.Equal(typeof(int?),column.Path.ValueType);Assert.Equal(typeof(int),column.ValueType);});
    }
    [Fact]
    public void RefusesMissingUnusedOrConflictingReplacementArguments()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"stock"},ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce},
            new CqlProjectionV1Column{Alias="Bad",Field="Version",Int32Replacement=0},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"stock"},ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0,NullReplacement="mixed"},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"stock"},ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0,WhenTrue="unused"}})
        {
            var request=new CqlProjectionV1Request{Columns=new[]{column}};
            Assert.Throws<ArgumentException>(()=>CqlRequestCapture.Capture(request));
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(request,Guid.NewGuid(),ProviderType.MongoDBOASIS));
        }
    }
    [Fact]
    public void RefusesWrongSourceDomainsAndUnenrolledMappedIntegerComputation()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",Field="Version",Function=CqlV1Function.Int32Coalesce,Int32Replacement=0},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"stock"},ValueType="Int64",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"stock"},ValueType="Int32",Nullable=false,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0}})
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(
                new CqlProjectionV1Request{Columns=new[]{column}},Guid.NewGuid(),ProviderType.MongoDBOASIS));
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileVirtualProjection(
            new CqlProjectionV1Request{OrderBy="Id",Columns=new[]{
                new CqlProjectionV1Column{Alias="Id",Field="Id",Int32Replacement=0}}}));
    }
}

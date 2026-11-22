using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class CqlMappedComputedWireTests
{
    private static CqlProjectionV1Request Request(params CqlProjectionV1Column[] columns)=>new(){
        OrderBy="Id",Take=5,VirtualMappingId=Guid.NewGuid(),
        Columns=new[]{new CqlProjectionV1Column{Field="Id",Alias="Id"}}.Concat(columns).ToArray()};
    [Fact]
    public void CapturesMappedLiteralIdentityAndPreservesArgumentsWhenBindingNativeFields()
    {
        var path=new[]{"label"};
        var request=Request(
            new(){Alias="First",MetadataPath=path,ValueType="String",Nullable=true,Function=CqlV1Function.TextCoalesce,NullReplacement="$literal"},
            new(){Alias="Second",MetadataPath=new[]{"label"},ValueType="String",Nullable=true,Function=CqlV1Function.TextCoalesce,NullReplacement="other"},
            new(){Alias="Flag",MetadataPath=new[]{"flag"},ValueType="Boolean",Nullable=false,Function=CqlV1Function.BooleanTextCase,WhenTrue="yes",WhenFalse="no"},
            new(){Alias="Stock",MetadataPath=new[]{"stock"},ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0});
        var captured=CqlRequestCapture.Capture(request);path[0]="changed";request.Columns[1].NullReplacement="changed";
        var mixed=CqlV1Compiler.CompileVirtualProjection(captured);
        Assert.Equal(4,mixed.Columns.Length);Assert.Equal("$literal",mixed.Columns[0].NullReplacement);
        Assert.NotEqual(mixed.Columns[0].ValueKey,mixed.Columns[1].ValueKey);
        var bound=mixed.Columns[0].WithField("nativeLabel");
        Assert.Equal("nativeLabel",bound.Field);Assert.Equal("$literal",bound.NullReplacement);
        Assert.Equal(NativeRecordFunction.BooleanTextCase,mixed.Columns[2].Function);
        Assert.Equal("yes",mixed.Columns[2].WhenTrue);Assert.Equal("no",mixed.Columns[2].WhenFalse);
        Assert.Equal(typeof(string),mixed.Columns[2].ValueType);
        Assert.Equal(NativeRecordFunction.Int32Coalesce,mixed.Columns[3].Function);
        Assert.Equal(0,mixed.Columns[3].Int32Replacement);Assert.Equal(typeof(int),mixed.Columns[3].ValueType);
        Assert.All(mixed.Columns,column=>Assert.False(column.Nullable));
        var stored=CqlV1Compiler.CompileProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS,null,CqlVirtualProjection.MaximumRows,captured.VirtualMappingId);
        Assert.Equal(CosmicProjectionFunction.Int32Coalesce,stored.Columns[4].Function);
        Assert.Equal(0,stored.Columns[4].Int32Replacement);
    }
    [Fact]
    public void ProjectionOnlyFunctionsCannotBecomeNativeSourcePredicates()
    {
        foreach(var function in new[]{NativeRecordFunction.TextCoalesce,NativeRecordFunction.BooleanTextCase,NativeRecordFunction.Int32Coalesce})
            Assert.Throws<ArgumentException>(()=>new NativeRecordFilter("field",CosmicQueryComparisonOperator.Equal,1,function));
        Assert.Throws<ArgumentException>(()=>new NativeProjectionColumn("field","Out",typeof(string),function:NativeRecordFunction.None,nullReplacement:"unused"));
        Assert.Throws<ArgumentException>(()=>new NativeProjectionColumn("field","Out",typeof(string),true,NativeRecordFunction.TextCoalesce,"x"));
        Assert.Throws<ArgumentException>(()=>new NativeProjectionColumn("field","Out",typeof(bool),function:NativeRecordFunction.BooleanTextCase,whenTrue:new string('x',513),whenFalse:"no"));
    }
    [Fact]
    public void RefusesInvalidMappedDomainsIdentityArgumentsAndUnenrolledAggregateComputation()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"stock"},ValueType="String",Function=CqlV1Function.Int32Coalesce,Nullable=true,Int32Replacement=0},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"flag"},ValueType="Boolean",Function=CqlV1Function.BooleanTextCase,Nullable=true,WhenTrue="yes",WhenFalse="no"},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"label"},ValueType="String",NullReplacement="unused"}})
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileVirtualProjection(Request(column)));
        var identity=Request(new CqlProjectionV1Column{Alias="Text",MetadataPath=new[]{"label"},ValueType="String"});
        identity.Columns[0].WhenTrue="unused";
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileVirtualProjection(identity));
        var aggregate=Request(new CqlProjectionV1Column{Alias="Stock",MetadataPath=new[]{"stock"},ValueType="Int32",Nullable=true,Function=CqlV1Function.Int32Coalesce,Int32Replacement=0});
        aggregate.ResultOperation=CqlProjectionV1ResultOperation.LongCount;
        Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileVirtualProjection(aggregate));
    }
}

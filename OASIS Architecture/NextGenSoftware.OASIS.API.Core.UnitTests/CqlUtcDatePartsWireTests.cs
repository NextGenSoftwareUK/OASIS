using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlUtcDatePartsWireTests
{
    [Fact]
    public void CapturesImmutableUtcPathsAndExactResultDomainsForTheWholeFamily()
    {
        var functions=new[]{CqlV1Function.UtcYear,CqlV1Function.UtcMonth,CqlV1Function.UtcDay,
            CqlV1Function.UtcDayOfYear,CqlV1Function.UtcHour,CqlV1Function.UtcMinute,CqlV1Function.UtcSecond};
        var path=new[]{"observed"};
        var request=new CqlProjectionV1Request{OrderBy="Id",Columns=functions.Select((function,index)=>
            new CqlProjectionV1Column{Alias="Part"+index,MetadataPath=path,ValueType="DateTime",
                Nullable=index%2==0,Function=function}).ToArray()};
        var captured=CqlRequestCapture.Capture(request);path[0]="changed";
        request.Columns[0].Function=CqlV1Function.UtcSecond;
        var query=CqlV1Compiler.CompileProjection(captured,Guid.NewGuid(),ProviderType.MongoDBOASIS);
        Assert.Equal(7,query.Columns.Count);
        for(var index=0;index<query.Columns.Count;index++)
        {
            var column=query.Columns[index];
            Assert.Equal(functions[index].ToString(),column.Function.ToString());
            Assert.Equal("observed",Assert.Single(column.Path.Segments));
            Assert.Equal(typeof(DateTime),column.Path.ValueType);
            Assert.Equal(index%2==0?typeof(int?):typeof(int),column.ValueType);
            Assert.True(column.IsUtcDatePart);
        }
    }

    [Fact]
    public void RefusesWrongDomainsAndUnrelatedFunctionArguments()
    {
        foreach(var column in new[]{
            new CqlProjectionV1Column{Alias="Bad",Field="Version",Function=CqlV1Function.UtcYear},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"observed"},ValueType="Int64",Function=CqlV1Function.UtcYear},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"observed"},ValueType="DateTime",Function=CqlV1Function.UtcYear,NullReplacement="unused"},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"observed"},ValueType="DateTime",Function=CqlV1Function.UtcYear,Int32Replacement=0},
            new CqlProjectionV1Column{Alias="Bad",MetadataPath=new[]{"observed"},ValueType="DateTime",Function=CqlV1Function.UtcYear,WhenTrue="unused"}})
            Assert.Throws<ArgumentException>(()=>CqlV1Compiler.CompileProjection(
                new CqlProjectionV1Request{Columns=new[]{column}},Guid.NewGuid(),ProviderType.MongoDBOASIS));
    }
}

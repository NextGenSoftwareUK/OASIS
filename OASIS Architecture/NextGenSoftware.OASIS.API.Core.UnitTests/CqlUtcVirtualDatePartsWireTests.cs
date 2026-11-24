using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CqlUtcVirtualDatePartsWireTests
{
    [Fact]
    public void VirtualDateComponentsRetainInputDomainOutputNullabilityAndLiteralRefusal()
    {
        foreach(var function in new[]{CqlV1Function.UtcYear,CqlV1Function.UtcMonth,CqlV1Function.UtcDay,
            CqlV1Function.UtcDayOfYear,CqlV1Function.UtcHour,CqlV1Function.UtcMinute,CqlV1Function.UtcSecond})
        {
            var request=new CqlProjectionV1Request{OrderBy="Id",Take=10,Columns=new[]{
                new CqlProjectionV1Column{Field="Id",Alias="Id"},
                new CqlProjectionV1Column{MetadataPath=new[]{"observed"},Alias="Part",
                    ValueType="DateTime",Nullable=true,Function=function}}};
            var compiled=CqlV1Compiler.CompileVirtualProjection(CqlRequestCapture.Capture(request));
            var column=Assert.Single(compiled.Columns);
            Assert.Equal(typeof(DateTime),column.InputType);Assert.Equal(typeof(int),column.ValueType);
            Assert.True(column.Nullable);Assert.Equal(function.ToString(),column.Function.ToString());
            Assert.NotNull(column.UtcDatePart);
            Assert.Throws<ArgumentException>(()=>new NativeProjectionColumn("observed","Bad",typeof(string),
                true,column.Function));
            request.Columns[1].NullReplacement="unused";
            Assert.Throws<ArgumentException>(()=>CqlRequestCapture.Capture(request));
        }
    }
}

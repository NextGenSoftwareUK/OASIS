using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CqlV1Web4Tests
{
    [Fact]
    public async Task CompositionRecordReselectionKeepsExactOutputAndOneReceipt()
    {
        var source=new Source(ProviderType.SQLLiteDBOASIS);
        CompositionAdapter(source,CompositionPacket());
        var admitted=Admission(ProviderType.SQLLiteDBOASIS);
        var request=CompositionRequest();
        request.Stages=new[]{
            request.Stages[0],
            new CqlCompositionV1Stage{Operation="Select",Columns=new[]{
                new CqlCompositionV1Column{Alias="ExactStock",SourceAlias="stock"}}}};
        var result=await admitted.Policy.RunAsync(()=>CqlV1Endpoint.ExecuteCompositionAsync(admitted.Http,request,source));
        using var document=JsonDocument.Parse(Assert.IsType<FileContentResult>(result).FileContents);
        var body=document.RootElement.GetProperty("result");
        Assert.Equal("cql-composition-v1",body.GetProperty("version").GetString());
        Assert.Equal(1,body.GetProperty("columns").GetArrayLength());
        Assert.Equal("ExactStock",body.GetProperty("columns")[0].GetProperty("alias").GetString());
        Assert.Equal("Int64",body.GetProperty("columns")[0].GetProperty("valueType").GetString());
        Assert.Equal(1,body.GetProperty("rows")[0].GetArrayLength());
        Assert.Equal("9007199254740993",body.GetProperty("rows")[0][0].GetString());
        Assert.Single(admitted.Receipts);
    }
}

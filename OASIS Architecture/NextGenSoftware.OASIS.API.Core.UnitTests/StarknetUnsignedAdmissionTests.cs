using System.Net;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Starknet;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class StarknetUnsignedAdmissionTests
{
    private sealed class NoDispatch:HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            Calls++;
            throw new InvalidOperationException("Unsigned payload admission must not dispatch any RPC or invent a transaction hash.");
        }
    }

    [Theory]
    [InlineData("null")][InlineData("from")][InlineData("to")][InlineData("unsigned")]
    public async Task UnsignedSubmissionNeverDispatchesOrReportsSyntheticSuccess(string kind)
    {
        using var transport=new NoDispatch();using var http=new HttpClient(transport);
        var client=new StarknetRpcClient(http,"http://127.0.0.1:18664/");
        var payload=kind=="null"?null:new StarknetTransactionPayload{From=kind=="from"?"":"0x123",To=kind=="to"?"":"0x456",Amount=1};
        var result=await client.SubmitTransactionAsync(payload!);
        Assert.True(result.IsError);Assert.Null(result.Result);Assert.False(result.IsSaved);Assert.Equal(0,transport.Calls);
        if(kind=="unsigned"){
            Assert.IsType<NotSupportedException>(result.Exception);
            Assert.Contains("nothing was sent",result.Message,StringComparison.Ordinal);
        }else Assert.Contains(kind=="null"?"payload":kind=="from"?"From":"To",result.Message,StringComparison.Ordinal);
    }
}

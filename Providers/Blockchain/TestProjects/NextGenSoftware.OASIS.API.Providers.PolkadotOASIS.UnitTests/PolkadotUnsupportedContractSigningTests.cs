using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotUnsupportedContractSigningTests
{
    private sealed class HeaderResponse : HttpMessageHandler
    {
        public bool LoseResponse;public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Requests++;
            if(LoseResponse)throw new IOException("Controlled RPC loss");
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content=new StringContent("""{"result":{"hash":"0x01","number":"0x01"}}""")});
        }
    }

    [DataTestMethod]
    [DataRow("save_avatar",false)] [DataRow("save_avatar",true)]
    [DataRow("save_holon",false)] [DataRow("save_holon",true)]
    [DataRow("delete_holon",false)] [DataRow("delete_holon",true)]
    public async Task MissingContractProtocolNeverFabricatesSignedExtrinsic(string operation,bool transportLoss)
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1","fixture","owned-test-key");
        using var handler=new HeaderResponse {LoseResponse=transportLoss};
        using var client=new HttpClient(handler) {BaseAddress=new Uri("http://127.0.0.1:1")};
        var transport=typeof(PolkadotOASIS).GetField("_httpClient",BindingFlags.NonPublic|BindingFlags.Instance)!;
        ((HttpClient)transport.GetValue(provider)!).Dispose();
        transport.SetValue(provider,client);
        var method=typeof(PolkadotOASIS).GetMethod("CreatePolkadotTransaction",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var pending=(Task<string>)method.Invoke(provider,new object[] {operation,"owned-private-record-marker"})!;
        var failure=await Assert.ThrowsExceptionAsync<NotSupportedException>(()=>pending);
        Assert.AreEqual(0,handler.Requests,"No fabricated contract protocol may dispatch a native request.");
        Assert.IsFalse(failure.Message.Contains("owned-private-record-marker",StringComparison.Ordinal));
        Assert.IsFalse(failure.Message.Contains("owned-test-key",StringComparison.Ordinal));
    }
}

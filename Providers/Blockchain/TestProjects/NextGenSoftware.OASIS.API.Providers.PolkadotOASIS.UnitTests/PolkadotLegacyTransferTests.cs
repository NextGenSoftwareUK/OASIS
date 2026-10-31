using System.Net;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotLegacyTransferTests
{
    private sealed class Observer:HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token) {
            Calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.BadRequest));
        }
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task LegacyFacadeMustUseAdmittedOwnerBeforeAnyNetworkRequest(bool synchronous)
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1");
        typeof(PolkadotOASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
        var field=typeof(PolkadotOASIS).GetField("_httpClient",BindingFlags.Instance|BindingFlags.NonPublic)!;
        using var original=(HttpClient)field.GetValue(provider)!;
        using var observer=new Observer();field.SetValue(provider,new HttpClient(observer){BaseAddress=new Uri("http://127.0.0.1:1")});
        var result=synchronous?provider.SendTransaction("sender","recipient",1m,"memo"):await provider.SendTransactionAsync("sender","recipient",1m,"memo");
        Assert.IsTrue(result.IsError);StringAssert.Contains(result.Message,"admitted execution owner");
        Assert.AreEqual(0,observer.Calls,"Legacy facade must not submit unsigned pseudo-extrinsics or perform its old REST preflight.");
    }
}

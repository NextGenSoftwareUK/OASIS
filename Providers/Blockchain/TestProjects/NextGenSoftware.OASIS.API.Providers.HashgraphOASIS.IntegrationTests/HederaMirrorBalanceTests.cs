using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaMirrorBalanceTests
{
    private sealed class FixtureHandler(string json, System.Net.HttpStatusCode status) : HttpMessageHandler
    {
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested(); Calls++;
            Assert.AreEqual("http://127.0.0.1/api/v1/accounts/0.0.3",request.RequestUri!.AbsoluteUri);
            return Task.FromResult(new HttpResponseMessage(status) { Content=new StringContent(json) });
        }
    }

    [DataTestMethod]
    [DataRow("{\"account\":\"0.0.4\",\"deleted\":false,\"balance\":{\"balance\":10}}",200)]
    [DataRow("{\"account\":\"0.0.3\",\"deleted\":true,\"balance\":{\"balance\":10}}",200)]
    [DataRow("{\"account\":\"0.0.3\",\"deleted\":false,\"balance\":{\"balance\":-1}}",200)]
    [DataRow("{\"account\":\"0.0.3\",\"deleted\":false,\"balance\":{\"balance\":1.5}}",200)]
    [DataRow("{\"account\":\"0.0.3\",\"deleted\":false,\"balance\":{\"balance\":9223372036854775808}}",200)]
    [DataRow("{}",200)]
    [DataRow("bad-json",200)]
    [DataRow("{}",404)]
    [DataRow("{}",500)]
    public async Task InvalidMirrorResponseNeverReportsSuccessfulZero(string json,int status)
    {
        var provider=new HashgraphOASIS(network:"testnet");
        using var handler=new FixtureHandler(json,(System.Net.HttpStatusCode)status);
        using var client=new HttpClient(handler) { BaseAddress=new Uri("http://127.0.0.1/api/v1/") };
        var field=typeof(HashgraphOASIS).GetField("_httpClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        ((HttpClient)field.GetValue(provider)!).Dispose(); field.SetValue(provider,client);
        Assert.IsTrue((await provider.GetAccountBalanceAsync("0.0.3")).IsError);
        Assert.IsTrue((await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress="0.0.3" })).IsError);
        Assert.AreEqual(2,handler.Calls);
    }

    [DataTestMethod]
    [DataRow("0")]
    [DataRow("9007199254740993")]
    [DataRow("9223372036854775807")]
    public async Task ExactInt64TinybarsRemainExactInDecimalInterface(string amount)
    {
        var provider=new HashgraphOASIS(network:"testnet");
        using var handler=new FixtureHandler("{\"account\":\"0.0.3\",\"deleted\":false,\"balance\":{\"balance\":"+amount+"}}",System.Net.HttpStatusCode.OK);
        using var client=new HttpClient(handler) { BaseAddress=new Uri("http://127.0.0.1/api/v1/") };
        var field=typeof(HashgraphOASIS).GetField("_httpClient",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        ((HttpClient)field.GetValue(provider)!).Dispose(); field.SetValue(provider,client);
        var result=await provider.GetAccountBalanceAsync("0.0.3");
        Assert.IsFalse(result.IsError,result.Message);
        Assert.AreEqual(decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture)/100000000m,result.Result);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("bad")]
    [DataRow("0.0.0")]
    public async Task InvalidAccountCannotBecomeSuccessfulZeroBalance(string account)
    {
        var provider = new HashgraphOASIS(network:"testnet");
        var exact = await provider.GetAccountBalanceAsync(account);
        var shared = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress=account });
        Assert.IsTrue(exact.IsError); Assert.IsTrue(shared.IsError);
    }

    [TestMethod]
    public async Task CancellationAndNullRequestAreExplicitFailuresBeforeSdkLaunch()
    {
        var provider = new HashgraphOASIS(network:"testnet");
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.IsTrue((await provider.GetAccountBalanceAsync("0.0.3",cancellation.Token)).IsError);
        Assert.IsTrue((await provider.GetBalanceAsync(null!)).IsError);
    }

    [TestMethod]
    public async Task HederaTestnetPublicNodeBalanceUsesSupportedMirrorWithoutOperatorFunding()
    {
        var provider = new HashgraphOASIS(rpcEndpoint:"https://testnet.mirrornode.hedera.com/api/v1",network:"testnet");
        var balance = await provider.GetAccountBalanceAsync("0.0.3");
        Assert.IsFalse(balance.IsError,balance.Message);
        Assert.IsTrue(balance.Result>0,"Known testnet node account should have a real positive ledger balance.");
        Assert.AreEqual(decimal.Truncate(balance.Result*100000000m),balance.Result*100000000m);
        StringAssert.Contains(balance.Message,"mirror");
    }
}

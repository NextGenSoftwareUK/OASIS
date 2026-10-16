using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaBalanceTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task PublicBalanceInitializesServiceAndPreservesActivationCause(bool synchronous,bool activationFailure)
    {
        var rpc=DispatchProxy.Create<IRpcClient,BalanceRpc>();var proxy=(BalanceRpc)(object)rpc;proxy.Lamports=1;
        proxy.HealthFailure=activationFailure ? new IOException("owned health connection failure"):null;
        var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var request=new GetWeb3WalletBalanceRequest{WalletAddress=account.PublicKey.Key};
        var result=synchronous ? provider.GetBalance(request):await provider.GetBalanceAsync(request);
        Assert.AreEqual(1,proxy.HealthReads);
        if(activationFailure) {Assert.IsTrue(result.IsError);Assert.AreSame(proxy.HealthFailure,result.Exception);Assert.IsFalse(provider.IsProviderActivated);Assert.IsNull(proxy.Address);}
        else {Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(0.000000001d,result.Result);Assert.IsTrue(provider.IsProviderActivated);}
    }

    [DataTestMethod]
    [DataRow(0UL)] [DataRow(1UL)] [DataRow(ulong.MaxValue)]
    public async Task BalanceUsesFinalizedNativeLamportsWithoutPrecisionLoss(ulong lamports)
    {
        var rpc=DispatchProxy.Create<IRpcClient,BalanceRpc>();var proxy=(BalanceRpc)(object)rpc;proxy.Lamports=lamports;
        var account=new Account();var service=new SolanaService(account,rpc);var console=Console.Out;
        var result=await service.GetAccountBalanceAsync(new GetWeb3WalletBalanceRequest{WalletAddress=account.PublicKey.Key});
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(lamports/1_000_000_000m,result.Result);
        Assert.AreEqual(account.PublicKey.Key,proxy.Address);Assert.AreEqual(Commitment.Finalized,proxy.Commitment);Assert.AreSame(console,Console.Out);
    }

    [TestMethod]
    public async Task BalanceTransportFailurePreservesOriginalCause()
    {
        var rpc=DispatchProxy.Create<IRpcClient,BalanceRpc>();var proxy=(BalanceRpc)(object)rpc;proxy.Failure=new IOException("owned transport loss");
        var account=new Account();var result=await new SolanaService(account,rpc).GetAccountBalanceAsync(new GetWeb3WalletBalanceRequest{WalletAddress=account.PublicKey.Key});
        Assert.IsTrue(result.IsError);Assert.AreSame(proxy.Failure,result.Exception);
    }

    public class BalanceRpc : SolanaNativeTokenTests.TokenRpc
    {
        public ulong Lamports;public string? Address;public Commitment Commitment;public IOException? Failure,HealthFailure;public int HealthReads;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name=="GetHealthAsync") {HealthReads++;if(HealthFailure!=null) throw HealthFailure;return Respond(method,"\"ok\"");}
            if(method!.Name!="GetBalanceAsync") throw new InvalidOperationException("Unexpected balance RPC: "+method.Name);
            Address=(string)args![0]!;Commitment=(Commitment)args[1]!;
            if(Failure!=null) throw Failure;
            return Respond(method,Newtonsoft.Json.JsonConvert.SerializeObject(new{Value=Lamports}));
        }
    }
}

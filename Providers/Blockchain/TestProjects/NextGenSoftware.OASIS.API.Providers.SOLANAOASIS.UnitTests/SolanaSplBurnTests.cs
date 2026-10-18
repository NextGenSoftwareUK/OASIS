using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaSplBurnTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task CheckedBurnUsesRequestedAmountExplicitSignerAndOriginalSignature(bool synchronous,bool lostReply)
    {
        var signer=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();var proxy=(SolanaSplSendTests.SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Mint=new Account().PublicKey;proxy.Recipient=new Account().PublicKey;proxy.LostReply=lostReply;
        var request=new BurnWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,OwnerPublicKey=signer.PublicKey.Key,OwnerSeedPhrase="",TokenAddress=proxy.Mint.Key,Amount=1.25m};
        var provider=Provider(rpc);var result=synchronous ? provider.BurnToken(request):await provider.BurnTokenAsync(request);
        Assert.AreEqual(1,proxy.Sends);var transaction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!);
        Assert.AreEqual(1,transaction.Instructions.Count);
        var source=AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(signer.PublicKey,proxy.Mint);
        CollectionAssert.AreEqual(TokenProgram.BurnChecked(proxy.Mint,source,signer.PublicKey,1_250_000,6).Data,transaction.Instructions.Single().Data);
        Assert.IsTrue(signer.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
        var signature=Solnet.Wallet.Utilities.Encoders.Base58.EncodeData(proxy.Signed.AsSpan(1,64).ToArray());
        if(lostReply)
        {
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,proxy.StatusReads);
            Assert.AreEqual(signature,((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
        }
        else {Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(signature,result.Result.TransactionResult);Assert.AreEqual(1,proxy.StatusReads);}
    }

    [DataTestMethod]
    [DataRow("mint-program")] [DataRow("source-owner")] [DataRow("source-mint")]
    [DataRow("frozen")] [DataRow("insufficient")] [DataRow("precision")] [DataRow("zero")]
    public async Task InvalidBurnNeverBroadcasts(string mode)
    {
        var signer=new Account();var rpc=DispatchProxy.Create<IRpcClient,SolanaSplSendTests.SplRpc>();var proxy=(SolanaSplSendTests.SplRpc)(object)rpc;
        proxy.Owner=signer.PublicKey;proxy.Mint=new Account().PublicKey;proxy.Recipient=new Account().PublicKey;proxy.Mode=mode;
        var result=await Provider(rpc).BurnTokenAsync(new BurnWeb3TokenRequest{OwnerPrivateKey=signer.PrivateKey.Key,OwnerPublicKey=signer.PublicKey.Key,OwnerSeedPhrase="",
            TokenAddress=proxy.Mint.Key,Amount=mode=="precision" ? 0.0000001m:mode=="zero" ? 0:1.25m});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);
        Assert.IsInstanceOfType(result.Exception,mode=="mint-program" ? typeof(NotSupportedException):mode=="insufficient" || mode=="precision" || mode=="zero" ? typeof(ArgumentException):typeof(InvalidDataException));
    }

    private static SolanaOASIS Provider(IRpcClient rpc)
    {
        var unrelated=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",unrelated.PrivateKey.Key,unrelated.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,rpc);return provider;
    }
}

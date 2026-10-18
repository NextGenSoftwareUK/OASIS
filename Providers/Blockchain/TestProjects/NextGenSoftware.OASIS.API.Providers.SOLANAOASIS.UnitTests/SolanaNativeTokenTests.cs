using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaNativeTokenTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task GeneratedWalletCanSignNativeTransferWithoutKeyReencoding(bool synchronous)
    {
        var owner=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        var generated=synchronous ? provider.GenerateKeyPair():await provider.GenerateKeyPairAsync();
        Assert.IsFalse(generated.IsError,generated.Message);Assert.IsNotNull(generated.Result);
        var rpc=DispatchProxy.Create<IRpcClient,TokenRpc>();var proxy=(TokenRpc)(object)rpc;
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest {FromWalletAddress=generated.Result.PublicKey,
            ToWalletAddress=new Account().PublicKey.Key,OwnerPrivateKey=generated.Result.PrivateKey,Amount=0.000000001m});
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,proxy.Sends);
        var publicKey=new PublicKey(generated.Result.PublicKey);
        Assert.IsTrue(publicKey.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
    }

    [DataTestMethod]
    [DataRow("wrong-signer")] [DataRow("conflicting-key")]
    public async Task InvalidSignerCannotReachRpc(string mode)
    {
        var owner=new Account();var other=new Account();var rpc=DispatchProxy.Create<IRpcClient,TokenRpc>();var proxy=(TokenRpc)(object)rpc;
        var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest {Amount=1,FromWalletAddress=owner.PublicKey.Key,ToWalletAddress=other.PublicKey.Key,
            OwnerPrivateKey=mode=="wrong-signer" ? other.PrivateKey.Key:owner.PrivateKey.Key,
            FromWalletPrivateKey=mode=="conflicting-key" ? other.PrivateKey.Key:null});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,proxy.Calls);
        Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeSolUsesExplicitSignerExactAmountAndOriginalFinalizedSignature(bool synchronous,bool lostReply)
    {
        var signer=new Account();var recipient=new Account();var owner=new Account();
        var rpc=DispatchProxy.Create<IRpcClient,TokenRpc>();var proxy=(TokenRpc)(object)rpc;proxy.LostReply=lostReply;
        var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,rpc);
        var request=new SendWeb3TokenRequest {FromWalletAddress=signer.PublicKey.Key,ToWalletAddress=recipient.PublicKey.Key,
            OwnerPrivateKey=synchronous ? signer.PrivateKey.Key:null,FromWalletPrivateKey=synchronous ? null:signer.PrivateKey.Key,Amount=0.000000001m,MemoText="owned memo"};
        var result=synchronous ? provider.SendToken(request):await provider.SendTokenAsync(request);
        Assert.AreEqual(1,proxy.Sends);Assert.IsNotNull(proxy.Signed);
        var signature=Encoders.Base58.EncodeData(proxy.Signed.AsSpan(1,64).ToArray());
        Assert.IsTrue(signer.Verify(proxy.Signed.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
        var transaction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed);
        Assert.AreEqual(2,transaction.Instructions.Count);
        CollectionAssert.AreEqual(SystemProgram.Transfer(signer.PublicKey,recipient.PublicKey,1).Data,transaction.Instructions[0].Data);
        Assert.AreEqual(signer.PublicKey.Key,transaction.Instructions[0].Keys[0].PublicKey);
        Assert.AreEqual(recipient.PublicKey.Key,transaction.Instructions[0].Keys[1].PublicKey);
        if(lostReply)
        {
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(0,proxy.StatusReads);
            var error=(SolanaTransactionReconciliationException)result.Exception;Assert.AreEqual(signature,error.OriginalTransactionId);
            Assert.IsInstanceOfType(error.InnerException,typeof(IOException));Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));
        }
        else {Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(signature,result.Result.TransactionResult);Assert.AreEqual(1,proxy.StatusReads);}
    }

    [DataTestMethod]
    [DataRow("0")] [DataRow("-1")] [DataRow("0.0000000001")] [DataRow("18446744073.709551616")]
    public async Task InvalidNativeAmountCannotClaimSuccessOrReachRpc(string amount)
    {
        var owner=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest {Amount=decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture)});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidMintAndBurnNeverReturnFabricatedSuccess(bool mint)
    {
        var owner=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",owner.PrivateKey.Key,owner.PublicKey.Key);
        var result=mint ? await provider.MintTokenAsync(new MintWeb3TokenRequest()):await provider.BurnTokenAsync(new BurnWeb3TokenRequest {OwnerPublicKey=owner.PublicKey.Key,OwnerPrivateKey=owner.PrivateKey.Key,OwnerSeedPhrase=""});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }

    public class TokenRpc : DispatchProxy
    {
        public bool LostReply;public int Sends,StatusReads,Calls;public byte[]? Signed;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Calls++;var resultType=method!.ReturnType.GetGenericArguments()[0];string json;
            if(method.Name=="GetLatestBlockHashAsync") json=JsonConvert.SerializeObject(new{Value=new{Blockhash=new Account().PublicKey.Key,LastValidBlockHeight=42}});
            else if(method.Name=="SendTransactionAsync")
            {
                Sends++;Signed=(byte[])args![0]!;
                if(LostReply) return typeof(Task).GetMethods().Single(item=>item.Name=="FromException" && item.IsGenericMethod).MakeGenericMethod(resultType).Invoke(null,new object[]{new IOException("owned lost send response")});
                json=JsonConvert.SerializeObject(Encoders.Base58.EncodeData(Signed.AsSpan(1,64).ToArray()));
            }
            else if(method.Name=="GetSignatureStatusesAsync") {StatusReads++;json=JsonConvert.SerializeObject(new{Value=new[]{new{confirmationStatus="finalized",err=(object?)null}}});}
            else throw new InvalidOperationException("Unexpected token RPC: "+method.Name);
            return Respond(method,json);
        }
        protected static object? Respond(MethodInfo method,string json)
        {
            var resultType=method.ReturnType.GetGenericArguments()[0];
            var valueType=resultType.GetGenericArguments()[0];var value=JsonConvert.DeserializeObject(json,valueType);
            var response=Activator.CreateInstance(resultType,new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK),value);
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[]{response});
        }
    }
}

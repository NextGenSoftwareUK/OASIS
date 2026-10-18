using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using Solnet.Rpc;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaNftBurnTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task LegacyNftBurnUsesMetaplexAccountsOwnerAndFinalizedReceipt(bool collection,bool lost)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,BurnProxy>();var proxy=(BurnProxy)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Collection=collection;proxy.Scenario=lost ? "lost-reply":"finalized";
        var service=new SolanaService(new Account(),rpc);
        var result=await service.BurnNftAsync(new BurnWeb3NFTRequest {NFTTokenAddress=proxy.Mint.Key,OwnerPrivateKey=owner.PrivateKey.Key,OwnerPublicKey=owner.PublicKey.Key,OwnerSeedPhrase=""});
        Assert.AreEqual(lost,result.IsError);Assert.AreEqual(1,proxy.Sends);
        Assert.AreEqual(lost ? 0:1,proxy.StatusReads);
        if(lost) Assert.AreEqual(proxy.Signature,((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
        else {Assert.IsTrue(result.IsSaved);Assert.AreEqual(proxy.Signature,result.Result.TransactionHash);}
        var instruction=Solnet.Rpc.Models.Transaction.Deserialize(proxy.Signed!).Instructions.Single();
        CollectionAssert.AreEqual(new byte[] {29},instruction.Data);
        CollectionAssert.AreEqual(Solnet.Metaplex.NFT.Library.MetadataProgram.ProgramIdKey.KeyBytes,instruction.ProgramId);
        Assert.AreEqual(collection ? 7:6,instruction.Keys.Count);
        Assert.AreEqual(proxy.Mint.Key,instruction.Keys[2].PublicKey);
        Assert.AreEqual(owner.PublicKey.Key,instruction.Keys[1].PublicKey);
        Assert.IsTrue(owner.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
    }

    [DataTestMethod]
    [DataRow("owner")] [DataRow("mint")] [DataRow("truncated")] [DataRow("programmable")]
    [DataRow("simulation-failure")] [DataRow("signer")]
    public async Task InvalidNftBurnCannotBroadcast(string mode)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,BurnProxy>();var proxy=(BurnProxy)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Mode=mode;proxy.Scenario=mode=="simulation-failure" ? mode:"finalized";
        var result=await new SolanaService(new Account(),rpc).BurnNftAsync(new BurnWeb3NFTRequest
        {NFTTokenAddress=proxy.Mint.Key,OwnerPrivateKey=owner.PrivateKey.Key,OwnerPublicKey=mode=="signer" ? new Account().PublicKey.Key:owner.PublicKey.Key,OwnerSeedPhrase=""});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsNotNull(result.Exception);
        Assert.AreEqual(0,proxy.Sends);Assert.AreEqual(0,proxy.StatusReads);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PublicNftBurnAdapterPreservesFinalizedOrUnknownReceipt(bool lost)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,BurnProxy>();var proxy=(BurnProxy)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Scenario=lost ? "lost-reply":"finalized";
        var service=new SolanaService(new Account(),rpc);var identity=new Account();
        var provider=new SolanaOASIS("http://127.0.0.1:1",identity.PrivateKey.Key,identity.PublicKey.Key);
        typeof(SolanaOASIS).GetField("_solanaService",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,service);
        typeof(SolanaOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
        var result=await provider.BurnNFTAsync(new BurnWeb3NFTRequest {NFTTokenAddress=proxy.Mint.Key,OwnerPrivateKey=owner.PrivateKey.Key,
            OwnerPublicKey=owner.PublicKey.Key,OwnerSeedPhrase=""});
        Assert.AreEqual(lost,result.IsError);Assert.AreEqual(!lost,result.IsSaved);Assert.AreEqual(1,proxy.Sends);
        if(lost){Assert.IsNull(result.Result);Assert.AreEqual(proxy.Signature,((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);}
        else Assert.AreEqual(proxy.Signature,result.Result.TransactionResult);
    }

    [DataTestMethod]
    [DataRow("seed")] [DataRow("service-owner")] [DataRow("dual-sources")]
    [DataRow("seed-mismatch")] [DataRow("missing-public-key")]
    public async Task BurnSignerSourcesAreExplicitAndValidatedBeforeRpc(string scenario)
    {
        const string syntheticSeed="abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        var owner=new Wallet(syntheticSeed).GetAccount(0);
        var rpc=DispatchProxy.Create<IRpcClient,BurnProxy>();var proxy=(BurnProxy)(object)rpc;
        proxy.Owner=owner.PublicKey;proxy.Scenario="finalized";
        var expectedFailure=scenario is "dual-sources" or "seed-mismatch" or "missing-public-key";
        var result=await new SolanaService(owner,rpc).BurnNftAsync(new BurnWeb3NFTRequest
        {
            NFTTokenAddress=proxy.Mint.Key,
            OwnerSeedPhrase=scenario is "seed" or "dual-sources" or "seed-mismatch" ? syntheticSeed:"",
            OwnerPrivateKey=scenario is "dual-sources" or "missing-public-key" ? owner.PrivateKey.Key:"",
            OwnerPublicKey=scenario=="seed-mismatch" ? new Account().PublicKey.Key:scenario=="missing-public-key" ? "":owner.PublicKey.Key
        });
        Assert.AreEqual(expectedFailure,result.IsError,result.Message);
        Assert.AreEqual(expectedFailure ? 0:1,proxy.MetadataReads);Assert.AreEqual(expectedFailure ? 0:1,proxy.Sends);
        if(expectedFailure) Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
        else Assert.IsTrue(owner.Verify(proxy.Signed!.AsSpan(65).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
    }

    public class BurnProxy:SolanaCollectionSizeTests.CollectionProxy
    {
        public PublicKey Owner=null!,Mint=new Account().PublicKey;public bool Collection;public string Mode="";public int MetadataReads;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name!="GetAccountInfoAsync") return base.Invoke(method,args);
            MetadataReads++;
            using var stream=new MemoryStream();using var writer=new BinaryWriter(stream);
            writer.Write((byte)4);writer.Write(Owner.KeyBytes);writer.Write((Mode=="mint" ? new Account().PublicKey:Mint).KeyBytes);
            writer.Write(0U);writer.Write(0U);writer.Write(0U);writer.Write((ushort)0);
            writer.Write((byte)0);writer.Write((byte)0);writer.Write((byte)1);writer.Write((byte)0);
            writer.Write((byte)1);writer.Write((byte)(Mode=="programmable" ? 4:0));
            writer.Write((byte)(Collection ? 1:0));if(Collection){writer.Write((byte)1);writer.Write(new Account().PublicKey.KeyBytes);}
            var bytes=Mode=="truncated" ? new byte[] {4}:stream.ToArray();
            var json=JsonConvert.SerializeObject(new {Value=new {owner=Mode=="owner" ? Owner.Key:Solnet.Metaplex.NFT.Library.MetadataProgram.ProgramIdKey.Key,
                executable=false,data=new[] {Convert.ToBase64String(bytes),"base64"}}});
            var resultType=method.ReturnType.GetGenericArguments()[0];var valueType=resultType.GetGenericArguments()[0];
            var response=Activator.CreateInstance(resultType,new HttpResponseMessage(System.Net.HttpStatusCode.OK),JsonConvert.DeserializeObject(json,valueType));
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] {response});
        }
    }
}

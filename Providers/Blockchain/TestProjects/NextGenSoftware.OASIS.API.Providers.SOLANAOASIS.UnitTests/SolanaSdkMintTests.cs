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
public class SolanaSdkMintTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ActualMetaplexSdkMintUsesFinalizedSubmissionAndKeepsLostReplyIdentity(bool lost)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,MintProxy>();var proxy=(MintProxy)(object)rpc;
        proxy.Scenario=lost ? "lost-reply":"finalized";
        var result=await new SolanaService(owner,rpc).MintNftAsync(new MintWeb3NFTRequest
        {Title="SDK boundary NFT",Symbol="OASIS",JSONMetaDataURL="https://example.invalid/nft.json",FreezeMetadata=false});
        Assert.AreEqual(lost,result.IsError,result.Exception?.ToString() ?? result.Message);
        Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(lost ? 0:1,proxy.StatusReads);
        if(lost)
        {
            Assert.IsNull(result.Result);Assert.IsInstanceOfType(result.Exception,typeof(SolanaTransactionReconciliationException));
            Assert.AreEqual(proxy.Signature,((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
            Assert.AreSame(proxy.LostReply,result.Exception.InnerException);
        }
        else {Assert.IsTrue(result.IsSaved);Assert.AreEqual(proxy.Signature,result.Result.TransactionHash);}
        Assert.AreEqual((byte)2,proxy.Signed![0]);
        Assert.IsTrue(owner.Verify(proxy.Signed.AsSpan(129).ToArray(),proxy.Signed.AsSpan(1,64).ToArray()));
    }

    [TestMethod]
    public async Task ActualSdkMintWaitsForFinalizedStatusWithoutSecondBroadcast()
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,MintProxy>();var proxy=(MintProxy)(object)rpc;proxy.Scenario="pending";
        var result=await new SolanaService(owner,rpc).MintNftAsync(new MintWeb3NFTRequest {Title="NFT",Symbol="NFT",JSONMetaDataURL="https://example.invalid/nft.json"});
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(2,proxy.StatusReads);
    }

    [TestMethod]
    public async Task PostMintUnknownVerificationKeepsConfirmedMintAndPendingVerificationIdentities()
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,MintProxy>();var proxy=(MintProxy)(object)rpc;proxy.FailVerification=true;proxy.Scenario="finalized";
        var result=await new SolanaService(owner,rpc).MintNftAsync(new MintWeb3NFTRequest {Title="NFT",Symbol="NFT",JSONMetaDataURL="https://example.invalid/nft.json",
            CollectionPublicKey=new Account().PublicKey.Key,WaitTillNFTVerified=true});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual(2,proxy.Sends);
        var partial=(SolanaPartialMutationException)result.Exception;
        Assert.AreEqual(proxy.Submitted[0],partial.CommittedSignatures.Single());
        var pending=(SolanaTransactionReconciliationException)partial.InnerException!;
        Assert.AreEqual(proxy.Submitted[1],pending.OriginalTransactionId);Assert.AreSame(proxy.LostReply,pending.InnerException);
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task SdkSubmissionWrapperRefusesReplayAfterUnknownOutcome(bool encoded)
    {
        var rpc=DispatchProxy.Create<IRpcClient,MintProxy>();var proxy=(MintProxy)(object)rpc;proxy.Scenario="lost-reply";
        var wrapped=((IRpcClient Client,FinalizedSolanaRpc State))typeof(FinalizedSolanaRpc).GetMethod("Wrap",BindingFlags.NonPublic|BindingFlags.Static)!
            .Invoke(null,new object[] {rpc,new Account()})!;
        var bytes=new byte[70];bytes[0]=1;bytes[1]=31;
        Task<Solnet.Rpc.Core.Http.RequestResult<string>> Send() => encoded ? wrapped.Client.SendTransactionAsync(Convert.ToBase64String(bytes)):wrapped.Client.SendTransactionAsync(bytes);
        var first=await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(Send);
        var second=await Assert.ThrowsExceptionAsync<SolanaTransactionReconciliationException>(Send);
        Assert.AreSame(first,second);Assert.AreSame(first,wrapped.State.SubmissionFailure);Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(0,wrapped.State.FinalizedSignatures.Count);
    }

    [DataTestMethod]
    [DataRow("valid")] [DataRow("mint-unknown")] [DataRow("size-unknown")] [DataRow("size-preflight")]
    public async Task ActualSdkCollectionCreationRetainsBothFinalizedOrPartialNativeOutcomes(string scenario)
    {
        var owner=new Account();var rpc=DispatchProxy.Create<IRpcClient,MintProxy>();var proxy=(MintProxy)(object)rpc;
        proxy.Scenario=scenario=="mint-unknown" ? "lost-reply":"finalized";
        proxy.FailVerification=scenario=="size-unknown";proxy.FailFollowupSimulation=scenario=="size-preflight";
        var result=await new SolanaService(owner,rpc).CreateCollectionNftAsync("SDK collection","OASIS","https://example.invalid/collection.json",19);
        Assert.AreEqual(scenario!="valid",result.IsError,result.Exception?.ToString());
        if(scenario=="valid")
        {
            Assert.IsTrue(result.IsSaved);Assert.AreEqual(2,proxy.Sends);Assert.AreEqual(2,proxy.StatusReads);
            Assert.AreEqual(proxy.Submitted[0],result.Result.TransactionHash);Assert.AreEqual(proxy.Submitted[1],result.Result.SetCollectionSizeTransactionHash);
        }
        else
        {
            Assert.IsNull(result.Result);Assert.IsFalse(result.IsSaved);
            if(scenario=="mint-unknown")
            {
                Assert.AreEqual(1,proxy.Sends);Assert.AreEqual(proxy.Submitted[0],((SolanaTransactionReconciliationException)result.Exception).OriginalTransactionId);
            }
            else
            {
                var partial=(SolanaPartialMutationException)result.Exception;
                Assert.AreEqual(proxy.Submitted[0],partial.CommittedSignatures.Single());
                if(scenario=="size-unknown")
                {
                    Assert.AreEqual(2,proxy.Sends);
                    Assert.AreEqual(proxy.Submitted[1],((SolanaTransactionReconciliationException)partial.InnerException!).OriginalTransactionId);
                }
                else {Assert.AreEqual(1,proxy.Sends);Assert.IsInstanceOfType(partial.InnerException,typeof(InvalidOperationException));}
            }
        }
    }

    [TestMethod]
    public async Task FreezeAtCreationIsEncodedBySdkWithoutSecondMutation()
    {
        var owner=new Account();var metadata=new List<byte[]>();
        foreach(var freeze in new[] {false,true})
        {
            var rpc=DispatchProxy.Create<IRpcClient,MintProxy>();var proxy=(MintProxy)(object)rpc;proxy.Scenario="finalized";
            var result=await new SolanaService(owner,rpc).MintNftAsync(new MintWeb3NFTRequest
            {Title="freeze fixture",Symbol="OASIS",JSONMetaDataURL="https://example.invalid/nft.json",FreezeMetadata=freeze});
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,proxy.Sends,"Freeze must be encoded at creation, not a second mutation.");
            var tx=Solnet.Rpc.Models.Transaction.Deserialize(proxy.SignedTransactions.Single());
            metadata.Add(tx.Instructions.Where(instruction=>instruction.ProgramId.SequenceEqual(Solnet.Metaplex.NFT.Library.MetadataProgram.ProgramIdKey.KeyBytes))
                .SelectMany(instruction=>instruction.Data).ToArray());
        }
        Assert.AreEqual(metadata[0].Length,metadata[1].Length);Assert.IsTrue(metadata[0].Length>0);
        var differences=Enumerable.Range(0,metadata[0].Length).Where(index=>metadata[0][index]!=metadata[1][index]).ToArray();
        Assert.AreEqual(1,differences.Length);Assert.AreEqual((byte)1,metadata[0][differences[0]]);Assert.AreEqual((byte)0,metadata[1][differences[0]]);
    }

    public class MintProxy:SolanaCollectionSizeTests.CollectionProxy
    {
        public bool FailVerification,FailFollowupSimulation;public List<string> Submitted=new();
        public List<byte[]> SignedTransactions=new();
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            string? json=method!.Name switch
            {
                "GetMinimumBalanceForRentExemptionAsync"=>"1000000",
                "GetBalanceAsync"=>"{\"value\":100000000000}",
                "GetAccountInfoAsync"=>"{\"value\":null}",_=>null
            };
            if(json==null)
            {
                if(method.Name=="SendTransactionAsync" && FailVerification && Sends==1) Scenario="lost-reply";
                if(method.Name=="SimulateTransactionAsync" && FailFollowupSimulation && Sends==1) Scenario="simulation-failure";
                var before=Sends;
                try {return base.Invoke(method,args);}
                finally {if(Sends>before){Submitted.Add(Signature);SignedTransactions.Add((byte[])Signed!.Clone());}}
            }
            var resultType=method.ReturnType.GetGenericArguments()[0];var valueType=resultType.GetGenericArguments()[0];
            var response=Activator.CreateInstance(resultType,new HttpResponseMessage(System.Net.HttpStatusCode.OK),JsonConvert.DeserializeObject(json,valueType));
            resultType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(resultType).Invoke(null,new[] {response});
        }
    }
}

using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Rpc;
using Solnet.Rpc.Types;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaChainIndexObservationTests
{
    [DataTestMethod]
    [DataRow("valid")] [DataRow("empty")] [DataRow("pruned")] [DataRow("pruned-later")]
    [DataRow("network")] [DataRow("missing")] [DataRow("wrong-slot")] [DataRow("order")]
    [DataRow("duplicate-version")] [DataRow("duplicate")] [DataRow("version-zero")] [DataRow("missing-version-id")]
    [DataRow("cancelled")] [DataRow("catalogue")]
    public async Task CompleteRangePreservesRevisionsAndNeverPublishesPartialOrPrunedCoverage(string scenario)
    {
        var account=new Account();var rpc=DispatchProxy.Create<IRpcClient,RangeRpc>();var proxy=(RangeRpc)(object)rpc;
        proxy.Mode=scenario;proxy.Writer=account.PublicKey.Key;
        var key=RandomNumberGenerator.GetBytes(32);
        try{
            var repository=new SolanaRepository(account,rpc,()=> (byte[])key.Clone(),SolanaReadFixture.From(rpc));
            var id=Guid.NewGuid();
            string Memo(SolanaHolonDto dto)=>Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes((string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(SolanaHolonDto)).Invoke(repository,new object[]{dto})!));
            proxy.Memos["new"]=Memo(new SolanaHolonDto{Id=id,Version=2,VersionId=Guid.NewGuid(),IsDeleted=true,DeletedDate=DateTime.UtcNow});
            proxy.Memos["old"]=Memo(new SolanaHolonDto{Id=id,Version=scenario=="version-zero"?0:scenario=="duplicate-version"?2:1,VersionId=scenario=="missing-version-id"?Guid.Empty:Guid.NewGuid()});
            var source=new CosmicChainQuerySource(ProviderType.SolanaOASIS,RangeRpc.Genesis,ProviderType.SQLLiteDBOASIS,Guid.NewGuid(),1,
                scenario=="catalogue"?new string('B',64):SolanaRepository.ChainHolonCatalogueHash(proxy.Writer,RangeRpc.Genesis),1);
            using var cancellation=new CancellationTokenSource();if(scenario=="cancelled")cancellation.Cancel();
            var attempts=new AttemptOwner();
            var result=await CosmicIndexedReadAttempts.RunAsync(source,attempts,
                ()=>repository.ObserveFinalizedHolonRangeAsync(source,cancellation.Token));
            Assert.AreEqual(proxy.Calls,attempts.Operations.Count,"Each actual RPC must have its own attempt boundary.");
            if(scenario is "valid" or "empty"){
                Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
                Assert.AreEqual(10L,result.Result.Checkpoint.Position.Slot);Assert.AreEqual(1L,result.Result.Checkpoint.CompleteFromSlot);
                Assert.AreEqual(RangeRpc.Hash(10),result.Result.Head.Position.BlockIdentity);
                Assert.AreEqual(scenario=="empty"?0:2,result.Result.Records.Count);
                Assert.AreEqual(2,proxy.RetentionCalls);
                if(scenario=="valid"){
                    Assert.IsTrue(result.Result.Records[0].Receipt.Record.IsDeleted);
                    Assert.AreEqual(1,result.Result.Records[1].Receipt.Record.Version);
                    var independent=await repository.ObserveFinalizedHolonAsync(source,"old");Assert.IsFalse(independent.IsError,independent.Exception?.ToString());
                    Assert.AreEqual(result.Result.Records[1].CanonicalJson,independent.Result.CanonicalJson);
                    Assert.AreEqual(result.Result.Records[1].Receipt.Record.CanonicalHash,independent.Result.Receipt.Record.CanonicalHash);
                }
            }else{
                Assert.IsTrue(result.IsError,scenario);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                if(scenario is "cancelled" or "catalogue")Assert.AreEqual(0,proxy.Calls);
            }
            Assert.AreEqual(0,proxy.Writes);
        }finally{CryptographicOperations.ZeroMemory(key);}
    }

    private sealed class AttemptOwner:ICosmicIndexedReadAttemptOwner
    {
        public readonly List<CosmicIndexedReadOperation> Operations=new();
        public async Task<T> ExecuteAsync<T>(CosmicChainQuerySource source,CosmicIndexedReadOperation operation,
            Func<Task<T>> invoke,CancellationToken cancellationToken)
        { Operations.Add(operation);return await invoke(); }
    }

    public class RangeRpc:DispatchProxy
    {
        public static string Genesis=>Encoders.Base58.EncodeData(new byte[32]);
        public static string Hash(int slot)=>Encoders.Base58.EncodeData(Enumerable.Repeat((byte)slot,32).ToArray());
        public string Mode="",Writer="";public int Calls,Writes,Pages,RetentionCalls;
        public Dictionary<string,string> Memos=new();
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            Calls++;string json;
            switch(method!.Name){
                case "GetGenesisHashAsync":json=JsonConvert.SerializeObject(Mode=="network"?Hash(1):Genesis);break;
                case "GetFirstAvailableBlockAsync":RetentionCalls++;json=Mode=="pruned"||Mode=="pruned-later"&&RetentionCalls>1?"2":"1";break;
                case "GetSlotAsync":Assert.AreEqual(Commitment.Finalized,args![0]);json="10";break;
                case "GetBlockAsync":Assert.AreEqual(Commitment.Finalized,args![1]);json=JsonConvert.SerializeObject(new{blockhash=Hash(Convert.ToInt32(args[0]))});break;
                case "GetSignaturesForAddressAsync":
                    Assert.AreEqual(Writer,args![0]);Assert.AreEqual(Commitment.Finalized,args[4]);Pages++;
                    json=Mode=="empty"||Pages>1?"[]":JsonConvert.SerializeObject(
                        (Mode=="duplicate"?new[]{"new","new"}:Mode=="order"?new[]{"old","new"}:new[]{"new","old"})
                        .Select(signature=>new{signature,slot=signature=="new"?7:5,confirmationStatus="finalized"}));break;
                case "GetTransactionAsync":
                    Assert.AreEqual(Commitment.Finalized,args![1]);var signature=(string)args[0]!;
                    json=Mode=="missing"?"null":JsonConvert.SerializeObject(new{Slot=Mode=="wrong-slot"?9:signature=="new"?7:5,Meta=new{},Transaction=new{Signatures=new[]{signature},Message=new{
                        AccountKeys=new[]{Writer,"MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr"},Instructions=new[]{new{ProgramIdIndex=1,Data=Memos[signature]}}}}});break;
                default:Writes++;throw new InvalidOperationException("Unexpected read-only RPC "+method.Name);
            }
            var responseType=method.ReturnType.GetGenericArguments()[0];var valueType=responseType.GetGenericArguments()[0];
            var response=Activator.CreateInstance(responseType,new HttpResponseMessage(System.Net.HttpStatusCode.OK),JsonConvert.DeserializeObject(json,valueType));
            responseType.GetProperty("WasRequestSuccessfullyHandled")!.SetValue(response,true);
            return typeof(Task).GetMethod("FromResult")!.MakeGenericMethod(responseType).Invoke(null,new[]{response});
        }
    }
}

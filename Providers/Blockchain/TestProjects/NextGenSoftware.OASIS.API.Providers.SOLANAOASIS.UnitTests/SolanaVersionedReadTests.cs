using System.Net;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using Solnet.Programs;
using Solnet.Rpc;
using Solnet.Wallet;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public sealed class SolanaVersionedReadTests
{
    [DataTestMethod][DataRow("legacy")][DataRow("v0")][DataRow("v1")]
    public async Task RealSdkHistoryReadsEverySupportedFormatAndResolvesLoadedAddresses(string version)
    {
        using var handler=new VersionedHandler(version);
        using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var provider=Provider(handler,http);
        var result=await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=handler.Owner.PublicKey.Key});
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsNotNull(result.Result);
        Assert.AreEqual(1,result.Result.Count);
        Assert.AreEqual("7",result.Result[0].RawAmount);
        Assert.AreEqual(handler.Owner.PublicKey.Key,result.Result[0].FromWalletAddress);
        Assert.AreEqual(handler.Recipient.PublicKey.Key,result.Result[0].ToWalletAddress);
        AssertVersionOptions(handler);
    }

    [DataTestMethod][DataRow("legacy")][DataRow("v0")][DataRow("v1")]
    public async Task BridgeStatusRequiresTheOriginalFinalizedVersionedTransaction(string version)
    {
        using var handler=new VersionedHandler(version);
        using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var result=await Provider(handler,http).GetTransactionStatusAsync(handler.Signature);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        Assert.AreEqual(BridgeTransactionStatus.Completed,result.Result);
        AssertVersionOptions(handler);
    }

    [DataTestMethod]
    [DataRow("legacy",false)][DataRow("legacy",true)][DataRow("v0",false)][DataRow("v0",true)][DataRow("v1",false)][DataRow("v1",true)]
    public async Task ActualSdkStorageReadsAuthenticateUnchangedEncryptedRecords(string version,bool collection)
    {
        using var handler=new VersionedHandler(version);
        using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var rpc=ClientFactory.GetClient(http.BaseAddress.ToString(),null,http,null);
        var key=System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        try{
            var repository=new SolanaRepository(handler.Owner,rpc,()=>key.ToArray(),
                signature=>SolanaTransactionReader.ReadAsync(http.BaseAddress,signature,handler));
            var source=new SolanaAvatarDto{Id=Guid.NewGuid(),Version=3,UserName="versioned native memo"};
            var cipher=(string)typeof(SolanaRepository).GetMethod("EncodeEntity",BindingFlags.Instance|BindingFlags.NonPublic)!
                .MakeGenericMethod(source.GetType()).Invoke(repository,new object[]{source})!;
            handler.Memo=Encoders.Base58.EncodeData(Encoding.UTF8.GetBytes(cipher));
            var records=collection ? (await repository.GetAllAsync<SolanaAvatarDto>()).Select(entry=>entry.Record).ToArray()
                :new[]{await repository.GetAsync<SolanaAvatarDto>(handler.Signature)};
            Assert.AreEqual(1,records.Length);Assert.AreEqual(source.Id,records[0].Id);
            Assert.AreEqual(3,records[0].Version);Assert.AreEqual(source.UserName,records[0].UserName);
            AssertVersionOptions(handler);
        }finally{System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);}
    }

    [DataTestMethod]
    [DataRow("rpcError")][DataRow("null")][DataRow("failed")][DataRow("http202")]
    [DataRow("wrongSignature")][DataRow("missingOutcome")][DataRow("missingConfig")][DataRow("futureVersion")]
    [DataRow("pending")][DataRow("finalizedMissing")][DataRow("statusError")][DataRow("statusMalformed")][DataRow("negativeVersion")]
    public async Task BridgeReportsOnlyConfirmedAbsenceAndPreservesErrors(string mode)
    {
        using var handler=new VersionedHandler("v1"){Mode=mode};
        using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var result=await Provider(handler,http).GetTransactionStatusAsync(handler.Signature);
        if(mode is "null" or "failed" or "pending"){
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(mode=="null"?BridgeTransactionStatus.NotFound:mode=="pending"?BridgeTransactionStatus.Pending:BridgeTransactionStatus.Canceled,result.Result);
        }else{Assert.IsTrue(result.IsError,"RPC/format failures cannot become NotFound or Completed.");Assert.IsFalse(result.IsLoaded);}
        AssertVersionOptions(handler);
    }

    [DataTestMethod]
    [DataRow("v0","missingLoaded")][DataRow("v0","badLoadedKey")][DataRow("v1","missingConfig")]
    [DataRow("v1","loadedV1")][DataRow("v1","missingOutcome")][DataRow("v1","futureVersion")]
    public async Task UnverifiableVersionedHistoryCannotBecomeAPartialCollection(string version,string mode)
    {
        using var handler=new VersionedHandler(version){Mode=mode};
        using var http=new HttpClient(handler){BaseAddress=new Uri("http://127.0.0.1:1")};
        var result=await Provider(handler,http).GetTransactionsAsync(new GetWeb3TransactionsRequest{WalletAddress=handler.Owner.PublicKey.Key});
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);AssertVersionOptions(handler);
    }

    private static SolanaOASIS Provider(VersionedHandler handler,HttpClient http)
    {
        var provider=new SolanaOASIS(http.BaseAddress!.ToString(),handler.Owner.PrivateKey.Key,handler.Owner.PublicKey.Key,handler);
        typeof(SolanaOASIS).GetField("_rpcClient",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,
            ClientFactory.GetClient(http.BaseAddress.ToString(),null,http,null));
        return provider;
    }

    private static void AssertVersionOptions(VersionedHandler handler)
    {
        Assert.AreEqual("json",handler.Options.GetProperty("encoding").GetString());
        Assert.AreEqual("finalized",handler.Options.GetProperty("commitment").GetString());
        Assert.AreEqual(JsonValueKind.Number,handler.Options.GetProperty("maxSupportedTransactionVersion").ValueKind);
        Assert.AreEqual(1,handler.Options.GetProperty("maxSupportedTransactionVersion").GetInt32());
        Assert.AreEqual(1,handler.TransactionReads,"No downgrade retry or duplicate read.");
    }

    private sealed class VersionedHandler(string version):HttpMessageHandler
    {
        public readonly Account Owner=new(),Recipient=new();
        public string Signature=Encoders.Base58.EncodeData(Enumerable.Repeat((byte)11,64).ToArray());
        public JsonElement Options;public int TransactionReads;
        public string? Memo,Mode;private int _pages;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            using var document=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var root=document.RootElement;var method=root.GetProperty("method").GetString();
            object? result;object? error=null;
            switch(method){
                case "getHealth":result="ok";break;
                case "getSignaturesForAddress":result=++_pages==1 ? new[]{new{signature=Signature,slot=42,err=(object?)null,blockTime=1700000000}}:Array.Empty<object>();break;
                case "getTransaction":
                    TransactionReads++;Options=root.GetProperty("params")[1].Clone();
                    var supported=Options.TryGetProperty("maxSupportedTransactionVersion",out var max) ? max.GetInt32():-1;
                    if((version=="v0" && supported<0)||(version=="v1" && supported<1)){
                        result=null;error=new{code=-32015,message="Transaction version is not supported by this client."};
                    }else if(Mode=="rpcError"){result=null;error=new{code=-32005,message="Node unhealthy"};}
                    else result=Mode is "null" or "pending" or "finalizedMissing" or "statusError" or "statusMalformed"?null:Native();
                    break;
                case "getSignatureStatuses":
                    var parameters=root.GetProperty("params");
                    Assert.AreEqual(Signature,parameters[0][0].GetString());
                    Assert.IsTrue(parameters[1].GetProperty("searchTransactionHistory").GetBoolean());
                    result=new{context=new{slot=50},value=Mode=="null"?new object?[]{null}:new object?[]{new{
                        slot=42,confirmations=Mode=="finalizedMissing"?(int?)null:1,err=(object?)null,status=new{Ok=(object?)null},
                        confirmationStatus=Mode=="finalizedMissing"?"finalized":Mode=="statusMalformed"?"unknown":"confirmed"}}};
                    if(Mode=="statusError")error=new{code=-32005,message="Status history unavailable"};
                    break;
                default:throw new AssertFailedException("Unexpected RPC "+method);
            }
            var envelope=error is null ? JObject.FromObject(new{jsonrpc="2.0",id=root.GetProperty("id").GetInt32(),result})
                :JObject.FromObject(new{jsonrpc="2.0",id=root.GetProperty("id").GetInt32(),error});
            return new HttpResponseMessage(method=="getTransaction" && Mode=="http202"?HttpStatusCode.Accepted:HttpStatusCode.OK)
                {Content=new StringContent(envelope.ToString(),Encoding.UTF8,"application/json")};
        }

        private JObject Native()
        {
            var loaded=version=="v0";
            var memo=Memo is not null;
            var program=memo ? "MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr":SystemProgram.ProgramIdKey.Key;
            var value=JObject.FromObject(new{slot=42,blockTime=1700000000,transaction=new{signatures=new[]{Signature},message=new{
                header=new{numRequiredSignatures=1,numReadonlySignedAccounts=0,numReadonlyUnsignedAccounts=loaded?0:1},
                accountKeys=loaded ? new[]{Owner.PublicKey.Key}:memo?new[]{Owner.PublicKey.Key,program}:new[]{Owner.PublicKey.Key,Recipient.PublicKey.Key,program},
                recentBlockhash=Owner.PublicKey.Key,instructions=new[]{new{programIdIndex=memo?1:2,accounts=memo?new[]{0}:new[]{0,1},
                    data=Memo??Encoders.Base58.EncodeData(SystemProgram.Transfer(Owner.PublicKey,Recipient.PublicKey,7).Data)}}}},
                meta=new{fee=5000,err=(object?)null,loadedAddresses=new{
                    writable=loaded && !memo?new[]{Recipient.PublicKey.Key}:Array.Empty<string>(),
                    @readonly=loaded?new[]{program}:Array.Empty<string>()}}});
            value["version"]=version=="legacy" ? new JValue("legacy"):new JValue(version=="v0"?0:1);
            if(version=="v1")value["transaction"]!["message"]!["transactionConfig"]=JObject.FromObject(new{priorityFee=17,computeUnitLimit=200000});
            switch(Mode){
                case "failed":value["meta"]!["err"]="AccountNotFound";break;
                case "wrongSignature":value["transaction"]!["signatures"]![0]=Encoders.Base58.EncodeData(new byte[64]);break;
                case "missingOutcome":((JObject)value["meta"]!).Remove("err");break;
                case "missingConfig":((JObject)value["transaction"]!["message"]!).Remove("transactionConfig");break;
                case "futureVersion":value["version"]=2;break;
                case "negativeVersion":value["version"]=-1;break;
                case "missingLoaded":((JObject)value["meta"]!).Remove("loadedAddresses");break;
                case "badLoadedKey":value["meta"]!["loadedAddresses"]!["writable"]![0]="invalid";break;
                case "loadedV1":value["meta"]!["loadedAddresses"]!["writable"]=JArray.FromObject(new[]{Recipient.PublicKey.Key});break;
            }
            return value;
        }
    }
}

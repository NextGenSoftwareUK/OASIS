using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.UnitTests;

[TestClass]
public class DynamoConditionalWriteTests
{
    private sealed class Client:AmazonDynamoDBClient
    {
        public Dictionary<string,Dictionary<string,AttributeValue>> Rows=new();
        public int Writes,Reads;
        public bool LoseAck,Reject;
        public Client():base(new BasicAWSCredentials("owned","owned"),new AmazonDynamoDBConfig {
            ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        private static string Key(string table,string id)=>table+"/"+id;
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken token=default) {
            Reads++;token.ThrowIfCancellationRequested();
            Rows.TryGetValue(Key(request.TableName,request.Key["id"].S),out var item);
            return Task.FromResult(new GetItemResponse {HttpStatusCode=HttpStatusCode.OK,Item=item??new()});
        }
        public override Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Writes++;Assert.AreEqual(32,request.ClientRequestToken.Length);
            Assert.IsTrue(request.TransactItems.Count<=100);
            if(Reject)throw new TransactionCanceledException("owned condition rejection") {
                CancellationReasons=new(){new(){Code="ConditionalCheckFailed"}}
            };
            foreach(var action in request.TransactItems) {
                if(action.Put!=null)Rows[Key(action.Put.TableName,action.Put.Item["id"].S)]=action.Put.Item;
                else Rows.Remove(Key(action.Delete.TableName,action.Delete.Key["id"].S));
            }
            if(LoseAck)throw new IOException("owned lost acknowledgement");
            return Task.FromResult(new TransactWriteItemsResponse{HttpStatusCode=HttpStatusCode.OK});
        }
        public override Task<PutItemResponse> PutItemAsync(PutItemRequest request,CancellationToken token=default) {
            var key=Key(request.TableName,request.Item["id"].S);Rows.TryGetValue(key,out var old);
            Rows[key]=request.Item;return Task.FromResult(new PutItemResponse{HttpStatusCode=HttpStatusCode.OK,Attributes=old??new()});
        }
    }
    private static ConditionalHolonWriteRequest Request(Holon holon,Guid op,Guid? revision=null,ConditionalHolonOperation operation=ConditionalHolonOperation.Save)
        =>new(new ConditionalHolonWrite(op,Guid.Empty,ProviderType.DynamoDBOASIS,
            new[]{new HolonWriteCondition(holon.Id,revision)}),operation,new[]{holon},false);
    private static (DynamoDBOASIS Provider,AmazonDynamoDBClient Original) Setup(Client client) {
        var provider=new DynamoDBOASIS("owned","owned","us-east-1");
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;field.SetValue(provider,client);
        return(provider,original);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DurableReceiptReplaysWithoutAnotherMutationAndRejectsChangedIntent(bool sync) {
        using var client=new Client();var (provider,original)=Setup(client);using var owned=original;
        var holon=new Holon{Id=Guid.NewGuid(),Name="original"};var operation=Guid.NewGuid();
        var request=Request(holon,operation);
        var first=sync?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.IsTrue(first.IsSaved);
        var saved=first.Result.Single();Assert.AreEqual(1,saved.Version);Assert.AreNotEqual(Guid.Empty,saved.VersionId);
        Assert.AreEqual(0,holon.Version);Assert.IsFalse(holon.ProviderUniqueStorageKey.ContainsKey(ProviderType.DynamoDBOASIS));
        var replay=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(saved.VersionId,replay.Result.Single().VersionId);
        holon.Name="different";
        var reused=await provider.ExecuteConditionalHolonWriteAsync(Request(holon,operation));
        Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",reused.ErrorCode);Assert.AreEqual(1,client.Writes);
    }
    [TestMethod]
    public async Task OversizedSelectedGraphRejectsBeforeAnySdkRead() {
        using var client=new Client();var (provider,original)=Setup(client);using var owned=original;
        var roots=Enumerable.Range(0,100).Select(_=>new Holon{Id=Guid.NewGuid()}).ToArray();
        var write=new ConditionalHolonWrite(Guid.NewGuid(),Guid.Empty,ProviderType.DynamoDBOASIS,
            roots.Select(row=>new HolonWriteCondition(row.Id,null)));
        var rejected=await provider.ExecuteConditionalHolonWriteAsync(new(write,ConditionalHolonOperation.Save,roots,false));
        Assert.IsTrue(rejected.IsError);Assert.IsInstanceOfType(rejected.Exception,typeof(ArgumentException));
        Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);
    }
    [TestMethod]
    public async Task OrdinarySaveRotatesProviderRevisionAndInvalidatesOldAtomicBaseline() {
        using var client=new Client();var (provider,original)=Setup(client);using var owned=original;
        var first=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid()));
        Assert.IsFalse(first.IsError,first.Exception?.ToString());
        var row=(Holon)first.Result.Single();var oldRevision=CosmicConcurrency.GetProviderRevisionId(row,ProviderType.DynamoDBOASIS);
        row.Name="ordinary replacement";
        var ordinary=await provider.SaveHolonAsync(row,saveChildren:false);
        Assert.IsFalse(ordinary.IsError,ordinary.Exception?.ToString());
        Assert.AreNotEqual(oldRevision,CosmicConcurrency.GetProviderRevisionId(row,ProviderType.DynamoDBOASIS));
        var rejected=await provider.ExecuteConditionalHolonWriteAsync(Request(row,Guid.NewGuid(),oldRevision));
        Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",rejected.ErrorCode);Assert.AreEqual(1,client.Writes);
    }
    [TestMethod]
    public async Task LostAcknowledgementIsNotAutomaticallyReplayedAndOriginalReceiptCanBeRead() {
        using var client=new Client{LoseAck=true};var (provider,original)=Setup(client);using var owned=original;
        var request=Request(new Holon{Id=Guid.NewGuid(),Name="owned"},Guid.NewGuid());
        var unknown=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.AreEqual("COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED",unknown.ErrorCode);
        Assert.IsInstanceOfType(unknown.Exception,typeof(INativeMutationReconciliationRequired));
        Assert.AreEqual(1,client.Writes);
        var resolved=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(resolved.IsError,resolved.Exception?.ToString());Assert.AreEqual(1,client.Writes);
    }
    [TestMethod]
    public async Task NativeConditionRejectionDoesNotBecomeUnknownExecution() {
        using var client=new Client{Reject=true};var (provider,original)=Setup(client);using var owned=original;
        var result=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid()));
        Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",result.ErrorCode);Assert.IsFalse(result.IsSaved);
        Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired);Assert.AreEqual(0,client.Rows.Count);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task CapacityAndCancellationRejectBeforeNativeMutation(bool cancellation) {
        using var client=new Client();var (provider,original)=Setup(client);using var owned=original;
        var holon=new Holon{Id=Guid.NewGuid(),Description=new string('x',410*1024)};
        var result=await provider.ExecuteConditionalHolonWriteAsync(Request(holon,Guid.NewGuid()),
            cancellation?new CancellationToken(true):default);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,client.Writes);
        if(cancellation)Assert.AreEqual(0,client.Reads);
    }
    [DataTestMethod][DataRow(ConditionalHolonOperation.SoftDelete)][DataRow(ConditionalHolonOperation.HardDelete)]
    public async Task ReplaceAndDeleteUseProviderRevisionAndRetainOperationReceipt(ConditionalHolonOperation deletion) {
        using var client=new Client();var (provider,original)=Setup(client);using var owned=original;
        var holon=new Holon{Id=Guid.NewGuid(),Name="owned"};
        var first=await provider.ExecuteConditionalHolonWriteAsync(Request(holon,Guid.NewGuid()));
        var saved=(Holon)first.Result.Single();saved.Name="replacement";
        var replaced=await provider.ExecuteConditionalHolonWriteAsync(Request(saved,Guid.NewGuid(),CosmicConcurrency.GetProviderRevisionId(saved,ProviderType.DynamoDBOASIS)));
        Assert.IsFalse(replaced.IsError,replaced.Exception?.ToString());var current=(Holon)replaced.Result.Single();
        Assert.AreEqual(2,current.Version);
        var request=Request(current,Guid.NewGuid(),CosmicConcurrency.GetProviderRevisionId(current,ProviderType.DynamoDBOASIS),deletion);
        var deleted=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.IsDeleted);
        Assert.IsTrue(deleted.Result.Single().IsDeleted);Assert.AreEqual(3,deleted.Result.Single().Version);
        var replay=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(3,client.Writes);
    }
}

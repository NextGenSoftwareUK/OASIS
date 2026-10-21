using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoConditionalNativeTests
{
    private sealed class NativeClient:AmazonDynamoDBClient
    {
        public bool LoseAck;public int Transactions;public Func<Task>? BeforeTransaction;
        public NativeClient():base(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Interlocked.Increment(ref Transactions);
            if(BeforeTransaction!=null)await BeforeTransaction();
            var result=await base.TransactWriteItemsAsync(request,token);
            if(LoseAck){LoseAck=false;throw new IOException("owned response loss after native commit");}
            return result;
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeAtomicRacePreservesWinnerAndConvergesSameOperation(bool aws,bool concurrentSameOperation)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"atomic-race-{aws}-{concurrentSameOperation}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        using var client=new NativeClient();using var verifier=new NativeClient();
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        var root=new Holon{Id=Guid.NewGuid(),Name="owned atomic race root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned atomic race child",IsActive=true};
        root.Children.Add(child);var ids=new[]{root.Id,child.Id};var operations=new[]{Guid.NewGuid(),Guid.NewGuid()};
        async Task<Dictionary<string,AttributeValue>> Read(string table,string id) {
            var result=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,result.HttpStatusCode);return result.Item;
        }
        ConditionalHolonWriteRequest Request(Holon current,int operation,bool initial)
            =>new(new ConditionalHolonWrite(operations[operation],Guid.Empty,authority,
                new[]{current}.Concat(current.Children).Select(row=>new HolonWriteCondition(row.Id,initial?null:CosmicConcurrency.GetProviderRevisionId(row,authority)))),
                ConditionalHolonOperation.Save,new[]{current});
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var id in ids)Assert.AreEqual(0,(await Read("OasisHolons",id.ToString())).Count);
            foreach(var op in operations)Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",authority+":"+op.ToString("N"))).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operations,authority=authority.ToString(),concurrentSameOperation,state="before-atomic-race"}));file.Flush(true);
            }
            field.SetValue(provider,client);var initialRequest=Request(root,0,true);
            if(concurrentSameOperation) {
                var entered=0;var barrier=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                client.BeforeTransaction=async()=> {
                    if(Interlocked.Increment(ref entered)==2)barrier.TrySetResult();
                    await barrier.Task.WaitAsync(TimeSpan.FromSeconds(15));
                };
                var results=await Task.WhenAll(provider.ExecuteConditionalHolonWriteAsync(initialRequest),provider.ExecuteConditionalHolonWriteAsync(initialRequest));
                Assert.AreEqual(2,entered);Assert.IsTrue(results.Any(result=>!result.IsError));
                foreach(var result in results.Where(result=>result.IsError))
                    Assert.IsTrue(result.Exception is INativeMutationReconciliationRequired||result.ErrorCode=="COSMIC_CONCURRENCY_CONFLICT",result.Exception?.ToString());
                client.BeforeTransaction=null;var attempts=client.Transactions;
                var resolved=await provider.ExecuteConditionalHolonWriteAsync(initialRequest);
                Assert.IsFalse(resolved.IsError,resolved.Exception?.ToString());Assert.AreEqual(attempts,client.Transactions);
                Assert.AreEqual(2,attempts);
                var winner=results.First(result=>!result.IsError).Result.Single();
                Assert.AreEqual(winner.VersionId,resolved.Result.Single().VersionId);
                Assert.AreEqual(winner.Children.Single().VersionId,resolved.Result.Single().Children.Single().VersionId);
            } else {
                var created=await provider.ExecuteConditionalHolonWriteAsync(initialRequest);
                Assert.IsFalse(created.IsError,created.Exception?.ToString());
                var current=(Holon)created.Result.Single();var originalRoot=(await Read("OasisHolons",root.Id.ToString()))["data_json"].S;
                string? winnerJson=null;
                client.BeforeTransaction=async()=> {
                    client.BeforeTransaction=null;
                    var old=await Read("OasisHolons",child.Id.ToString());
                    var winner=OasisJson.DeserializeStored<Holon>(old["data_json"].S);
                    winner.Name="owned independent race winner";winner.PreviousVersionId=winner.VersionId;winner.VersionId=Guid.NewGuid();winner.Version++;
                    CosmicConcurrency.SetProviderRevisionId(winner,authority,Guid.NewGuid());
                    winnerJson=OasisJson.Serialize(winner);var item=new Dictionary<string,AttributeValue>(old){["data_json"]=new(){S=winnerJson}};
                    var ack=await verifier.PutItemAsync(new PutItemRequest{TableName="OasisHolons",Item=item,
                        ConditionExpression="#json=:expected",ExpressionAttributeNames=new(){{"#json","data_json"}},
                        ExpressionAttributeValues=new(){{":expected",new(){S=old["data_json"].S}}},ReturnValues=ReturnValue.ALL_OLD});
                    Assert.AreEqual(HttpStatusCode.OK,ack.HttpStatusCode);Assert.AreEqual(old["data_json"].S,ack.Attributes["data_json"].S);
                };
                current.Name="must not partially commit";
                var rejected=await provider.ExecuteConditionalHolonWriteAsync(Request(current,1,false));
                Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",rejected.ErrorCode,rejected.Exception?.ToString());
                Assert.IsFalse(rejected.Exception is INativeMutationReconciliationRequired);Assert.AreEqual(2,client.Transactions);
                Assert.AreEqual(originalRoot,(await Read("OasisHolons",root.Id.ToString()))["data_json"].S);
                Assert.IsNotNull(winnerJson);Assert.AreEqual(winnerJson,(await Read("OasisHolons",child.Id.ToString()))["data_json"].S);
            }
            Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",authority+":"+operations[1].ToString("N"))).Count);
            foreach(var id in ids) {
                var item=await Read("OasisHolons",id.ToString());
                var expected=OasisJson.DeserializeStored<Holon>(item["data_json"].S);
                Assert.AreEqual(id,expected.Id);
                Assert.AreEqual(concurrentSameOperation||id==root.Id?1:2,expected.Version);
                Assert.AreEqual(id==root.Id?"owned atomic race root":concurrentSameOperation?"owned atomic race child":"owned independent race winner",expected.Name);
                await RemoveExact("OasisHolons",id.ToString(),"data_json",item["data_json"].S);
            }
            var receiptKey=authority+":"+operations[0].ToString("N");var receipt=await Read("OasisHolonWriteReceipts",receiptKey);
            var acknowledged=ConditionalHolonReceipt.Read(receipt["result_json"].S);
            Assert.AreEqual(root.Id,acknowledged.Single().Id);Assert.AreEqual(child.Id,acknowledged.Single().Children.Single().Id);
            await RemoveExact("OasisHolonWriteReceipts",receiptKey,"result_json",receipt["result_json"].S);
            File.Delete(journal);
        } finally {field.SetValue(provider,original);await provider.DeActivateProviderAsync();}

        async Task RemoveExact(string table,string id,string fieldName,string json) {
            var ack=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},
                ConditionExpression="#json=:expected",ExpressionAttributeNames=new(){{"#json",fieldName}},
                ExpressionAttributeValues=new(){{":expected",new(){S=json}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,ack.HttpStatusCode);Assert.AreEqual(json,ack.Attributes[fieldName].S);
            Assert.AreEqual(0,(await Read(table,id)).Count);
        }
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeGraphReceiptReplayRevisionsAndDeletes(bool aws,bool loseAck)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"atomic-graph-{aws}-{loseAck}.pending.json");
        Assert.IsFalse(File.Exists(journal));
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;
        using var client=new NativeClient();using var verifier=new NativeClient();
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        var root=new Holon{Id=Guid.NewGuid(),Name="owned atomic root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned atomic child",IsActive=true};
        root.Children.Add(child);root.MetaData["exact"]=0.1234567890123456789012345678m;
        var operations=Enumerable.Range(0,5).Select(_=>Guid.NewGuid()).ToArray();
        var ids=new[]{root.Id,child.Id};
        async Task<Dictionary<string,AttributeValue>> Read(string table,string id) {
            var response=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response.Item;
        }
        ConditionalHolonWriteRequest Request(Holon current,int operation,ConditionalHolonOperation kind,bool initial=false)
            =>new(new ConditionalHolonWrite(operations[operation],Guid.Empty,authority,
                new[]{current}.Concat(current.Children).Select(row=>new HolonWriteCondition(row.Id,initial?null:CosmicConcurrency.GetProviderRevisionId(row,authority)))),
                kind,new[]{current});
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var id in ids)Assert.AreEqual(0,(await Read("OasisHolons",id.ToString())).Count);
            foreach(var op in operations)Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",authority+":"+op.ToString("N"))).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,operations,authority=authority.ToString(),state="before-atomic-native-write"}));file.Flush(true);
            }
            field.SetValue(provider,client);client.LoseAck=loseAck;
            var request=Request(root,0,ConditionalHolonOperation.Save,true);
            var first=await provider.ExecuteConditionalHolonWriteAsync(request);
            var originalOutcome=first;
            if(loseAck) {
                Assert.AreEqual("COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED",first.ErrorCode);
                Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(first.Exception);
                Assert.AreEqual(1,client.Transactions);
                first=await provider.ExecuteConditionalHolonWriteAsync(request);
            }
            Assert.IsFalse(first.IsError,first.Exception?.ToString());
            var current=(Holon)first.Result.Single();Assert.AreEqual(1,current.Children.Single().Version);
            var initialVersion=current.VersionId;
            var replay=await provider.ExecuteConditionalHolonWriteAsync(request);
            var acknowledgements=new List<(ConditionalHolonOperation Kind,OASISResult<IEnumerable<IHolon>> Outcome)>{(ConditionalHolonOperation.Save,first),(ConditionalHolonOperation.Save,replay)};
            Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(initialVersion,replay.Result.Single().VersionId);
            Assert.AreEqual(1,client.Transactions);
            var stored=await Read("OasisHolons",root.Id.ToString());
            using(var canonical=JsonDocument.Parse(stored["data_json"].S))
                Assert.AreEqual(0.1234567890123456789012345678m,canonical.RootElement.GetProperty("MetaData").GetProperty("exact").GetDecimal());
            root.Name="changed request";
            var reused=await provider.ExecuteConditionalHolonWriteAsync(Request(root,0,ConditionalHolonOperation.Save,true));
            Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",reused.ErrorCode);Assert.AreEqual(1,client.Transactions);
            current.Name="owned atomic replacement";
            var stale=await provider.ExecuteConditionalHolonWriteAsync(Request(root,1,ConditionalHolonOperation.Save,true));
            Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",stale.ErrorCode);Assert.AreEqual(1,client.Transactions);
            for(var index=1;index<4;index++) {
                var kind=index==1?ConditionalHolonOperation.Save:index==2?ConditionalHolonOperation.SoftDelete:ConditionalHolonOperation.HardDelete;
                var result=await provider.ExecuteConditionalHolonWriteAsync(Request(current,index,kind));
                acknowledgements.Add((kind,result));
                Assert.IsFalse(result.IsError,result.Exception?.ToString());current=(Holon)result.Result.Single();
                Assert.AreEqual(index+1,current.Version);Assert.AreEqual(index+1,current.Children.Single().Version);
                if(index==2){
                    var before=await Read("OasisHolons",root.Id.ToString());var childBefore=await Read("OasisHolons",child.Id.ToString());
                    var deletion=current.DeletedDate;var active=current.IsActive;var version=current.VersionId;
                    current.IsActive=true;current.DeletedDate=DateTime.MinValue;
                    var rejected=await provider.ExecuteConditionalHolonWriteAsync(Request(current,4,ConditionalHolonOperation.Save));
                    current.IsActive=active;current.DeletedDate=deletion;
                    Assert.IsTrue(rejected.IsError);Assert.AreEqual(Core.Helpers.StorageLifecycleConflictException.ErrorCode,rejected.ErrorCode);
                    Assert.IsFalse(rejected.Exception is INativeMutationReconciliationRequired);Assert.AreEqual(3,client.Transactions);Assert.AreEqual(version,current.VersionId);
                    Assert.AreEqual(before["data_json"].S,(await Read("OasisHolons",root.Id.ToString()))["data_json"].S);
                    Assert.AreEqual(childBefore["data_json"].S,(await Read("OasisHolons",child.Id.ToString()))["data_json"].S);
                }
            }
            Assert.AreEqual(4,client.Transactions);
            foreach(var id in ids)Assert.AreEqual(0,(await Read("OasisHolons",id.ToString())).Count);
            foreach(var op in operations) {
                var key=authority+":"+op.ToString("N");var receipt=await Read("OasisHolonWriteReceipts",key);
                if(op==operations[4]){Assert.AreEqual(0,receipt.Count);continue;}
                Assert.AreEqual(key,receipt["id"].S);
                var ack=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName="OasisHolonWriteReceipts",Key=new(){{"id",new(){S=key}}},
                    ConditionExpression="#json=:expected",ExpressionAttributeNames=new(){{"#json","result_json"}},
                    ExpressionAttributeValues=new(){{":expected",new(){S=receipt["result_json"].S}}},ReturnValues=ReturnValue.ALL_OLD});
                Assert.AreEqual(HttpStatusCode.OK,ack.HttpStatusCode);Assert.AreEqual(receipt["result_json"].S,ack.Attributes["result_json"].S);
                Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",key)).Count);
            }
            File.Delete(journal);
            // Result-only checks follow exact cleanup; receipt replay describes the original graph, not new billable writes.
            if(loseAck){Assert.AreEqual(0,originalOutcome.SavedCount);Assert.AreEqual(0,originalOutcome.DeletedCount);}
            foreach(var acknowledged in acknowledgements){
                Assert.AreEqual(acknowledged.Kind==ConditionalHolonOperation.Save?2:0,acknowledged.Outcome.SavedCount);
                Assert.AreEqual(acknowledged.Kind==ConditionalHolonOperation.Save?0:2,acknowledged.Outcome.DeletedCount);
                Assert.IsTrue(acknowledged.Outcome.HasAnyHolonsChanged);var node=acknowledged.Outcome.Result.Single();
                Assert.IsFalse(node.IsNewHolon);Assert.IsFalse(node.Children.Single().IsNewHolon);
            }
        } finally {
            field.SetValue(provider,original);await provider.DeActivateProviderAsync();
        }
    }
}

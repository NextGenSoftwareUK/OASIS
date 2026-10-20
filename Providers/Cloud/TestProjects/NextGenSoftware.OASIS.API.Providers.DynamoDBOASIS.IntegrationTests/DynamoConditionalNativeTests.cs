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
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class DynamoConditionalNativeTests
{
    private sealed class NativeClient:AmazonDynamoDBClient
    {
        public bool LoseAck;public int Transactions;
        public NativeClient():base(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0}){}
        public override async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken token=default) {
            Transactions++;var result=await base.TransactWriteItemsAsync(request,token);
            if(LoseAck){LoseAck=false;throw new IOException("owned response loss after native commit");}
            return result;
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
        var operations=Enumerable.Range(0,4).Select(_=>Guid.NewGuid()).ToArray();
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
                Assert.IsFalse(result.IsError,result.Exception?.ToString());current=(Holon)result.Result.Single();
                Assert.AreEqual(index+1,current.Version);Assert.AreEqual(index+1,current.Children.Single().Version);
            }
            Assert.AreEqual(4,client.Transactions);
            foreach(var id in ids)Assert.AreEqual(0,(await Read("OasisHolons",id.ToString())).Count);
            foreach(var op in operations) {
                var key=authority+":"+op.ToString("N");var receipt=await Read("OasisHolonWriteReceipts",key);
                Assert.AreEqual(key,receipt["id"].S);
                var ack=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName="OasisHolonWriteReceipts",Key=new(){{"id",new(){S=key}}},
                    ConditionExpression="#json=:expected",ExpressionAttributeNames=new(){{"#json","result_json"}},
                    ExpressionAttributeValues=new(){{":expected",new(){S=receipt["result_json"].S}}},ReturnValues=ReturnValue.ALL_OLD});
                Assert.AreEqual(HttpStatusCode.OK,ack.HttpStatusCode);Assert.AreEqual(receipt["result_json"].S,ack.Attributes["result_json"].S);
                Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",key)).Count);
            }
            File.Delete(journal);
        } finally {
            field.SetValue(provider,original);await provider.DeActivateProviderAsync();
        }
    }
}

using System.Net;
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
public class DynamoBatchRevisionNativeTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeOrdinaryBatchPublishesTokensForConditionalFollowOn(bool aws,bool synchronous)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        Assert.AreEqual(0,Directory.GetFiles(work,"*.pending.json").Length);
        var journal=Path.Combine(work,$"batch-revision-{aws}-{synchronous}.pending.json");
        var authority=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):
            new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;
        using var verifier=new AmazonDynamoDBClient(new BasicAWSCredentials("ownedhistorytest","ownedhistorytest"),
            new AmazonDynamoDBConfig{ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",MaxErrorRetry=0});
        var root=new Holon{Id=Guid.NewGuid(),Name="owned ordinary batch root",IsActive=true};
        var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id,Name="owned ordinary batch child",IsActive=true};root.Children.Add(child);
        var operation=Guid.NewGuid();var actor=Guid.NewGuid();var rows=new[]{root,child};
        async Task<Dictionary<string,AttributeValue>> Read(string table,string id) {
            var response=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response.Item;
        }
        var receiptKey=authority+":"+operation.ToString("N");
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var row in rows)Assert.AreEqual(0,(await Read("OasisHolons",row.Id.ToString())).Count);
            Assert.AreEqual(0,(await Read("OasisHolonWriteReceipts",receiptKey)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)) {
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id).ToArray(),operation,actor,authority=authority.ToString(),synchronous,state="before-native-batch-revision"}));file.Flush(true);
            }
            var saved=synchronous?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
            Assert.IsFalse(saved.IsError,saved.Exception?.ToString());var revisions=new Dictionary<Guid,Guid?>();
            foreach(var row in rows) {
                var revision=CosmicConcurrency.GetProviderRevisionId(row,authority);Assert.IsTrue(revision.HasValue&&revision!=Guid.Empty);revisions.Add(row.Id,revision);
                var stored=await Read("OasisHolons",row.Id.ToString());var canonical=OasisJson.DeserializeStored<Holon>(stored["data_json"].S);
                Assert.AreEqual(revision,CosmicConcurrency.GetProviderRevisionId(canonical,authority));
            }
            root.Name="owned conditional follow-on";
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,authority,
                rows.Select(row=>new HolonWriteCondition(row.Id,revisions[row.Id]))),ConditionalHolonOperation.Save,new[]{root});
            var update=await provider.ExecuteConditionalHolonWriteAsync(request);Assert.IsFalse(update.IsError,update.Exception?.ToString());
            foreach(var row in new[]{update.Result.Single(),update.Result.Single().Children.Single()})
                Assert.AreNotEqual(revisions[row.Id],CosmicConcurrency.GetProviderRevisionId(row,authority));
            foreach(var row in rows)await Remove("OasisHolons",row.Id.ToString(),"data_json");
            var receipt=await Read("OasisHolonWriteReceipts",receiptKey);
            Assert.AreEqual(root.Id,ConditionalHolonReceipt.Read(receipt["result_json"].S).Single().Id);
            await Remove("OasisHolonWriteReceipts",receiptKey,"result_json");File.Delete(journal);
        } finally {await provider.DeActivateProviderAsync();}
        async Task Remove(string table,string id,string field) {
            var stored=await Read(table,id);Assert.IsTrue(stored.ContainsKey(field));var json=stored[field].S;
            var response=await verifier.DeleteItemAsync(new DeleteItemRequest{TableName=table,Key=new(){{"id",new(){S=id}}},
                ConditionExpression="#payload=:expected",ExpressionAttributeNames=new(){{"#payload",field}},
                ExpressionAttributeValues=new(){{":expected",new(){S=json}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);Assert.AreEqual(json,response.Attributes[field].S);
            Assert.AreEqual(0,(await Read(table,id)).Count);
        }
    }
}

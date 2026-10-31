using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeOrdinaryBatchRetainsKnownRootCountAndStopsAfterUnknownChild(bool aws,bool synchronous)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,"ordinary-batch-ack.pending.json");Assert.IsFalse(File.Exists(journal));
        var root=new Holon{Id=Guid.NewGuid()};var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=root.Id};var leaf=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};root.Children.Add(child);child.Children.Add(leaf);
        var rows=new[]{root,child,leaf};var marker="owned-batch-ack-"+root.Id.ToString("N");foreach(var row in rows){row.Name=marker;row.MetaData["owned_ack"]=marker;}
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"):new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        async Task<GetItemResponse> Read(Guid id){var response=await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response;}
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);foreach(var row in rows)Assert.AreEqual(0,(await Read(row.Id)).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids=rows.Select(row=>row.Id).ToArray(),marker,aws,synchronous,state="before-owned-batch-ack"}));file.Flush(true);
            }
            client.BeforePut=()=>{client.BeforePut=()=>{client.Mode="lost";return Task.CompletedTask;};return Task.CompletedTask;};
            var result=synchronous?provider.SaveHolons(new[]{root}):await provider.SaveHolonsAsync(new[]{root});
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsInstanceOfType<DynamoDBPartialMutationException>(result.Exception);Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
            var partial=(DynamoDBPartialMutationException)result.Exception;Assert.AreEqual(root.Id,partial.AcknowledgedRecordIds.Single());Assert.IsInstanceOfType<DynamoDBMutationReconciliationException>(partial.InnerException);
            Assert.AreEqual(child.Id.ToString(),((DynamoDBMutationReconciliationException)partial.InnerException!).RecordKey);Assert.AreEqual(2,client.Writes);Assert.AreEqual(0,client.Transactions);
            Assert.AreEqual(1,root.Version);Assert.AreEqual(0,child.Version);Assert.AreEqual(0,leaf.Version);Assert.AreEqual(0,(await Read(leaf.Id)).Item.Count);
            foreach(var row in new[]{root,child}){
                var json=(await Read(row.Id)).Item["data_json"].S;var record=JObject.Parse(json);Assert.AreEqual(row.Id,Guid.Parse(record["Id"]!.Value<string>()!));Assert.AreEqual(marker,record["MetaData"]!["owned_ack"]!.Value<string>());Assert.AreEqual(1,record["Version"]!.Value<int>());
                await RemoveOwned(verifier,"OasisHolons",row.Id,json);Assert.AreEqual(0,(await Read(row.Id)).Item.Count);
            }
            File.Delete(journal);
            // Two actual writes were inspected, but only the first was acknowledged to this operation; never use this as a fee counter.
            Assert.AreEqual(1,result.SavedCount);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}

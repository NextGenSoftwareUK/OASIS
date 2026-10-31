using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.ProviderTestSupport;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeHistoricalEmbeddedVisibilityKeepsEachStoredVersion(bool aws,bool sync)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"embedded-export-{aws}-{sync}.pending.json");Assert.IsFalse(File.Exists(journal));
        var owner=Guid.NewGuid();var id=Guid.NewGuid();var allowed=Guid.NewGuid();var foreign=Guid.NewGuid();
        // Validate the exact persisted legacy format through the real restricted decoder
        // before admitting any fixture write, including interface child type metadata.
        var history=new JArray();
        foreach(var version in new[]{1,2}){
            var root=new Holon{Id=id,CreatedByAvatarId=owner,Version=version,Name="owned Dynamo embedded history v"+version};
            root.Children.Add(new Holon{Id=allowed,ParentHolonId=id,CreatedByAvatarId=owner,Version=version==1?4:9});
            var hidden=new Holon{Id=foreign,ParentHolonId=id,CreatedByAvatarId=Guid.NewGuid(),IsPublic=true};
            hidden.Children.Add(new Holon{Id=Guid.NewGuid(),ParentHolonId=foreign,CreatedByAvatarId=owner});root.Children.Add(hidden);
            var payload=AllVersionExportVisibilityAssertions.Legacy(root);
            Assert.AreEqual(2,OasisJson.DeserializeStored<Holon>(payload.ToString(Newtonsoft.Json.Formatting.None)).Children.Count);
            history.Add(payload);
        }
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000")
            :new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        using var verifier=Client();
        async Task<Dictionary<string,AttributeValue>> Read(){
            var response=await verifier.GetItemAsync(new GetItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,response.HttpStatusCode);return response.Item;
        }
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.AreEqual(0,(await Read()).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,owner,allowed,foreign,aws,sync,state="before-owned-typed-embedded-export"}));file.Flush(true);}
            var current=new Holon{Id=id,CreatedByAvatarId=Guid.NewGuid(),Version=3,Name="owned Dynamo embedded current"};
            var saved=await provider.SaveHolonAsync(current,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.AreEqual(3,current.Version);
            var original=await Read();var document=OasisJson.ParseStoredObject(original["data_json"].S);
            document[EmbeddedRecordHistory.PropertyName]=new JObject{{"schema",1},{"records",history}};
            var injected=document.ToString(Newtonsoft.Json.Formatting.None);
            var acknowledged=await verifier.UpdateItemAsync(new UpdateItemRequest{TableName="OasisHolons",Key=new(){{"id",new(){S=id.ToString()}}},
                UpdateExpression="SET #payload=:next",ConditionExpression="#payload=:old",ExpressionAttributeNames=new(){{"#payload","data_json"}},
                ExpressionAttributeValues=new(){{":old",new(){S=original["data_json"].S}},{":next",new(){S=injected}}},ReturnValues=ReturnValue.ALL_OLD});
            Assert.AreEqual(HttpStatusCode.OK,acknowledged.HttpStatusCode);Assert.AreEqual(original["data_json"].S,acknowledged.Attributes["data_json"].S);
            var observed=await Read();Assert.AreEqual(injected,observed["data_json"].S);
            var exported=sync?provider.ExportAllDataForAvatarById(owner,-1):await provider.ExportAllDataForAvatarByIdAsync(owner,-1);
            Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.AreEqual(2,exported.LoadedCount);
            var roots=exported.Result.Where(row=>row.Id==id).OrderBy(row=>row.Version).ToArray();Assert.AreEqual(2,roots.Length);
            for(var index=0;index<2;index++){
                CollectionAssert.AreEqual(new[]{allowed},roots[index].Children.Select(row=>row.Id).ToArray());Assert.AreEqual(index==0?4:9,roots[index].Children.Single().Version);
                ((Holon)roots[index]).AcceptChanges();var rejected=await provider.SaveHolonAsync(roots[index]);
                Assert.IsTrue(rejected.IsError);Assert.IsFalse(rejected.IsSaved);Assert.IsInstanceOfType<InvalidOperationException>(rejected.Exception);
            }
            var unscoped=await provider.ExportAllAsync(-1);Assert.IsFalse(unscoped.IsError,unscoped.Exception?.ToString());
            Assert.IsTrue(unscoped.Result.Where(row=>row.Id==id&&row.Version<3).All(row=>row.Children.Count()==2));
            Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(observed),JToken.FromObject(await Read())),"Projection and rejected saves cannot mutate native attributes/history.");
            await RemoveOwned(verifier,"OasisHolons",id,injected);Assert.AreEqual(0,(await Read()).Count);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}

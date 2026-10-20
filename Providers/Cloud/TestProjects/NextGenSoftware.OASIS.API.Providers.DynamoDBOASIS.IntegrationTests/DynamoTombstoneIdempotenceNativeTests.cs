using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    public static IEnumerable<object[]> TombstoneCases(){foreach(var paired in new[]{false,true})foreach(var sync in new[]{false,true})foreach(var mode in paired?new[]{0,1,2,3}:new[]{0,1,2})yield return new object[]{paired,sync,mode};}
    [DataTestMethod][DynamicData(nameof(TombstoneCases),DynamicDataSourceType.Method)]
    public async Task NativeMatchingTombstonesPreserveRevisionsAndValidateConditions(bool paired,bool sync,int mode)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var id=Guid.NewGuid();var name="owned-tombstone-"+id.ToString("N");
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"tombstone-{paired}-{sync}-{mode}.pending.json");Assert.IsFalse(File.Exists(journal));
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"){IsVersionControlEnabled=true};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        async Task<Dictionary<string,AttributeValue>> Read(string table)=>(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true})).Item;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})Assert.AreEqual(0,(await Read(table)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,paired,sync,mode,state="before-owned-tombstone"}));file.Flush(true);}
            await SavePublic(provider,new Avatar{Id=id,Username=name,IsActive=true});await SavePublic(provider,new AvatarDetail{Id=id,Username=name,IsActive=true,XP=456,Karma=123});
            if(mode==3){
                var item=await Read("OasisAvatars");var expected=item["data_json"].S;var json=JObject.Parse(expected);json["IsActive"]=false;json["DeletedDate"]=DateTime.UtcNow;
                item["data_json"]=new(){S=json.ToString(Newtonsoft.Json.Formatting.None)};item["is_deleted"]=new(){BOOL=true};
                Assert.AreEqual(HttpStatusCode.OK,(await verifier.PutItemAsync(new PutItemRequest{TableName="OasisAvatars",Item=item,ConditionExpression="data_json = :expected",ExpressionAttributeValues=new(){{":expected",new(){S=expected}}}})).HttpStatusCode);
            }else{var first=paired?await provider.DeleteAvatarAsync(id):await provider.DeleteAvatarDetailAsync(id);Assert.IsFalse(first.IsError,first.Exception?.ToString());}
            var profile=(await Read("OasisAvatars"))["data_json"].S;var detail=(await Read("OasisAvatarDetails"))["data_json"].S;string? winner=null;
            if(mode==1)client.BeforeTransaction=async()=>{
                var item=await Read("OasisAvatarDetails");var json=JObject.Parse(item["data_json"].S);json["XP"]=999;winner=json.ToString(Newtonsoft.Json.Formatting.None);item["data_json"]=new(){S=winner};
                Assert.AreEqual(HttpStatusCode.OK,(await verifier.PutItemAsync(new PutItemRequest{TableName="OasisAvatarDetails",Item=item,ConditionExpression="data_json = :expected",ExpressionAttributeValues=new(){{":expected",new(){S=detail}}}})).HttpStatusCode);
            };
            client.Transactions=0;client.LoseTransactionReturn=mode==2;
            var result=paired?sync?provider.DeleteAvatar(id):await provider.DeleteAvatarAsync(id):sync?provider.DeleteAvatarDetail(id):await provider.DeleteAvatarDetailAsync(id);
            Assert.AreEqual(mode is 1 or 2,result.IsError,result.Exception?.ToString());Assert.AreEqual(mode is 0 or 3,result.Result);Assert.AreEqual(1,client.Transactions);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));
            if(mode==1){Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsInstanceOfType<TransactionCanceledException>(result.Exception);Assert.IsNotNull(winner);}
            if(mode==2)Assert.AreSame(client.Lost,result.Exception);
            var actualProfile=(await Read("OasisAvatars"))["data_json"].S;var actualDetail=(await Read("OasisAvatarDetails"))["data_json"].S;
            Assert.AreEqual(profile,actualProfile);
            if(mode==3){var updated=JObject.Parse(actualDetail);Assert.IsFalse(updated["IsActive"]!.Value<bool>());Assert.AreEqual(JObject.Parse(profile)["DeletedDate"]!.Value<DateTime>(),updated["DeletedDate"]!.Value<DateTime>());Assert.AreEqual(456L,updated["XP"]!.Value<long>());Assert.AreEqual(2,updated["Version"]!.Value<int>());var archived=updated[EmbeddedRecordHistory.PropertyName]!["records"]!;Assert.AreEqual(1,archived.Count());Assert.IsTrue(archived[0]!["IsActive"]!.Value<bool>());Assert.AreEqual(456L,archived[0]!["XP"]!.Value<long>());}
            else Assert.AreEqual(mode==1?winner:detail,actualDetail);
            await RemoveOwned(verifier,"OasisAvatarDetails",id,actualDetail);await RemoveOwned(verifier,"OasisAvatars",id,actualProfile);File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}

using System.Net;
using System.Reflection;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeLifecycleOversizedHistoryIsKnownRejectionNotLostAcknowledgement(bool paired)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"detail-validation-{paired}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-validation-"+id.ToString("N");
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"){IsVersionControlEnabled=true};
        using var verifier=Client();var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        async Task<GetItemResponse> Read(string table)=>await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true});
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})Assert.AreEqual(0,(await Read(table)).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,paired,state="before-owned-native-validation"}));file.Flush(true);}
            await SavePublic(provider,new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});
            await SavePublic(provider,new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",Description=new string('x',230*1024),IsActive=true});
            var avatar=(await Read("OasisAvatars")).Item["data_json"].S;var detail=(await Read("OasisAvatarDetails")).Item["data_json"].S;
            var result=paired?await provider.DeleteAvatarAsync(id):await provider.DeleteAvatarDetailAsync(id);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
            Assert.IsInstanceOfType<Amazon.DynamoDBv2.AmazonDynamoDBException>(result.Exception);
            var cause=(Amazon.DynamoDBv2.AmazonDynamoDBException)result.Exception;
            Assert.AreEqual(HttpStatusCode.BadRequest,cause.StatusCode);Assert.AreEqual("ValidationException",cause.ErrorCode);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(cause));
            Assert.AreEqual(avatar,(await Read("OasisAvatars")).Item["data_json"].S);Assert.AreEqual(detail,(await Read("OasisAvatarDetails")).Item["data_json"].S);
            await RemoveOwned(verifier,"OasisAvatarDetails",id,detail);await RemoveOwned(verifier,"OasisAvatars",id,avatar);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)]
    public async Task StandaloneDetailNativeLifecyclePreservesAvatarAndOriginalCondition(bool soft,int mode)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"detail-native-{soft}-{mode}.pending.json");
        Assert.IsFalse(File.Exists(journal));var id=Guid.NewGuid();var name="owned-detail-"+id.ToString("N");
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"){IsVersionControlEnabled=true};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var original=(AmazonDynamoDBClient)field.GetValue(provider)!;using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        async Task<GetItemResponse> Read(string table)=>await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=key,ConsistentRead=true});
        try {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})Assert.AreEqual(0,(await Read(table)).Item.Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,soft,mode,state="before-owned-standalone-detail"}));file.Flush(true);}
            await SavePublic(provider,new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});
            await SavePublic(provider,new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});
            var avatarJson=(await Read("OasisAvatars")).Item["data_json"].S;
            string? racedJson=null;
            if(mode==3)client.BeforeTransaction=async()=>{
                var item=(await Read("OasisAvatarDetails")).Item;var json=JObject.Parse(item["data_json"].S);json["Username"]=name+"-raced";
                racedJson=json.ToString(Newtonsoft.Json.Formatting.None);item["data_json"]=new(){S=racedJson};item["username"]=new(){S=name+"-raced"};
                Assert.AreEqual(HttpStatusCode.OK,(await verifier.PutItemAsync(new PutItemRequest{TableName="OasisAvatarDetails",Item=item})).HttpStatusCode);
            };
            client.LoseTransactionReturn=mode==4;
            IOASISStorageProvider storage=provider;
            var result=mode==1?storage.DeleteAvatarDetailByUsername(name,soft):mode==2?await storage.DeleteAvatarDetailByEmailAsync(name+"@example.invalid",soft):await storage.DeleteAvatarDetailAsync(id,soft);
            Assert.AreEqual(mode>=3,result.IsError,result.Exception?.ToString());Assert.AreEqual(mode<3,result.Result);
            Assert.AreEqual(1,client.Transactions);Assert.AreEqual(avatarJson,(await Read("OasisAvatars")).Item["data_json"].S);
            var actual=await Read("OasisAvatarDetails");Assert.AreEqual(HttpStatusCode.OK,actual.HttpStatusCode);
            if(mode==3){Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.AreEqual(racedJson,actual.Item["data_json"].S);}
            else {
                if(mode==4){Assert.IsInstanceOfType<DynamoDBMutationReconciliationException>(result.Exception);Assert.AreEqual(client.TransactionToken,((DynamoDBMutationReconciliationException)result.Exception).ClientRequestToken);}
                if(soft){var json=JObject.Parse(actual.Item["data_json"].S);Assert.AreEqual(2,json["Version"]!.Value<int>());Assert.IsTrue(actual.Item["is_deleted"].BOOL);
                    Assert.AreEqual(name,json[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Username"]!.Value<string>());}
                else Assert.AreEqual(0,actual.Item.Count);
            }
            if(actual.Item.Count>0)await RemoveOwned(verifier,"OasisAvatarDetails",id,actual.Item["data_json"].S);
            await RemoveOwned(verifier,"OasisAvatars",id,avatarJson);File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}

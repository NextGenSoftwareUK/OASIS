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
    public static IEnumerable<object[]> IdentityRaceCases(){foreach(var detail in new[]{false,true})foreach(var email in new[]{false,true})foreach(var soft in new[]{false,true})foreach(var sync in new[]{false,true})yield return new object[]{detail,email,soft,sync};}
    [DataTestMethod][DynamicData(nameof(IdentityRaceCases),DynamicDataSourceType.Method)]
    public async Task NativeIdentityDeletionRefusesReassignedCanonicalRecord(bool detail,bool email,bool soft,bool sync)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var id=Guid.NewGuid();var name="owned-identity-race-"+id.ToString("N");var table=detail?"OasisAvatarDetails":"OasisAvatars";
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"identity-race-{detail}-{email}-{soft}-{sync}.pending.json");Assert.IsFalse(File.Exists(journal));
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"){IsVersionControlEnabled=true};
        var field=typeof(DynamoDBOASIS).GetField("_client",BindingFlags.Instance|BindingFlags.NonPublic)!;var original=(AmazonDynamoDBClient)field.GetValue(provider)!;
        using var client=new FaultClient();using var verifier=Client();field.SetValue(provider,client);
        var key=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString()}}};
        async Task<Dictionary<string,AttributeValue>> Read(string target)=>(await verifier.GetItemAsync(new GetItemRequest{TableName=target,Key=key,ConsistentRead=true})).Item;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var target in new[]{"OasisAvatars","OasisAvatarDetails"})Assert.AreEqual(0,(await Read(target)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,detail,email,soft,sync,state="before-owned-identity-race"}));file.Flush(true);}
            await SavePublic(provider,new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});
            await SavePublic(provider,new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true,XP=456,Karma=123});
            var profileBefore=(await Read("OasisAvatars"))["data_json"].S;var detailBefore=(await Read("OasisAvatarDetails"))["data_json"].S;string? winner=null;
            client.AfterScan=async()=>{
                var item=await Read(table);var expected=item["data_json"].S;var json=JObject.Parse(expected);json["Username"]=name+"-new";json["Email"]=name+"-new@example.invalid";
                winner=json.ToString(Newtonsoft.Json.Formatting.None);item["data_json"]=new(){S=winner};item["username"]=new(){S=name+"-new"};item["email"]=new(){S=name+"-new@example.invalid"};
                Assert.AreEqual(HttpStatusCode.OK,(await verifier.PutItemAsync(new PutItemRequest{TableName=table,Item=item,ConditionExpression="data_json = :expected",ExpressionAttributeValues=new(){{":expected",new(){S=expected}}}})).HttpStatusCode);
            };
            var result=detail?email?sync?provider.DeleteAvatarDetailByEmail(name+"@example.invalid",soft):await provider.DeleteAvatarDetailByEmailAsync(name+"@example.invalid",soft)
                :sync?provider.DeleteAvatarDetailByUsername(name,soft):await provider.DeleteAvatarDetailByUsernameAsync(name,soft)
                :email?sync?provider.DeleteAvatarByEmail(name+"@example.invalid",soft):await provider.DeleteAvatarByEmailAsync(name+"@example.invalid",soft)
                :sync?provider.DeleteAvatarByUsername(name,soft):await provider.DeleteAvatarByUsernameAsync(name,soft);
            Assert.IsNotNull(winner);Assert.IsTrue(result.IsError,result.Exception?.ToString());Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
            Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(0,client.Transactions);
            var profileAfter=(await Read("OasisAvatars"))["data_json"].S;var detailAfter=(await Read("OasisAvatarDetails"))["data_json"].S;
            Assert.AreEqual(detail?profileBefore:winner,profileAfter);Assert.AreEqual(detail?winner:detailBefore,detailAfter);
            await RemoveOwned(verifier,"OasisAvatarDetails",id,detailAfter);await RemoveOwned(verifier,"OasisAvatars",id,profileAfter);File.Delete(journal);
        }finally{field.SetValue(provider,original);await provider.DeActivateProviderAsync();}
    }
}

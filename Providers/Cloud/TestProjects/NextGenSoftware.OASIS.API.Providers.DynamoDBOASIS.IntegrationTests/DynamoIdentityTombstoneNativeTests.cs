using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    public static IEnumerable<object[]> IdentityTombstoneCases(){foreach(var detail in new[]{false,true})foreach(var email in new[]{false,true})foreach(var sync in new[]{false,true})foreach(var ambiguous in new[]{false,true})yield return new object[]{detail,email,sync,ambiguous};}
    [DataTestMethod][DynamicData(nameof(IdentityTombstoneCases),DynamicDataSourceType.Method)]
    public async Task NativeLifecycleIdentityResolvesTombstoneButRefusesAmbiguity(bool detail,bool email,bool sync,bool ambiguous)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var ids=ambiguous?new[]{Guid.NewGuid(),Guid.NewGuid()}:new[]{Guid.NewGuid()};var name="owned-tombstone-selector-"+ids[0].ToString("N");
        var journal=Path.Combine(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!,$"identity-tombstone-{detail}-{email}-{sync}-{ambiguous}.pending.json");Assert.IsFalse(File.Exists(journal));
        var provider=new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000"){IsVersionControlEnabled=true};using var verifier=Client();
        async Task<Dictionary<string,AttributeValue>> Read(string table,Guid id)=>(await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true})).Item;
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var id in ids)foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})Assert.AreEqual(0,(await Read(table,id)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,detail,email,sync,ambiguous,state="before-owned-identity-tombstone"}));file.Flush(true);}
            foreach(var id in ids){await SavePublic(provider,new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true});await SavePublic(provider,new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true,XP=456,Karma=123});}
            var removed=detail?await provider.DeleteAvatarDetailAsync(ids[0]):await provider.DeleteAvatarAsync(ids[0]);Assert.IsFalse(removed.IsError,removed.Exception?.ToString());
            if(detail){var selected=email?await provider.LoadAvatarDetailByEmailAsync(name+"@example.invalid"):await provider.LoadAvatarDetailByUsernameAsync(name);Assert.AreEqual(!ambiguous,selected.IsError);if(ambiguous)Assert.AreEqual(ids[1],selected.Result.Id);var historical=await provider.LoadAvatarDetailAsync(ids[0],1);Assert.IsFalse(historical.IsError);Assert.IsTrue(historical.Result.IsActive);}
            else{var selected=email?await provider.LoadAvatarByEmailAsync(name+"@example.invalid"):await provider.LoadAvatarByUsernameAsync(name);Assert.AreEqual(!ambiguous,selected.IsError);if(ambiguous)Assert.AreEqual(ids[1],selected.Result.Id);var historical=await provider.LoadAvatarAsync(ids[0],1);Assert.IsFalse(historical.IsError);Assert.IsTrue(historical.Result.IsActive);}
            var before=new Dictionary<(string,Guid),string>();foreach(var id in ids)foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"})before[(table,id)]=(await Read(table,id))["data_json"].S;
            var result=detail?email?sync?provider.DeleteAvatarDetailByEmail(name+"@example.invalid",false):await provider.DeleteAvatarDetailByEmailAsync(name+"@example.invalid",false)
                :sync?provider.DeleteAvatarDetailByUsername(name,false):await provider.DeleteAvatarDetailByUsernameAsync(name,false)
                :email?sync?provider.DeleteAvatarByEmail(name+"@example.invalid",false):await provider.DeleteAvatarByEmailAsync(name+"@example.invalid",false)
                :sync?provider.DeleteAvatarByUsername(name,false):await provider.DeleteAvatarByUsernameAsync(name,false);
            Assert.AreEqual(ambiguous,result.IsError,result.Exception?.ToString());Assert.AreEqual(!ambiguous,result.Result);
            if(ambiguous){Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsInstanceOfType<StorageIdentityAmbiguousException>(result.Exception);}
            foreach(var id in ids)foreach(var table in new[]{"OasisAvatars","OasisAvatarDetails"}){
                var actual=await Read(table,id);
                if(ambiguous||detail&&table=="OasisAvatars"){Assert.AreEqual(before[(table,id)],actual["data_json"].S);await RemoveOwned(verifier,table,id,before[(table,id)]);}
                else Assert.AreEqual(0,actual.Count);
            }
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}

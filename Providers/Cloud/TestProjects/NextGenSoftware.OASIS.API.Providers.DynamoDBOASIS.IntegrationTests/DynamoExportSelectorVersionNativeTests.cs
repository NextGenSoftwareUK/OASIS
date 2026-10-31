using System.Net;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

public partial class DynamoHistoryNativeTests
{
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task NativeIndependentExportVersionsPreserveCurrentIdentityAndOwnedRecords(bool aws,bool sync,bool email)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var work=Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!;
        var journal=Path.Combine(work,$"independent-export-{aws}-{sync}-{email}.pending.json");Assert.IsFalse(File.Exists(journal));
        var owner=Guid.NewGuid();var own=Guid.NewGuid();var foreign=Guid.NewGuid();var name="owned-export-"+owner.ToString("N");
        DynamoDBOASIS provider=aws?new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","ownedhistorytest","ownedhistorytest","http://127.0.0.1:18000")
            :new DynamoDBOASIS("ownedhistorytest","ownedhistorytest","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;using var verifier=Client();
        var rows=new[]{(Table:"OasisAvatars",Id:owner),(Table:"OasisHolons",Id:own),(Table:"OasisHolons",Id:foreign)};
        async Task<Dictionary<string,AttributeValue>> Read(string table,Guid id){
            var result=await verifier.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new(){S=id.ToString()}}},ConsistentRead=true});
            Assert.AreEqual(HttpStatusCode.OK,result.HttpStatusCode);return result.Item;
        }
        try{
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var row in rows)Assert.AreEqual(0,(await Read(row.Table,row.Id)).Count);
            using(var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{owner,own,foreign,name,aws,sync,email,state="before-owned-independent-export"}));file.Flush(true);
            }
            var avatar=new Avatar{Id=owner,Username=name,Email=name+"@example.invalid",IsActive=true};
            await SavePublic(provider,avatar);avatar.Username=name+"-current";avatar.Email=avatar.Username+"@example.invalid";
            await SavePublic(provider,avatar);Assert.AreEqual(2,avatar.Version);
            var holon=new Holon{Id=own,CreatedByAvatarId=owner,IsActive=true};var other=new Holon{Id=foreign,CreatedByAvatarId=Guid.NewGuid(),IsActive=true};
            for(var version=1;version<=3;version++){
                holon.Name=name+"-v"+version;other.Name=name+"-foreign-v"+version;
                await SavePublic(provider,holon);await SavePublic(provider,other);
            }
            var originals=new List<Dictionary<string,AttributeValue>>();foreach(var row in rows)originals.Add(await Read(row.Table,row.Id));
            foreach(var version in new[]{0,1,2,3}){
                var exported=email
                    ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)
                    :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,version):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,version);
                Assert.IsFalse(exported.IsError,exported.Exception?.ToString());Assert.IsTrue(exported.IsLoaded);Assert.AreEqual(1,exported.LoadedCount);
                var expected=version==0?3:version;
                Assert.AreEqual(own,exported.Result.Single().Id);Assert.AreEqual(expected,exported.Result.Single().Version);Assert.AreEqual(name+"-v"+expected,exported.Result.Single().Name);
            }
            var allVersions=email
                ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,-1):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,-1)
                :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,-1):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,-1);
            Assert.IsFalse(allVersions.IsError,allVersions.Exception?.ToString());Assert.IsTrue(allVersions.IsLoaded);Assert.AreEqual(3,allVersions.LoadedCount);
            var versions=allVersions.Result.ToArray();CollectionAssert.AreEqual(new[]{1,2,3},versions.Select(row=>row.Version).ToArray());
            Assert.IsTrue(versions.All(row=>row.Id==own&&row.CreatedByAvatarId==owner));
            for(var index=0;index<versions.Length;index++)Assert.AreEqual(name+"-v"+(index+1),versions[index].Name);
            var oldAvatar=email?await provider.LoadAvatarByEmailAsync(name+"@example.invalid",1):await provider.LoadAvatarByUsernameAsync(name,1);
            Assert.IsFalse(oldAvatar.IsError);Assert.AreEqual(owner,oldAvatar.Result.Id);Assert.AreEqual(1,oldAvatar.Result.Version);
            var oldExport=email?await provider.ExportAllDataForAvatarByEmailAsync(name+"@example.invalid",1):await provider.ExportAllDataForAvatarByUsernameAsync(name,1);
            Assert.IsTrue(oldExport.IsError);Assert.IsNull(oldExport.Result);
            for(var index=0;index<rows.Length;index++)Assert.IsTrue(JToken.DeepEquals(JToken.FromObject(originals[index]),JToken.FromObject(await Read(rows[index].Table,rows[index].Id))),"Export must not alter native columns or canonical bytes/history.");
            for(var index=0;index<rows.Length;index++)await RemoveOwned(verifier,rows[index].Table,rows[index].Id,originals[index]["data_json"].S);
            File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}

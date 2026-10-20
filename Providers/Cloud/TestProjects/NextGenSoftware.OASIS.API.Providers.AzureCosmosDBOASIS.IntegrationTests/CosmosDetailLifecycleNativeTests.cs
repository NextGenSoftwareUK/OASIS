using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    public async Task NativeStandaloneDetailDeletionPreservesAvatarAndRetainsSoftHistory(bool soft,int selector)
    {
        var id=Guid.NewGuid();var name="owned-detail-"+id.ToString("N");var partition=new PartitionKey(id.ToString());
        var path=$"C:/Source/OASIS/TestResults/CosmosDBCurrent/detail-{soft}-{selector}.pending.json";Assert.IsFalse(File.Exists(path));
        var profile=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarItems"));var details=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarDetailItems"));
        Assert.IsNull(await profile.ReadDocumentAsync(id.ToString(),partition));Assert.IsNull(await details.ReadDocumentAsync(id.ToString(),partition));
        using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,soft,selector,state="before-owned-cosmos-detail-lifecycle"}));stream.Flush(true);}
        _provider.IsVersionControlEnabled=true;
        var avatar=new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true};
        Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=avatar.Email,IsActive=true,XP=321,Karma=29})).IsError);
        var originalAvatar=await profile.ReadDocumentAsync(id.ToString(),partition);
        IOASISStorageProvider storage=_provider;
        var result=selector==1?storage.DeleteAvatarDetailByUsername(name,soft):selector==2?await storage.DeleteAvatarDetailByEmailAsync(avatar.Email,soft):await storage.DeleteAvatarDetailAsync(id,soft);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(result.IsDeleted);
        Assert.AreEqual(originalAvatar,await profile.ReadDocumentAsync(id.ToString(),partition));
        var json=await details.ReadDocumentAsync(id.ToString(),partition);
        if(soft){Assert.IsNotNull(json);var document=JObject.Parse(json);Assert.AreEqual(2,document["Version"]!.Value<int>());
            var current=OasisJson.DeserializeStored<AvatarDetail>(CosmosEmbeddedHistory.CanonicalPayload(document).ToString());Assert.IsTrue(current.IsDeleted);Assert.IsFalse(current.IsActive);
            Assert.AreEqual(name,document[CosmosEmbeddedHistory.PropertyName]!["records"]![0]!["Username"]!.Value<string>());
            var hard=await storage.DeleteAvatarDetailAsync(id,false);Assert.IsFalse(hard.IsError,hard.Exception?.ToString());
        }else Assert.IsNull(json);
        Assert.IsNull(await details.ReadDocumentAsync(id.ToString(),partition));
        await profile.DeleteUnchangedDocumentAsync(id.ToString(),avatar,partition);Assert.IsNull(await profile.ReadDocumentAsync(id.ToString(),partition));File.Delete(path);
    }
}

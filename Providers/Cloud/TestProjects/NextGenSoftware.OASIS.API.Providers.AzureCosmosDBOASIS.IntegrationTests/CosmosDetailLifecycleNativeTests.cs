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
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeAvatarSelectorsRepeatAndPurgePairedTombstones(bool sync,bool email)
    {
        var id=Guid.NewGuid();var name="owned-profile-selector-"+id.ToString("N");var address=name+"@example.invalid";
        var partition=new PartitionKey(id.ToString());
        var path=$"C:/Source/OASIS/TestResults/CosmosDBCurrent/profile-selector-{sync}-{email}.pending.json";
        var profiles=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarItems"));
        var details=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarDetailItems"));
        Assert.IsFalse(File.Exists(path));Assert.IsNull(await profiles.ReadDocumentAsync(id.ToString(),partition));
        Assert.IsNull(await details.ReadDocumentAsync(id.ToString(),partition));
        using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,name,sync,email,state="before-owned-paired-selector-lifecycle"}));stream.Flush(true);}
        _provider.IsVersionControlEnabled=true;
        Assert.IsFalse((await _provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=address,IsActive=true})).IsError);
        Assert.IsFalse((await _provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=address,IsActive=true,XP=321})).IsError);
        IOASISStorageProvider storage=_provider;
        Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> Delete(bool soft)=>email
            ?(sync?Task.FromResult(storage.DeleteAvatarByEmail(address.ToUpperInvariant(),soft)):storage.DeleteAvatarByEmailAsync(address.ToUpperInvariant(),soft))
            :(sync?Task.FromResult(storage.DeleteAvatarByUsername(name.ToUpperInvariant(),soft)):storage.DeleteAvatarByUsernameAsync(name.ToUpperInvariant(),soft));
        var first=await Delete(true);Assert.IsFalse(first.IsError,first.Message+first.Exception);Assert.IsTrue(first.IsDeleted);
        var profile=await profiles.ReadDocumentAsync(id.ToString(),partition);
        var detail=await details.ReadDocumentAsync(id.ToString(),partition);
        Assert.IsNotNull(profile);Assert.IsNotNull(detail);
        Assert.AreEqual(2,JObject.Parse(profile)["Version"]!.Value<int>());
        Assert.AreEqual(2,JObject.Parse(detail)["Version"]!.Value<int>());
        Assert.IsTrue((await storage.LoadAvatarAsync(id)).IsError);Assert.IsTrue((await storage.LoadAvatarDetailAsync(id)).IsError);
        var repeated=await Delete(true);Assert.IsFalse(repeated.IsError,repeated.Message+repeated.Exception);
        Assert.IsFalse(repeated.IsSaved);Assert.IsTrue(repeated.IsDeleted);
        Assert.AreEqual(profile,await profiles.ReadDocumentAsync(id.ToString(),partition));
        Assert.AreEqual(detail,await details.ReadDocumentAsync(id.ToString(),partition));
        var purged=await Delete(false);Assert.IsFalse(purged.IsError,purged.Message+purged.Exception);Assert.IsTrue(purged.IsDeleted);
        Assert.IsNull(await profiles.ReadDocumentAsync(id.ToString(),partition));Assert.IsNull(await details.ReadDocumentAsync(id.ToString(),partition));
        File.Delete(path);
    }

    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task NativeRepositoryDeleteOverloadsUseConditionalOwnerAndRejectStaleEntity(int kind)
    {
        var id=Guid.NewGuid();var source=new AvatarDetail{Id=id,Username="owned-repository-"+id.ToString("N"),IsActive=true};
        var partition=new PartitionKey(id.ToString());var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarDetailItems"));
        var path=$"C:/Source/OASIS/TestResults/CosmosDBCurrent/repository-delete-{kind}.pending.json";Assert.IsFalse(File.Exists(path));Assert.IsNull(await native.ReadDocumentAsync(id.ToString(),partition));
        using(var stream=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{id,kind,state="before-owned-repository-delete"}));stream.Flush(true);}
        _provider.IsVersionControlEnabled=true;var saved=await _provider.SaveAvatarDetailAsync(source);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());
        var repository=(Interfaces.IAvatarDetailRepository)typeof(AzureCosmosDBOASIS).GetField("avatarDetailRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(_provider)!;
        if(kind==0)await repository.DeleteAsync(source);
        else if(kind==1)await repository.DeleteAsync(id);
        else if(kind==2)await repository.DeleteAsync(id.ToString());
        else {
            var changed=OasisJson.Project<AvatarDetail>(source);changed.Username+="-changed";
            var update=await _provider.SaveAvatarDetailAsync(changed);Assert.IsFalse(update.IsError,update.Exception?.ToString());
            var expected=await native.ReadDocumentAsync(id.ToString(),partition);
            await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(()=>repository.DeleteAsync(source));
            Assert.AreEqual(expected,await native.ReadDocumentAsync(id.ToString(),partition));
            await repository.DeleteAsync(await repository.GetByIdAsync(id.ToString()));
        }
        Assert.IsNull(await native.ReadDocumentAsync(id.ToString(),partition));File.Delete(path);
    }

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
            var repeated=selector==1?storage.DeleteAvatarDetailByUsername(name,true)
                :selector==2?await storage.DeleteAvatarDetailByEmailAsync(avatar.Email,true):await storage.DeleteAvatarDetailAsync(id,true);
            Assert.IsFalse(repeated.IsError,repeated.Message+repeated.Exception);Assert.IsTrue(repeated.IsDeleted);Assert.IsFalse(repeated.IsSaved);
            Assert.AreEqual(json,await details.ReadDocumentAsync(id.ToString(),partition),"Repeat soft deletion must preserve the original payload/history/native revision.");
            Assert.IsTrue((await storage.LoadAvatarDetailAsync(id)).IsError,"Ordinary reads must still hide the tombstone.");
            var hard=selector==1?storage.DeleteAvatarDetailByUsername(name,false)
                :selector==2?await storage.DeleteAvatarDetailByEmailAsync(avatar.Email,false):await storage.DeleteAvatarDetailAsync(id,false);
            Assert.IsFalse(hard.IsError,hard.Message+hard.Exception);
        }else Assert.IsNull(json);
        Assert.IsNull(await details.ReadDocumentAsync(id.ToString(),partition));
        await profile.DeleteUnchangedDocumentAsync(id.ToString(),avatar,partition);Assert.IsNull(await profile.ReadDocumentAsync(id.ToString(),partition));File.Delete(path);
    }
}

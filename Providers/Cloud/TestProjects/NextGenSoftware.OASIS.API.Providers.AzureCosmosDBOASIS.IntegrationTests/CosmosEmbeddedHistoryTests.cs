using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [DataTestMethod][DataRow("avatar")][DataRow("detail")][DataRow("holon")]
    public async Task NativePublicWritesRetainHistoryAfterDisablementAndPublishAcknowledgedVersion(string kind)
    {
        var id=Guid.NewGuid();
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase source=kind=="avatar" ? new Avatar{Username="owned-history-"+id}
            : kind=="detail" ? new AvatarDetail{XP=456} : new Holon();
        source.Id=id;source.IsActive=true;source.MetaData=new Dictionary<string,object>{{"history_marker","first"}};
        _provider.IsVersionControlEnabled=true;
        async Task Save()
        {
            if(kind=="avatar") {var r=await _provider.SaveAvatarAsync((NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar)source);Assert.IsFalse(r.IsError,r.Exception?.ToString());}
            else if(kind=="detail") {var r=await _provider.SaveAvatarDetailAsync((NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail)source);Assert.IsFalse(r.IsError,r.Exception?.ToString());}
            else {var r=await _provider.SaveHolonAsync((NextGenSoftware.OASIS.API.Core.Interfaces.IHolon)source,saveChildren:false);Assert.IsFalse(r.IsError,r.Exception?.ToString());}
        }
        await Save();var firstVersion=source.Version;var firstIdentity=source.VersionId;
        _provider.IsVersionControlEnabled=false;
        source.MetaData["history_marker"]="second";
        await Save();var secondVersion=source.Version;var secondIdentity=source.VersionId;
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase old;
        if(kind=="avatar") {var r=await _provider.LoadAvatarAsync(id,1);Assert.IsFalse(r.IsError,r.Exception?.ToString());old=r.Result;}
        else if(kind=="detail") {var r=await _provider.LoadAvatarDetailAsync(id,1);Assert.IsFalse(r.IsError,r.Exception?.ToString());old=r.Result;}
        else {var r=await _provider.LoadHolonAsync(id,loadChildren:false,version:1);Assert.IsFalse(r.IsError,r.Exception?.ToString());old=r.Result;}
        if(kind=="avatar") {
            var deleted=await _provider.DeleteAvatarAsync(id,softDelete:true);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());
            var archived=await _provider.LoadAvatarAsync(id,1);Assert.IsFalse(archived.IsError,archived.Exception?.ToString());
            Assert.IsTrue((await _provider.LoadAvatarAsync(id)).IsError);
        }
        var native=new CosmosDbClient(_client.GetContainer(DatabaseName,kind=="avatar"?"avatarItems":kind=="detail"?"avatarDetailItems":"holonItems"));
        var stored=JObject.Parse(await native.ReadDocumentAsync(id.ToString(),new PartitionKey(id.ToString())));
        Assert.AreEqual(id.ToString(),stored["id"]!.Value<string>());
        Assert.AreEqual("second",stored["MetaData"]!["history_marker"]!.Value<string>());
        await native.DeleteDocumentAsync(id.ToString(),new PartitionKey(id.ToString()));
        Assert.AreEqual(1,firstVersion);Assert.AreEqual(2,secondVersion);Assert.AreNotEqual(Guid.Empty,firstIdentity);
        Assert.AreEqual(firstIdentity,old.VersionId);Assert.AreEqual("first",old.MetaData["history_marker"].ToString());
        Assert.AreEqual(kind=="avatar"?3:2,stored["Version"]!.Value<int>());
        Assert.AreEqual(kind=="avatar"?secondIdentity:firstIdentity,Guid.Parse(stored["PreviousVersionId"]!.Value<string>()!));
    }

    [TestMethod]
    public async Task NativeSingleItemHistoryPersistsCurrentAndPriorVersionsThroughOfficialSdk()
    {
        var owner=Guid.NewGuid();
        var source=new Holon{Id=Guid.NewGuid(),IsActive=true,CreatedByAvatarId=owner,ParentHolonId=owner,
            Name="owned-atomic-history-"+Guid.NewGuid().ToString("N"),MetaData=new Dictionary<string,object>{{"history_marker","first"}}};
        var originalName=source.Name;
        var partition=new PartitionKey(source.Id.ToString());
        var writer=new CosmosDbClient(_client.GetContainer(DatabaseName,"holonItems"));
        var first=JObject.Parse(await writer.UpsertVersionedDocumentAsync(source,DecodeHistoryHolon,partition));
        Assert.AreEqual(1,first["Version"]!.Value<int>());
        source.Name=originalName+"-updated";
        source.MetaData["history_marker"]="second";
        var second=JObject.Parse(await writer.UpsertVersionedDocumentAsync(source,DecodeHistoryHolon,partition));
        Assert.AreEqual(2,second["Version"]!.Value<int>());
        var independent=new CosmosDbClient(_client.GetContainer(DatabaseName,"holonItems"));
        var stored=JObject.Parse(await independent.ReadDocumentAsync(source.Id.ToString(),partition));
        Assert.AreEqual(source.Id.ToString(),stored["id"]!.Value<string>());
        Assert.AreEqual(source.Name,stored["Name"]!.Value<string>());
        var old=await CosmosEmbeddedHistory.ReadAsync(source.Id,stored,1,DecodeHistoryHolon);
        var current=await CosmosEmbeddedHistory.ReadAsync(source.Id,stored,0,DecodeHistoryHolon);
        var byId=await _provider.LoadHolonAsync(source.Id,loadChildren:false,version:1);
        var byKey=_provider.LoadHolon(source.Id.ToString(),loadChildren:false,version:1);
        var all=await _provider.LoadAllHolonsAsync(loadChildren:false,version:1);
        var metadata=await _provider.LoadHolonsByMetaDataAsync("history_marker","first",loadChildren:false,version:1);
        var parent=await _provider.LoadHolonsForParentAsync(owner,loadChildren:false,version:1);
        var exported=await _provider.ExportAllDataForAvatarByIdAsync(owner,1);
        var searched=await _provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false},loadChildren:false,version:1);
        // Only this acknowledged+independently inspected exact fixture is removed.
        await independent.DeleteDocumentAsync(source.Id.ToString(),partition);
        Assert.AreEqual(originalName,old.Name);Assert.AreEqual(source.Name,current.Name);
        Assert.AreEqual(old.VersionId,current.PreviousVersionId);Assert.AreNotEqual(Guid.Empty,old.VersionId);
        Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
        Assert.IsFalse(byId.IsError,byId.Exception?.ToString());Assert.IsFalse(byKey.IsError,byKey.Exception?.ToString());
        Assert.AreEqual(originalName,byId.Result.Name);Assert.AreEqual(originalName,byKey.Result.Name);
        foreach(var selected in new[]{all,metadata,parent,exported}) {
            Assert.IsFalse(selected.IsError,selected.Exception?.ToString());Assert.AreEqual(originalName,selected.Result.Single().Name);
        }
        Assert.IsFalse(searched.IsError,searched.Exception?.ToString());Assert.AreEqual(originalName,searched.Result.SearchResultHolons.Single().Name);
    }
    private static Holon DecodeHistoryHolon(string json)=>OasisJson.DeserializeStored<Holon>(json);
}

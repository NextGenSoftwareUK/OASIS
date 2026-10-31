using Azure.Storage.Blobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task PublicProfileAndDetailHistorySelectorsAndDeletionPreserveArchives(bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        var id=Guid.NewGuid();
        var profile=new Avatar {Id=id,Username="owned-history-first",Email="first@example.invalid",IsActive=true};
        var detail=new AvatarDetail {Id=id,Username=profile.Username,Email=profile.Email,XP=123,Karma=45,IsActive=true};
        var savedProfile=synchronous ? _provider.SaveAvatar(profile):await _provider.SaveAvatarAsync(profile);
        var savedDetail=synchronous ? _provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(savedProfile.IsError,savedProfile.Message);Assert.IsFalse(savedDetail.IsError,savedDetail.Message);
        Assert.AreEqual(1,profile.Version);Assert.AreEqual(1,detail.Version);
        var originalProfileVersion=profile.VersionId;var originalDetailVersion=detail.VersionId;
        profile.Username="owned-history-second";profile.Email="second@example.invalid";
        profile.VerificationToken="owned-history-verification";profile.ResetToken="owned-history-reset";
        profile.RefreshTokens=new() {new Core.Objects.RefreshToken {Token="owned-history-refresh"}};
        detail.Username=profile.Username;detail.Email=profile.Email;detail.XP=987;
        _provider.IsVersionControlEnabled=false; // Existing archives must not disappear.
        Assert.IsFalse((synchronous ? _provider.SaveAvatar(profile):await _provider.SaveAvatarAsync(profile)).IsError);
        Assert.IsFalse((synchronous ? _provider.SaveAvatarDetail(detail):await _provider.SaveAvatarDetailAsync(detail)).IsError);
        Assert.AreEqual(2,profile.Version);Assert.AreEqual(2,detail.Version);
        Assert.AreEqual(originalProfileVersion,profile.PreviousVersionId);Assert.AreEqual(originalDetailVersion,detail.PreviousVersionId);
        var oldProfile=synchronous ? _provider.LoadAvatar(id,1):await _provider.LoadAvatarAsync(id,1);
        var oldDetail=synchronous ? _provider.LoadAvatarDetail(id,1):await _provider.LoadAvatarDetailAsync(id,1);
        Assert.IsFalse(oldProfile.IsError,oldProfile.Message);Assert.IsFalse(oldDetail.IsError,oldDetail.Message);
        Assert.AreEqual("owned-history-first",oldProfile.Result.Username);Assert.AreEqual(123,oldDetail.Result.XP);
        Assert.AreEqual(id,(await _provider.LoadAvatarByUsernameAsync("owned-history-first",1)).Result.Id);
        Assert.AreEqual(id,(await _provider.LoadAvatarByEmailAsync("FIRST@example.invalid",1)).Result.Id);
        Assert.AreEqual(id,(await _provider.LoadAvatarDetailByUsernameAsync("owned-history-first",1)).Result.Id);
        Assert.AreEqual(id,(await _provider.LoadAvatarDetailByEmailAsync("FIRST@example.invalid",1)).Result.Id);
        Assert.AreEqual(id,(await _provider.LoadAvatarByVerificationTokenAsync("owned-history-verification",2)).Result.Id);
        Assert.AreEqual(id,(await _provider.LoadAvatarByResetTokenAsync("owned-history-reset",2)).Result.Id);
        Assert.AreEqual(id,(await _provider.LoadAvatarByRefreshTokenAsync("owned-history-refresh",2)).Result.Id);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await _provider.LoadAvatarAsync(id,99)).ErrorCode);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await _provider.LoadAvatarDetailAsync(id,99)).ErrorCode);
        Assert.IsFalse((await _provider.LoadAllAvatarsAsync(99)).Result.Any());
        Assert.IsFalse((await _provider.LoadAllAvatarDetailsAsync(99)).Result.Any());
        var deleted=synchronous ? _provider.DeleteAvatar(id,true):await _provider.DeleteAvatarAsync(id,true);
        Assert.IsFalse(deleted.IsError,deleted.Message);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await _provider.LoadAvatarAsync(id)).ErrorCode);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await _provider.LoadAvatarDetailAsync(id)).ErrorCode);
        Assert.AreEqual(originalProfileVersion,(await _provider.LoadAvatarAsync(id,1)).Result.VersionId);
        Assert.AreEqual(originalDetailVersion,(await _provider.LoadAvatarDetailAsync(id,1)).Result.VersionId);
        foreach(var index in new[] {1,2}) {
            var blob=_service.GetBlobContainerClient(_containers[index]).GetBlobClient(id.ToString());
            var json=(await blob.DownloadContentAsync()).Value.Content.ToString();
            Assert.IsTrue(Newtonsoft.Json.Linq.JObject.Parse(json)[EmbeddedRecordHistory.PropertyName]!=null);
            Assert.AreEqual(3,OasisJson.DeserializeStored<Avatar>(json).Version);
        }
        Assert.IsFalse((await _provider.DeleteAvatarAsync(id,false)).IsError);
        foreach(var index in new[] {1,2}) Assert.IsFalse((await _service.GetBlobContainerClient(_containers[index]).GetBlobClient(id.ToString()).ExistsAsync()).Value);
    }

    [DataTestMethod] [DataRow(false)] [DataRow(true)]
    public async Task PublicHolonHistoryFiltersRootsButLoadsIndependentLatestChildren(bool synchronous)
    {
        RequireLive();_provider.IsVersionControlEnabled=true;
        var owner=Guid.NewGuid();var parent=Guid.NewGuid();
        var avatar=new Avatar {Id=owner,Username="owned-root-owner",Email="root-owner@example.invalid",IsActive=true};
        Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
        var parentRecord=new Holon {Id=parent,CustomKey="owned-parent-key",Name="owned-parent"};
        Assert.IsFalse((await _provider.SaveHolonAsync(parentRecord,false)).IsError);
        var child=new Holon {Id=Guid.NewGuid(),Name="owned-child-first",CreatedByAvatarId=owner};
        var root=new Holon {Id=Guid.NewGuid(),Name="owned-root-first",ParentHolonId=parent,CreatedByAvatarId=owner,
            MetaData=new() {{"history-marker","first"}},Children=new List<IHolon> {child}};
        var first=synchronous ? _provider.SaveHolon(root):await _provider.SaveHolonAsync(root);
        Assert.IsFalse(first.IsError,first.Message);Assert.AreEqual(1,root.Version);Assert.AreEqual(1,child.Version);
        root.Name="owned-root-second";root.MetaData["history-marker"]="second";
        Assert.IsFalse((await _provider.SaveHolonAsync(root,false)).IsError);
        child.Name="owned-child-third";
        Assert.IsFalse((await _provider.SaveHolonAsync(child,false)).IsError);
        Assert.IsFalse((await _provider.SaveHolonAsync(child,false)).IsError);
        Assert.AreEqual(2,root.Version);Assert.AreEqual(3,child.Version);
        var loaded=synchronous ? _provider.LoadHolon(root.Id,loadChildrenFromProvider:true,version:1)
            :await _provider.LoadHolonAsync(root.Id,loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual("owned-root-first",loaded.Result.Name);
        Assert.AreEqual(1,loaded.Result.Version);Assert.AreEqual(3,loaded.Result.Children.Single().Version);
        Assert.AreEqual("owned-child-third",loaded.Result.Children.Single().Name);
        var all=await _provider.LoadAllHolonsAsync(loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(all.IsError,all.Message);
        Assert.AreEqual(3,all.Result.Single(row=>row.Id==root.Id).Children.Single().Version);
        var roots=await _provider.LoadHolonsForParentAsync(parent,loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(roots.IsError,roots.Message);Assert.AreEqual(root.Id,roots.Result.Single().Id);
        Assert.AreEqual(3,roots.Result.Single().Children.Single().Version);
        var keyedRoots=await _provider.LoadHolonsForParentAsync("owned-parent-key",loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(keyedRoots.IsError,keyedRoots.Message);Assert.AreEqual(root.Id,keyedRoots.Result.Single().Id);
        Assert.AreEqual(3,keyedRoots.Result.Single().Children.Single().Version);
        var metadata=await _provider.LoadHolonsByMetaDataAsync("history-marker","first",loadChildrenFromProvider:true,version:1);
        Assert.IsFalse(metadata.IsError,metadata.Message);Assert.AreEqual(root.Id,metadata.Result.Single().Id);
        var search=await _provider.SearchAsync(new SearchParams {SearchOnlyForCurrentAvatar=false,FilterByMetaData=new() {{"history-marker","first"}}},version:1);
        Assert.IsFalse(search.IsError,search.Message);Assert.AreEqual(root.Id,search.Result.SearchResultHolons.Single().Id);
        var exported=await _provider.ExportAllDataForAvatarByIdAsync(owner,1);
        Assert.IsFalse(exported.IsError,exported.Message);Assert.AreEqual(2,exported.Result.Count());
        Assert.AreEqual(2,(await _provider.ExportAllDataForAvatarByUsernameAsync("OWNED-ROOT-OWNER",1)).Result.Count());
        Assert.AreEqual(2,(await _provider.ExportAllDataForAvatarByEmailAsync("ROOT-OWNER@example.invalid",1)).Result.Count());
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await _provider.LoadHolonAsync(root.Id,version:99)).ErrorCode);
        Assert.IsFalse((await _provider.LoadAllHolonsAsync(version:99)).Result.Any());
        Assert.IsFalse((await _provider.DeleteHolonAsync(child.Id)).IsError);
        Assert.IsFalse((await _provider.DeleteHolonAsync(root.Id)).IsError);
        Assert.IsFalse((await _provider.DeleteHolonAsync("owned-parent-key")).IsError);
        Assert.IsFalse((await _provider.DeleteAvatarAsync(owner,false)).IsError);
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await _provider.LoadHolonAsync(root.Id,version:1)).ErrorCode);
    }
}

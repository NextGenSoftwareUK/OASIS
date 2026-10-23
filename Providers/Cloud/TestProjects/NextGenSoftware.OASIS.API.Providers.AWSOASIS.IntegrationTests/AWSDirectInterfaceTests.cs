using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeDirectInterfaceSelectorsImportSearchExportAndDeletion(bool synchronous)
    {
        // The suite-level durable empty-baseline intent owns these synthetic rows.
        // Any failure preserves them; only verified full-suite success permits cleanup.
        var storage=(IOASISStorageProvider)_provider;
        var marker="owned-interface-"+Guid.NewGuid().ToString("N");
        var avatar=new Avatar {Id=Guid.NewGuid(),Username=marker,Email=marker+"@example.invalid",FirstName="complete profile",IsActive=true};
        var detail=new AvatarDetail {Id=avatar.Id,Username=avatar.Username,Email=avatar.Email,XP=4321,Karma=123,IsActive=true};
        var saved=synchronous?storage.SaveAvatar(avatar):await storage.SaveAvatarAsync(avatar);
        Assert.IsFalse(saved.IsError,saved.Message);
        var savedDetail=synchronous?storage.SaveAvatarDetail(detail):await storage.SaveAvatarDetailAsync(detail);
        Assert.IsFalse(savedDetail.IsError,savedDetail.Message);

        var byKey=synchronous?storage.LoadAvatarByProviderKey(avatar.Id.ToString()):await storage.LoadAvatarByProviderKeyAsync(avatar.Id.ToString());
        Assert.IsFalse(byKey.IsError,byKey.Message);Assert.IsTrue(byKey.IsLoaded);
        Assert.AreEqual(1,byKey.LoadedCount);
        Assert.AreEqual(avatar.Id,byKey.Result.Id);Assert.AreEqual("complete profile",byKey.Result.FirstName);
        var absentKey=synchronous?storage.LoadAvatarByProviderKey(Guid.NewGuid().ToString()):await storage.LoadAvatarByProviderKeyAsync(Guid.NewGuid().ToString());
        Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,absentKey.ErrorCode);Assert.IsNull(absentKey.Result);
        Assert.AreEqual(0,absentKey.LoadedCount);Assert.IsFalse(absentKey.IsLoaded);
        var allProfiles=synchronous?storage.LoadAllAvatars():await storage.LoadAllAvatarsAsync();
        var allDetails=synchronous?storage.LoadAllAvatarDetails():await storage.LoadAllAvatarDetailsAsync();
        Assert.IsFalse(allProfiles.IsError,allProfiles.Message);Assert.IsFalse(allDetails.IsError,allDetails.Message);
        Assert.AreEqual(allProfiles.Result.Count(),allProfiles.LoadedCount);Assert.AreEqual(allDetails.Result.Count(),allDetails.LoadedCount);
        Assert.AreEqual(avatar.Username,allProfiles.Result.Single(row=>row.Id==avatar.Id).Username);
        Assert.AreEqual(4321L,allDetails.Result.Single(row=>row.Id==avatar.Id).XP);
        var selectedDetails=new[] {
            synchronous?storage.LoadAvatarDetailByEmail(avatar.Email):await storage.LoadAvatarDetailByEmailAsync(avatar.Email),
            synchronous?storage.LoadAvatarDetailByUsername(avatar.Username):await storage.LoadAvatarDetailByUsernameAsync(avatar.Username)};
        foreach(var selected in selectedDetails)
        {
            Assert.IsFalse(selected.IsError,selected.Message);Assert.IsTrue(selected.IsLoaded);
            Assert.AreEqual(1,selected.LoadedCount);
            Assert.AreEqual(avatar.Id,selected.Result.Id);Assert.AreEqual(4321L,selected.Result.XP);Assert.AreEqual(123L,selected.Result.Karma);
        }

        var rows=new IHolon[] {new Holon {Id=Guid.NewGuid(),Name=marker+"-one"},new Holon {Id=Guid.NewGuid(),Name=marker+"-two"}};
        var imported=synchronous?storage.Import(rows):await storage.ImportAsync(rows);
        Assert.IsFalse(imported.IsError,imported.Message);Assert.IsTrue(imported.Result);Assert.IsTrue(imported.IsSaved);
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=[new SearchTextGroup {SearchQuery=marker}]};
        var searched=synchronous?storage.Search(query,loadChildren:false):await storage.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(searched.IsError,searched.Message);
        Assert.AreEqual(searched.Result.NumberOfResults,searched.LoadedCount);
        CollectionAssert.AreEquivalent(rows.Select(row=>row.Id).ToArray(),searched.Result.SearchResultHolons.Select(row=>row.Id).ToArray());
        var exported=synchronous?storage.ExportAll():await storage.ExportAllAsync();
        Assert.IsFalse(exported.IsError,exported.Message);
        Assert.AreEqual(exported.Result.Count(),exported.LoadedCount);
        foreach(var row in rows)Assert.AreEqual(row.Name,exported.Result.Single(value=>value.Id==row.Id).Name);

        foreach(var email in new[] {false,true})foreach(var soft in new[] {false,true})
        {
            var id=Guid.NewGuid();var name=marker+"-delete-"+id.ToString("N");
            var profile=new Avatar {Id=id,Username=name,Email=name+"@example.invalid",FirstName="preserved profile",IsActive=true};
            var companion=new AvatarDetail {Id=id,Username=name,Email=profile.Email,XP=765,IsActive=true};
            var profileSaved=synchronous?storage.SaveAvatar(profile):await storage.SaveAvatarAsync(profile);
            var detailSaved=synchronous?storage.SaveAvatarDetail(companion):await storage.SaveAvatarDetailAsync(companion);
            Assert.IsFalse(profileSaved.IsError,profileSaved.Message);Assert.IsFalse(detailSaved.IsError,detailSaved.Message);
            var deleted=email
                ?synchronous?storage.DeleteAvatarByEmail(profile.Email,soft):await storage.DeleteAvatarByEmailAsync(profile.Email,soft)
                :synchronous?storage.DeleteAvatarByUsername(name,soft):await storage.DeleteAvatarByUsernameAsync(name,soft);
            Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.Result);Assert.IsTrue(deleted.IsDeleted);
            if(soft)
            {
                var committed=await ReadPair(id);
                Assert.IsTrue(committed.Profile.IsDeleted);Assert.IsFalse(committed.Profile.IsActive);
                Assert.IsTrue(committed.Detail.IsDeleted);Assert.IsFalse(committed.Detail.IsActive);
                Assert.AreEqual("preserved profile",committed.Profile.FirstName);Assert.AreEqual(765L,committed.Detail.XP);
            }
            else foreach(var table in new[] {"OasisAvatars","OasisAvatarDetails"})
                Assert.AreEqual(0,(await Client.GetItemAsync(new Amazon.DynamoDBv2.Model.GetItemRequest {TableName=table,Key=Key(id),ConsistentRead=true})).Item.Count);
        }
    }
}

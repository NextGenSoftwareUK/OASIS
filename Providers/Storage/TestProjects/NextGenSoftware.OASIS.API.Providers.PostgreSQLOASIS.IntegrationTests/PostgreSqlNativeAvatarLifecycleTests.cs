using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    private sealed class AvatarFixture:OwnedFixture
    {
        public AvatarFixture(string name,params Guid[] ids):base(name,ids.SelectMany(id=>new[]{("oasis_avatars",id),("oasis_avatar_details",id)})){}
        public async Task Seed(Guid id,string identity,bool exhausted=false,bool includeDetail=true){
            var profile=new Avatar{Id=id,Username=identity,Email=identity+"@example.invalid",FirstName="retained-profile",IsActive=true,Version=1,MetaData=new(){["owned-fixture"]=Marker,["precision"]=0.1234567890123456789012345678m}};
            var detail=new AvatarDetail{Id=id,Username=identity,Email=identity+"@example.invalid",FirstName="retained-detail",IsActive=true,Version=exhausted?int.MaxValue:1,XP=4321,Karma=77,MetaData=new(){["owned-fixture"]=Marker,["precision"]=0.1234567890123456789012345678m}};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name=Marker+"-inventory"});
            BeforeMutation();var first=await Provider.SaveAvatarAsync(profile);Assert.IsFalse(first.IsError,first.Exception?.ToString());await Observe();
            if(!includeDetail)return;
            BeforeMutation();var second=await Provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(second.IsError,second.Exception?.ToString());await Observe();
        }
    }

    private static Newtonsoft.Json.Linq.JObject WithoutLifecycleFields(string body){
        var payload=new EmbeddedRecordHistory().CanonicalPayload(OasisJson.ParseStoredObject(body));
        foreach(var field in new[]{"IsDeleted","IsActive","DeletedDate","DeletedByAvatarId","Version","VersionId","PreviousVersionId"})payload.Remove(field);
        return payload;
    }

    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)][DataRow(3,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)][DataRow(3,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)][DataRow(3,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)][DataRow(3,true,true)]
    public async Task NativeAvatarDeletionKeepsThePairAndUnrelatedFieldsCoherent(int selector,bool synchronous,bool soft)
    {
        var id=Guid.NewGuid();await using var fixture=new AvatarFixture($"native-avatar-delete-{selector}-{synchronous}-{soft}",id);await fixture.Begin();await fixture.Seed(id,fixture.Marker);
        var beforeProfile=await fixture.Body("oasis_avatars",id);var beforeDetail=await fixture.Body("oasis_avatar_details",id);Assert.IsNotNull(beforeProfile);Assert.IsNotNull(beforeDetail);
        var cachedProfile=await fixture.Provider.LoadAvatarAsync(id);var cachedDetail=await fixture.Provider.LoadAvatarDetailAsync(id);Assert.IsFalse(cachedProfile.IsError,cachedProfile.Message);Assert.IsFalse(cachedDetail.IsError,cachedDetail.Message);
        // A provider key must not silently become a username deletion.
        fixture.BeforeMutation();var invalid=await fixture.Provider.DeleteAvatarAsync(fixture.Marker,soft);
        if(!invalid.IsError)await fixture.Observe(); // Known old-bug success is still an owned outcome.
        Assert.IsTrue(invalid.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);await fixture.Observe();
        Assert.AreEqual(beforeProfile,await fixture.Body("oasis_avatars",id));Assert.AreEqual(beforeDetail,await fixture.Body("oasis_avatar_details",id));
        OASISResult<bool> deleted;fixture.BeforeMutation();
        if(selector==0)deleted=synchronous?fixture.Provider.DeleteAvatar(id,soft):await fixture.Provider.DeleteAvatarAsync(id,soft);
        else if(selector==1)deleted=synchronous?fixture.Provider.DeleteAvatar(id.ToString(),soft):await fixture.Provider.DeleteAvatarAsync(id.ToString(),soft);
        else if(selector==2)deleted=synchronous?fixture.Provider.DeleteAvatarByUsername(" "+fixture.Marker.ToUpperInvariant()+" ",soft):await fixture.Provider.DeleteAvatarByUsernameAsync(" "+fixture.Marker.ToUpperInvariant()+" ",soft);
        else deleted=synchronous?fixture.Provider.DeleteAvatarByEmail(" "+fixture.Marker.ToUpperInvariant()+"@EXAMPLE.INVALID ",soft):await fixture.Provider.DeleteAvatarByEmailAsync(" "+fixture.Marker.ToUpperInvariant()+"@EXAMPLE.INVALID ",soft);
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.Result);await fixture.Observe();Assert.IsTrue(deleted.IsDeleted);Assert.AreEqual(1,deleted.DeletedCount);
        Assert.IsTrue((await fixture.Provider.LoadAvatarAsync(id)).IsError);Assert.IsTrue((await fixture.Provider.LoadAvatarDetailAsync(id)).IsError);
        Assert.IsTrue((await fixture.Provider.LoadAvatarAsync(id,1)).IsError);Assert.IsTrue((await fixture.Provider.LoadAvatarDetailAsync(id,1)).IsError);
        var profiles=await fixture.Provider.LoadAllAvatarsAsync(1);var details=await fixture.Provider.LoadAllAvatarDetailsAsync(1);Assert.IsFalse(profiles.IsError,profiles.Message);Assert.IsFalse(details.IsError,details.Message);Assert.IsFalse(profiles.Result.Any(row=>row.Id==id));Assert.IsFalse(details.Result.Any(row=>row.Id==id));
        if(!soft){Assert.IsNull(await fixture.Body("oasis_avatars",id));Assert.IsNull(await fixture.Body("oasis_avatar_details",id));Assert.IsFalse(AvatarTombstoneEvidence.TryGet(deleted,out _));return;}
        Assert.AreEqual(2,deleted.SavedCount);Assert.IsTrue(AvatarTombstoneEvidence.TryGet(deleted,out var tombstone));Assert.AreEqual(id,tombstone.AvatarId);Assert.AreEqual(DateTimeKind.Utc,tombstone.DeletedAtUtc.Kind);
        var profileBody=await fixture.Body("oasis_avatars",id);var detailBody=await fixture.Body("oasis_avatar_details",id);Assert.IsNotNull(profileBody);Assert.IsNotNull(detailBody);
        Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(WithoutLifecycleFields(beforeProfile),WithoutLifecycleFields(profileBody)),"Unrelated profile fields must not change.");
        Assert.IsTrue(StoredRecordPayloadEquality.Equivalent(WithoutLifecycleFields(beforeDetail),WithoutLifecycleFields(detailBody)),"XP, karma, inventory and every unrelated detail field must not change.");
        var profile=OasisJson.ParseStoredObject(profileBody);var detail=OasisJson.ParseStoredObject(detailBody);
        Assert.AreEqual(profile["DeletedDate"]!.ToString(),detail["DeletedDate"]!.ToString());using var nativeProfile=JsonDocument.Parse(profileBody);Assert.AreEqual(tombstone.DeletedAtUtc,nativeProfile.RootElement.GetProperty("DeletedDate").GetDateTime());
        Assert.AreEqual(2,profile["Version"]!.Value<int>());Assert.AreEqual(2,detail["Version"]!.Value<int>());Assert.AreEqual(1,((Newtonsoft.Json.Linq.JArray)detail[EmbeddedRecordHistory.PropertyName]!["records"]!).Count);
        fixture.BeforeMutation();var staleProfile=await fixture.Provider.SaveAvatarAsync(cachedProfile.Result);Assert.IsTrue(staleProfile.IsError);Assert.IsInstanceOfType<StorageLifecycleConflictException>(staleProfile.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,staleProfile.ErrorCode);Assert.IsFalse(staleProfile.IsSaved);await fixture.Observe();
        fixture.BeforeMutation();var staleDetail=await fixture.Provider.SaveAvatarDetailAsync(cachedDetail.Result);Assert.IsTrue(staleDetail.IsError);Assert.IsInstanceOfType<StorageLifecycleConflictException>(staleDetail.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,staleDetail.ErrorCode);Assert.IsFalse(staleDetail.IsSaved);await fixture.Observe();
        Assert.AreEqual(1,cachedProfile.Result.Version);Assert.AreEqual(1,cachedDetail.Result.Version);Assert.AreEqual(profileBody,await fixture.Body("oasis_avatars",id));Assert.AreEqual(detailBody,await fixture.Body("oasis_avatar_details",id));
        fixture.BeforeMutation();var repeated=await fixture.Provider.DeleteAvatarAsync(id,true);Assert.IsFalse(repeated.IsError,repeated.Exception?.ToString());await fixture.Observe();Assert.AreEqual(0,repeated.SavedCount);
        Assert.AreEqual(profileBody,await fixture.Body("oasis_avatars",id));Assert.AreEqual(detailBody,await fixture.Body("oasis_avatar_details",id));Assert.IsTrue(AvatarTombstoneEvidence.TryGet(repeated,out var repeatEvidence));Assert.AreEqual(tombstone.DeletedAtUtc,repeatEvidence.DeletedAtUtc);
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,true,false)][DataRow(true,false,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(false,true,true)][DataRow(true,false,true)][DataRow(true,true,true)]
    public async Task NativeAmbiguousAvatarDeleteCannotMutateAnyMatchingRecord(bool email,bool synchronous,bool soft)
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};await using var fixture=new AvatarFixture($"native-avatar-ambiguous-{email}-{synchronous}-{soft}",ids);await fixture.Begin();
        foreach(var id in ids)await fixture.Seed(id,fixture.Marker);
        var originals=new Dictionary<(string Table,Guid Id),string>();foreach(var id in ids)foreach(var table in new[]{"oasis_avatars","oasis_avatar_details"})originals[(table,id)]=(await fixture.Body(table,id))!;
        fixture.BeforeMutation();var result=email?(synchronous?fixture.Provider.DeleteAvatarByEmail(fixture.Marker+"@example.invalid",soft):await fixture.Provider.DeleteAvatarByEmailAsync(fixture.Marker+"@example.invalid",soft)):(synchronous?fixture.Provider.DeleteAvatarByUsername(fixture.Marker,soft):await fixture.Provider.DeleteAvatarByUsernameAsync(fixture.Marker,soft));
        Assert.IsTrue(result.IsError);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);await fixture.Observe();
        foreach(var original in originals)Assert.AreEqual(original.Value,await fixture.Body(original.Key.Table,original.Key.Id));
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeFailedDetailTombstoneRollsBackTheProfileToo(bool synchronous)
    {
        var id=Guid.NewGuid();await using var fixture=new AvatarFixture($"native-avatar-rollback-{synchronous}",id);await fixture.Begin();fixture.Provider.IsVersionControlEnabled=false;await fixture.Seed(id,fixture.Marker,exhausted:true);fixture.Provider.IsVersionControlEnabled=true;
        var profile=await fixture.Body("oasis_avatars",id);var detail=await fixture.Body("oasis_avatar_details",id);
        fixture.BeforeMutation();var result=synchronous?fixture.Provider.DeleteAvatar(id,true):await fixture.Provider.DeleteAvatarAsync(id,true);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));await fixture.Observe();
        Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(AvatarTombstoneEvidence.TryGet(result,out _));Assert.AreEqual(profile,await fixture.Body("oasis_avatars",id));Assert.AreEqual(detail,await fixture.Body("oasis_avatar_details",id));
        await using var nativeFlag=new NpgsqlCommand("SELECT is_deleted FROM oasis_avatars WHERE id=@id",fixture.Connection);nativeFlag.Parameters.AddWithValue("id",id);Assert.AreEqual(false,await nativeFlag.ExecuteScalarAsync());
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NativeDeletedProfileCannotAcquireANewActiveDetail(bool synchronous)
    {
        var id=Guid.NewGuid();await using var fixture=new AvatarFixture($"native-avatar-new-detail-{synchronous}",id);await fixture.Begin();await fixture.Seed(id,fixture.Marker,includeDetail:false);
        fixture.BeforeMutation();var deleted=await fixture.Provider.DeleteAvatarAsync(id,true);Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());await fixture.Observe();var profile=await fixture.Body("oasis_avatars",id);Assert.IsNull(await fixture.Body("oasis_avatar_details",id));
        var proposed=new AvatarDetail{Id=id,Username=fixture.Marker,IsActive=true,XP=999,MetaData=new(){["owned-fixture"]=fixture.Marker}};
        fixture.BeforeMutation();var result=synchronous?fixture.Provider.SaveAvatarDetail(proposed):await fixture.Provider.SaveAvatarDetailAsync(proposed);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsFalse(result.IsSaved);await fixture.Observe();
        Assert.AreEqual(0,proposed.Version);Assert.AreEqual(profile,await fixture.Body("oasis_avatars",id));Assert.IsNull(await fixture.Body("oasis_avatar_details",id));
    }
}

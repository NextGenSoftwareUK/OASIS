using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnedNativeAvatarHistoryPreservesSelectorsAndHardDeletesArchive(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Avatar OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid().ToString("N");
            var avatar=new Avatar{Id=Guid.NewGuid(),Username=$"history-v1-{marker}",Email=$"v1-{marker}@example.invalid",FirstName="original"};
            var oldUsername=avatar.Username;var oldEmail=avatar.Email;
            var created=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(created.IsError,created.Message);Assert.AreEqual(1,avatar.Version);
            var originalVersionId=avatar.VersionId;
            avatar.Username=$"history-v2-{marker}";avatar.Email=$"v2-{marker}@example.invalid";avatar.FirstName="updated";
            var updated=synchronous?provider.SaveAvatar(avatar):await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(updated.IsError,updated.Message);Assert.AreEqual(2,avatar.Version);
            Assert.AreEqual(originalVersionId,avatar.PreviousVersionId);
            var old=synchronous?provider.LoadAvatar(avatar.Id,1):await provider.LoadAvatarAsync(avatar.Id,1);
            Assert.IsFalse(old.IsError,old.Message);Assert.AreEqual("original",old.Result.FirstName);
            Assert.AreEqual(originalVersionId,old.Result.VersionId);
            Assert.AreEqual(oldEmail,provider.LoadAvatarByProviderKey(avatar.Id.ToString(),1).Result.Email);
            Assert.AreEqual(avatar.Id,(await provider.LoadAvatarByUsernameAsync(oldUsername,1)).Result.Id);
            Assert.AreEqual(avatar.Id,provider.LoadAvatarByEmail(oldEmail,1).Result.Id);
            Assert.AreEqual(1,(await provider.LoadAllAvatarsAsync(1)).Result.Count());
            Assert.AreEqual("updated",(await provider.LoadAvatarAsync(avatar.Id)).Result.FirstName);
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id,99)).IsError);
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id,-1)).IsError);
            Assert.AreEqual(1L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id}) RETURN count(v) AS count",new{id=avatar.Id.ToString()})).SingleAsync())["count"].As<long>());
            var archiveJson=(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id,Version:1}) RETURN v.SnapshotJson AS snapshot",new{id=avatar.Id.ToString()})).SingleAsync())["snapshot"].As<string>();
            foreach(var bad in new object[]{"not-json","null",42,null!})
            {
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id,Version:1}) SET v.SnapshotJson=$bad",new{id=avatar.Id.ToString(),bad})).ConsumeAsync();
                try
                {
                    var corrupted=await provider.LoadAvatarAsync(avatar.Id,1);
                    Assert.IsTrue(corrupted.IsError);Assert.IsNull(corrupted.Result);
                    Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,corrupted.ErrorCode);
                    Assert.AreEqual("updated",(await provider.LoadAvatarAsync(avatar.Id)).Result.FirstName);
                }
                finally
                {
                    await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Avatar',RecordId:$id,Version:1}) SET v.SnapshotJson=$snapshot",new{id=avatar.Id.ToString(),snapshot=archiveJson})).ConsumeAsync();
                }
            }
            var deleted=synchronous?provider.DeleteAvatar(avatar.Id,false):await provider.DeleteAvatarAsync(avatar.Id,false);
            Assert.IsFalse(deleted.IsError,deleted.Message);
            Assert.IsTrue((await provider.LoadAvatarAsync(avatar.Id,1)).IsError);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=avatar.Id.ToString()})).SingleAsync())["count"].As<long>());
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

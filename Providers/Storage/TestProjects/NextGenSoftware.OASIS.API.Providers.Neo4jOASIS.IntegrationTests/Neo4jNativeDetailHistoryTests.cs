using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnedNativeDetailHistoryKeepsIndependentGameplaySnapshots(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid().ToString("N");
            var profile=new Avatar{Id=Guid.NewGuid(),Username=$"profile-{marker}"};
            Assert.IsFalse((await provider.SaveAvatarAsync(profile)).IsError);
            var detail=new AvatarDetail{Id=profile.Id,Username=$"detail-v1-{marker}",Email=$"v1-{marker}@example.invalid",Karma=10,XP=20};
            detail.Inventory.Add(new InventoryItem{Id=Guid.NewGuid(),Name="history seed",Quantity=1});
            var created=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(created.IsError,created.Message);Assert.AreEqual(1,detail.Version);
            var oldUsername=detail.Username;var oldEmail=detail.Email;var oldVersionId=detail.VersionId;
            detail.Username=$"detail-v2-{marker}";detail.Email=$"v2-{marker}@example.invalid";
            detail.Karma=30;detail.XP=40;detail.Inventory.Single().Quantity=3;
            var updated=synchronous?provider.SaveAvatarDetail(detail):await provider.SaveAvatarDetailAsync(detail);
            Assert.IsFalse(updated.IsError,updated.Message);Assert.AreEqual(2,detail.Version);
            Assert.AreEqual(oldVersionId,detail.PreviousVersionId);
            var old=synchronous?provider.LoadAvatarDetail(detail.Id,1):await provider.LoadAvatarDetailAsync(detail.Id,1);
            Assert.IsFalse(old.IsError,old.Message);Assert.AreEqual(10L,old.Result.Karma);Assert.AreEqual(20L,old.Result.XP);
            Assert.AreEqual(1,old.Result.Inventory.Single().Quantity);
            Assert.AreEqual(oldVersionId,old.Result.VersionId);
            Assert.AreEqual(detail.Id,provider.LoadAvatarDetailByUsername(oldUsername,1).Result.Id);
            Assert.AreEqual(detail.Id,(await provider.LoadAvatarDetailByEmailAsync(oldEmail,1)).Result.Id);
            Assert.AreEqual(1,(await provider.LoadAllAvatarDetailsAsync(1)).Result.Count());
            Assert.AreEqual(1,(await provider.LoadAvatarAsync(profile.Id)).Result.Version);
            Assert.AreEqual(30L,(await provider.LoadAvatarDetailAsync(detail.Id)).Result.Karma);
            Assert.IsTrue((await provider.LoadAvatarDetailAsync(detail.Id,99)).IsError);
            var archive=(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'AvatarDetail',RecordId:$id,Version:1}) RETURN v.SnapshotJson AS snapshot",new{id=detail.Id.ToString()})).SingleAsync())["snapshot"].As<string>();
            await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'AvatarDetail',RecordId:$id,Version:1}) SET v.SnapshotJson='null'",new{id=detail.Id.ToString()})).ConsumeAsync();
            try
            {
                var invalid=await provider.LoadAvatarDetailAsync(detail.Id,1);
                Assert.IsTrue(invalid.IsError);Assert.IsNull(invalid.Result);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,invalid.ErrorCode);
            }
            finally
            {
                await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'AvatarDetail',RecordId:$id,Version:1}) SET v.SnapshotJson=$snapshot",new{id=detail.Id.ToString(),snapshot=archive})).ConsumeAsync();
            }
            Assert.IsFalse((await provider.DeleteAvatarAsync(profile.Id,false)).IsError);
            Assert.IsTrue((await provider.LoadAvatarDetailAsync(detail.Id,1)).IsError);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (v:OasisRecordVersion {RecordId:$id}) RETURN count(v) AS count",new{id=detail.Id.ToString()})).SingleAsync())["count"].As<long>());
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}

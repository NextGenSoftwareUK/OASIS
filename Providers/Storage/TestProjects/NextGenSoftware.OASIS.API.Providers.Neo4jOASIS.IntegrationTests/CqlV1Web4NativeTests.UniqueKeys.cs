using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    /// <summary>M10: alternate keys are claimed natively in the same transaction as the write.</summary>
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionUniqueKeysAreEnforcedNatively(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyUniqueKeyJourney,nativeJourneySuffix:"-unique-keys-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyUniqueKeyJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var adapter=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var access=CosmicQueryReadAccess.OwnerOrPublic(actor).Result;
        Holon New(string name)
        {
            var row=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(row.Id);return row;
        }
        async Task<List<Holon>> Stored(params Guid[] ids)
        {
            using var reader=CosmicSession.Create(manager,adapter,access).Result;
            var rows=await reader.Linq<Holon>(trackResults:false).Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync();
            Assert.IsFalse(rows.IsError,rows.Message);return rows.Result.ToList();
        }
        var checks=0;
        var scope="email"+Guid.NewGuid().ToString("N");
        CosmicSession Open(){var opened=CosmicSession.Create(manager,adapter,access).Result;Assert.IsFalse(opened.HasUniqueKey<Holon>(scope,row=>row.Description).IsError);return opened;}
        async Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolon>>> Save(CosmicSession session)=>await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        using var first=Open();
        var a=New("unique-a");a.Description="a@example.test";Assert.IsFalse(first.Add(a).IsError);
        var created=await Save(first);Assert.IsFalse(created.IsError,created.ErrorCode+": "+created.Message);checks++;
        using var second=Open();
        var b=New("unique-b");b.Description="a@example.test";Assert.IsFalse(second.Add(b).IsError);
        var duplicate=await Save(second);
        Assert.IsTrue(duplicate.IsError);Assert.AreEqual("COSMIC_UNIQUE_KEY_CONFLICT",duplicate.ErrorCode,duplicate.Message);
        Assert.AreEqual(0,(await Stored(b.Id)).Count,"A key conflict writes nothing.");checks++;
        a.Description="a2@example.test";Assert.IsFalse(first.Update(a).IsError);
        var moved=await Save(first);Assert.IsFalse(moved.IsError,moved.ErrorCode+": "+moved.Message);
        var released=await Save(second);Assert.IsFalse(released.IsError,released.ErrorCode+": "+released.Message);checks++;
        Assert.IsFalse(first.Remove(a,softDelete:false).IsError);
        var removed=await Save(first);Assert.IsFalse(removed.IsError,removed.ErrorCode+": "+removed.Message);
        using var third=Open();
        var c=New("unique-c");c.Description="a2@example.test";Assert.IsFalse(third.Add(c).IsError);
        var reused=await Save(third);Assert.IsFalse(reused.IsError,"A deleted record releases its key: "+reused.ErrorCode+": "+reused.Message);checks++;
        var byKey=await third.FindByKeyAsync<Holon>(scope,"a2@example.test");
        Assert.IsFalse(byKey.IsError,byKey.ErrorCode+": "+byKey.Message);Assert.AreEqual(c.Id,byKey.Result.Id);
        Assert.AreEqual(b.Id,(await third.FindByKeyAsync<Holon>(scope,"a@example.test")).Result.Id);
        Assert.IsNull((await third.FindByKeyAsync<Holon>(scope,"missing@example.test")).Result);checks++;
        // A02 one-to-one: a unique key on the child's ParentHolonId (EF: FK + unique index).
        var oneScope="profile"+Guid.NewGuid().ToString("N");
        using(var one=CosmicSession.Create(manager,adapter,access).Result)
        {
            Assert.IsFalse(one.HasUniqueKey<Holon>(oneScope,row=>row.ParentHolonId==Guid.Empty?null:row.ParentHolonId.ToString("D")).IsError);
            var owner=New("one-owner");Assert.IsFalse(one.Add(owner).IsError);
            Assert.IsFalse((await Save(one)).IsError);
            var profile=New("one-profile");profile.ParentHolonId=owner.Id;Assert.IsFalse(one.Add(profile).IsError);
            var first1=await Save(one);Assert.IsFalse(first1.IsError,first1.ErrorCode+": "+first1.Message);
            var second1=New("one-second");second1.ParentHolonId=owner.Id;Assert.IsFalse(one.Add(second1).IsError);
            var refused1=await Save(one);
            Assert.IsTrue(refused1.IsError);Assert.AreEqual("COSMIC_UNIQUE_KEY_CONFLICT",refused1.ErrorCode);
            Assert.AreEqual(profile.Id,(await one.FindByKeyAsync<Holon>(oneScope,owner.Id.ToString("D"))).Result.Id);
        }
        checks++;
        return new NativeFixtureJourneyProof(checks,5,new Dictionary<string,bool>{
            ["duplicateRefusedVerified"]=true,["keyMoveReleasesVerified"]=true,["deleteReleasesVerified"]=true,["findByKeyVerified"]=true,["oneToOneVerified"]=true});
    }
}

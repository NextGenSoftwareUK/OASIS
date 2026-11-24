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
    /// <summary>A06/A07: cascade delete of a descendant graph in one atomic batch.</summary>
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionCascadeDeleteRemovesDescendantsAtomically(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyCascadeJourney,nativeJourneySuffix:"-cascade-delete-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyCascadeJourney(
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
        using var session=CosmicSession.Create(manager,adapter,access).Result;
        var root=New("cascade-root");var child=New("cascade-child");var grandchild=New("cascade-grandchild");var other=New("cascade-other");
        Assert.IsFalse(session.Add(root).IsError);Assert.IsFalse(session.Add(other).IsError);
        Assert.IsFalse((await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
        child.ParentHolonId=root.Id;Assert.IsFalse(session.Add(child).IsError);
        Assert.IsFalse((await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))).IsError);
        grandchild.ParentHolonId=child.Id;Assert.IsFalse(session.Add(grandchild).IsError);
        var built=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(built.IsError,built.Message);checks++;
        using var deleter=CosmicSession.Create(manager,adapter,access).Result;
        var loaded=(await deleter.FindAsync<Holon>(root.Id)).Result;Assert.IsNotNull(loaded);
        var bounded=await deleter.RemoveCascadeAsync(loaded,softDelete:false,maximumDescendants:1);
        Assert.IsTrue(bounded.IsError);Assert.AreEqual("COSMIC_CASCADE_LIMIT_EXCEEDED",bounded.ErrorCode);checks++;
        var queued=await deleter.RemoveCascadeAsync(loaded,softDelete:false);
        Assert.IsFalse(queued.IsError,queued.ErrorCode+": "+queued.Message);Assert.AreEqual(3,queued.Result);checks++;
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var deleted=await deleter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings.Add("cascade-commit",clock.ElapsedMilliseconds);
        Assert.IsFalse(deleted.IsError,deleted.ErrorCode+": "+deleted.Message);checks++;
        CollectionAssert.AreEquivalent(new[]{other.Id},(await Stored(root.Id,child.Id,grandchild.Id,other.Id)).Select(row=>row.Id).ToArray());checks++;
        return new NativeFixtureJourneyProof(checks,4,new Dictionary<string,bool>{
            ["cascadeDeleteVerified"]=true,["cascadeBoundRefusedVerified"]=true,["siblingPreservedVerified"]=true});
    }
}

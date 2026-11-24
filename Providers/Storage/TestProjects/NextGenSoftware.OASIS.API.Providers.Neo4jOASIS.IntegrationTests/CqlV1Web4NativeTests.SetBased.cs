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
    /// <summary>S16/S17: ExecuteUpdate/ExecuteDelete equivalents without tracked entities.</summary>
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task StatelessSetBasedUpdateAndDeleteAreAtomicAndBounded(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifySetBasedJourney,nativeJourneySuffix:"-set-based-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifySetBasedJourney(
        IOASISStorageProvider provider,List<Guid> owned,Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA{OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS{
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings{LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();
        var session=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        Holon New(string name){var row=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};owned.Add(row.Id);return row;}
        var rows=new[]{New("set-a"),New("set-b"),New("set-c")};
        var created=await session.SaveAsync(rows,new CosmicWriteOptions(Guid.NewGuid(),rows.ToDictionary(row=>row.Id,row=>(Guid?)null)));
        Assert.IsFalse(created.IsError,created.Message);
        var ids=rows.Select(row=>row.Id).ToArray();var checks=0;
        CosmicQuery<Holon> Matching(params Guid[] selected)=>session.Query<Holon>().Result.Where(row=>selected.Contains(row.Id)).Result;
        async Task<List<Holon>> Stored()=> (await session.ReadAsync(Matching(ids))).Result.ToList();

        var clock=System.Diagnostics.Stopwatch.StartNew();
        var updated=await session.ExecuteUpdateAsync(Matching(ids[0],ids[1]),row=>row.Description="set-updated",new CosmicWriteOptions(Guid.NewGuid()));
        timings.Add("execute-update",clock.ElapsedMilliseconds);
        Assert.IsFalse(updated.IsError,updated.ErrorCode+": "+updated.Message);Assert.AreEqual(2,updated.Result);checks++;
        var afterUpdate=await Stored();
        Assert.AreEqual(2,afterUpdate.Count(row=>row.Description=="set-updated"));checks++;

        var bounded=await session.ExecuteDeleteAsync(Matching(ids),new CosmicWriteOptions(Guid.NewGuid()),softDelete:false,maximumRows:2);
        Assert.IsTrue(bounded.IsError);Assert.AreEqual("COSMIC_SET_LIMIT_EXCEEDED",bounded.ErrorCode);
        Assert.AreEqual(3,(await Stored()).Count,"An oversized set must delete nothing.");checks++;

        clock.Restart();
        var deleted=await session.ExecuteDeleteAsync(Matching(ids[1],ids[2]),new CosmicWriteOptions(Guid.NewGuid()),softDelete:false);
        timings.Add("execute-delete",clock.ElapsedMilliseconds);
        Assert.IsFalse(deleted.IsError,deleted.ErrorCode+": "+deleted.Message);Assert.AreEqual(2,deleted.Result);checks++;
        CollectionAssert.AreEquivalent(new[]{ids[0]},(await Stored()).Select(row=>row.Id).ToArray());checks++;

        var none=await session.ExecuteDeleteAsync(Matching(Guid.NewGuid()),new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(none.IsError,none.Message);Assert.AreEqual(0,none.Result);checks++;
        // S14: synchronous forms share the async semantics.
        var syncNone=session.ExecuteDelete(Matching(Guid.NewGuid()),new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(syncNone.IsError,syncNone.Message);Assert.AreEqual(0,syncNone.Result);
        var syncUpdated=session.ExecuteUpdate(Matching(ids[0]),row=>row.Description="set-sync",new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(syncUpdated.IsError,syncUpdated.ErrorCode+": "+syncUpdated.Message);Assert.AreEqual(1,syncUpdated.Result);
        Assert.AreEqual("set-sync",(await Stored()).Single().Description);checks++;
        return new NativeFixtureJourneyProof(checks,3,new Dictionary<string,bool>{
            ["setUpdateVerified"]=true,["setDeleteVerified"]=true,["oversizedSetRefusedVerified"]=true,["emptySetNoOpVerified"]=true,["syncFormsVerified"]=true});
    }
}

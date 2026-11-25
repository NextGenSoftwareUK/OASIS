using System.Collections.Specialized;
using System.ComponentModel;
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
    // O18: EF6 DbSet.Local / databinding local view over the session identity map.
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task NativeSessionLocalViewTracksAddsReadsRemovalsAndCommits(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,
            nativeJourney:VerifyLocalViewJourney,nativeJourneySuffix:"-local-view-native.json");

    private static async Task<NativeFixtureJourneyProof> VerifyLocalViewJourney(
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
        async Task<T> Timed<T>(string name,Func<Task<T>> action)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await action();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name)
        {
            var entity=new Holon{Id=Guid.NewGuid(),Name=name,CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
            owned.Add(entity.Id);return entity;
        }
        var checks=0;

        // 1. Adds through the session and through the view raise ordered Add notifications.
        using var writer=CosmicSession.Create(manager,adapter,access).Result;
        var localResult=writer.Local<Holon>();
        Assert.IsFalse(localResult.IsError,localResult.Message);
        var local=localResult.Result;
        var events=new List<NotifyCollectionChangedEventArgs>();var counts=0;
        local.CollectionChanged+=(_,e)=>events.Add(e);
        ((INotifyPropertyChanged)local).PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(local.Count))counts++;};
        var first=New("local first");var second=New("local second");var third=New("local third");
        Assert.IsFalse(writer.Add(first).IsError);
        Assert.IsFalse(local.Add(second).IsError);
        Assert.AreEqual(CosmicEntityState.Added,writer.State(second).Result);
        CollectionAssert.AreEqual(new[]{first,second},local.ToArray());
        Assert.AreEqual(2,events.Count);Assert.IsTrue(events.All(e=>e.Action==NotifyCollectionChangedAction.Add));
        Assert.AreEqual(0,events[0].NewStartingIndex);Assert.AreEqual(1,events[1].NewStartingIndex);Assert.AreEqual(2,counts);
        Assert.IsTrue(local.Add(first).IsError,"A tracked entity cannot be added twice.");Assert.AreEqual(2,local.Count);
        checks++;

        // 2. Removing an uncommitted Add detaches it and leaves the view.
        Assert.IsFalse(writer.Add(third).IsError);Assert.AreEqual(3,local.Count);
        Assert.IsFalse(local.Remove(third).IsError);
        Assert.AreEqual(CosmicEntityState.Detached,writer.State(third).Result);
        CollectionAssert.AreEqual(new[]{first,second},local.ToArray());
        Assert.AreEqual(NotifyCollectionChangedAction.Remove,events[^1].Action);Assert.AreSame(third,events[^1].OldItems![0]);
        owned.Remove(third.Id);
        checks++;

        // 3. An acknowledged native commit keeps the committed entities in the view without spurious events.
        var before=events.Count;
        var created=await Timed("local-view-create",()=>writer.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        CollectionAssert.AreEqual(new[]{first,second},local.ToArray());Assert.AreEqual(before,events.Count);
        Assert.AreEqual(CosmicEntityState.Unchanged,writer.State(first).Result);
        checks++;

        // 4. Tracked native reads populate a second session's view; no-tracking reads never do.
        using var reader=CosmicSession.Create(manager,adapter,access).Result;
        var readerLocal=reader.Local<Holon>().Result;var readerAdds=0;
        readerLocal.CollectionChanged+=(_,e)=>{if(e.Action==NotifyCollectionChangedAction.Add)readerAdds++;};
        var ids=new[]{first.Id,second.Id};
        var plain=await Timed("local-view-no-tracking-read",()=>reader.Linq<Holon>().Result.Where(row=>ids.Contains(row.Id)).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(plain.IsError,plain.Message+plain.Exception);Assert.AreEqual(2,plain.Result.Count);
        Assert.AreEqual(0,readerLocal.Count);Assert.AreEqual(0,readerAdds);
        var loaded=await Timed("local-view-tracked-read",()=>reader.Linq<Holon>().Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync());
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.AreEqual(2,loaded.Result.Count);
        Assert.AreEqual(2,readerLocal.Count);Assert.AreEqual(2,readerAdds);
        CollectionAssert.AreEquivalent(loaded.Result.ToArray(),readerLocal.ToArray());
        var again=await Timed("local-view-repeat-read",()=>reader.Linq<Holon>().Result.Where(row=>ids.Contains(row.Id)).ToCosmicListAsync());
        Assert.IsFalse(again.IsError,again.Message+again.Exception);Assert.AreEqual(2,readerLocal.Count);Assert.AreEqual(2,readerAdds);
        checks++;

        // 5. Remove marks Deleted and leaves the view; CancelRemoval returns it; Detach removes it.
        var victim=loaded.Result.Single(row=>row.Id==second.Id);
        Assert.IsFalse(readerLocal.Remove(victim,softDelete:false).IsError);
        Assert.AreEqual(CosmicEntityState.Deleted,reader.State(victim).Result);
        Assert.AreEqual(1,readerLocal.Count);Assert.IsFalse(readerLocal.Contains(victim));
        Assert.IsFalse(reader.CancelRemoval(victim).IsError);
        Assert.AreEqual(2,readerLocal.Count);Assert.IsTrue(readerLocal.Contains(victim));
        var keeper=loaded.Result.Single(row=>row.Id==first.Id);
        Assert.IsTrue(reader.Detach(keeper).Result);
        CollectionAssert.AreEqual(new[]{victim},readerLocal.ToArray());
        checks++;

        // 6. An acknowledged native hard delete leaves the entity out of the view and out of the store.
        Assert.IsFalse(readerLocal.Remove(victim,softDelete:false).IsError);
        var deleted=await Timed("local-view-delete",()=>reader.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(deleted.IsError,deleted.Message+deleted.Exception);Assert.IsTrue(deleted.IsSaved);
        Assert.AreEqual(0,readerLocal.Count);Assert.AreEqual(CosmicEntityState.Detached,reader.State(victim).Result);
        var gone=await Timed("local-view-deleted-read",()=>reader.Linq<Holon>().Result.Where(row=>row.Id==second.Id).AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(gone.IsError,gone.Message+gone.Exception);Assert.AreEqual(0,gone.Result.Count);
        owned.Remove(second.Id);
        checks++;

        // 7. The view never shows another actor's rows: a foreign session's tracked read stays empty.
        using var foreign=CosmicSession.Create(manager,adapter,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var foreignLocal=foreign.Local<Holon>().Result;
        var hidden=await Timed("local-view-foreign-read",()=>foreign.Linq<Holon>().Result.Where(row=>row.Id==first.Id).ToCosmicListAsync());
        Assert.IsFalse(hidden.IsError,hidden.Message+hidden.Exception);Assert.AreEqual(0,hidden.Result.Count);Assert.AreEqual(0,foreignLocal.Count);
        checks++;

        // 8. Clear resets the view; a disposed view stops tracking; disposing the session closes all views.
        var resets=0;local.CollectionChanged+=(_,e)=>{if(e.Action==NotifyCollectionChangedAction.Remove)resets++;};
        var detachedView=writer.Local<Holon>().Result;detachedView.Dispose();
        Assert.IsFalse(detachedView.IsOpen);Assert.AreEqual(0,detachedView.Count);
        Assert.IsTrue(detachedView.Add(New("never")).IsError);owned.RemoveAt(owned.Count-1);
        Assert.IsFalse(writer.Clear().IsError);
        Assert.AreEqual(0,local.Count);Assert.AreEqual(2,resets);
        Assert.IsFalse(writer.Attach(first).IsError);Assert.AreEqual(1,local.Count);
        writer.Dispose();
        Assert.IsFalse(local.IsOpen);Assert.AreEqual(0,local.Count);
        Assert.IsTrue(writer.Local<Holon>().IsError);
        checks++;

        return new NativeFixtureJourneyProof(checks,2,new Dictionary<string,bool>{
            ["sessionAndViewAddsNotifyInOrder"]=true,["uncommittedAddRemovalDetaches"]=true,
            ["acknowledgedCommitKeepsView"]=true,["trackedReadsPopulateNoTrackingDoesNot"]=true,
            ["removeCancelDetachSynchronize"]=true,["acknowledgedDeleteLeavesViewAndStore"]=true,
            ["foreignActorViewStaysEmpty"]=true,["clearDisposeAndSessionLifetime"]=true});
    }
}

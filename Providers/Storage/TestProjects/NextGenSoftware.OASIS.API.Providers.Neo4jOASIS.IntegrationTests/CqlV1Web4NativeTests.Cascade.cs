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
        // A06 cascade insert: a new child placed in a tracked parent's Children is inserted by SaveChanges.
        var navChild=new Holon{Name="cascade-nav-child",CreatedByAvatarId=actor,IsActive=true,IsPublic=false,IsNewHolon=true};
        root.Children.Add(navChild);
        var navSaved=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(navSaved.IsError,navSaved.ErrorCode+": "+navSaved.Message);owned.Add(navChild.Id);
        var navStored=(await Stored(navChild.Id)).Single();Assert.AreEqual(root.Id,navStored.ParentHolonId);checks++;
        // A17: re-parenting a record under its own descendant is refused before dispatch.
        root.ParentHolonId=grandchild.Id;
        var cyclic=await session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsTrue(cyclic.IsError);Assert.AreEqual("COSMIC_PARENT_CYCLE",cyclic.ErrorCode,cyclic.Message);
        root.ParentHolonId=Guid.Empty;
        Assert.AreEqual(Guid.Empty,(await Stored(root.Id)).Single().ParentHolonId);checks++;
        using var deleter=CosmicSession.Create(manager,adapter,access).Result;
        var loaded=(await deleter.FindAsync<Holon>(root.Id)).Result;Assert.IsNotNull(loaded);
        // A10: explicit loading assigns tracked children without dirtying the parent.
        var children=await deleter.LoadChildrenAsync<Holon>(loaded);
        Assert.IsFalse(children.IsError,children.ErrorCode+": "+children.Message);
        CollectionAssert.AreEquivalent(new[]{child.Id,navChild.Id},children.Result.Select(row=>row.Id).ToArray());
        Assert.AreEqual(2,loaded.Children.Count);checks++;
        Assert.AreEqual(CosmicEntityState.Unchanged,deleter.State(loaded).Result);
        Assert.AreSame(children.Result.Single(row=>row.Id==child.Id),(await deleter.FindAsync<Holon>(child.Id)).Result,"Loaded children join the identity map.");checks++;
        // A05: a child created under the loaded parent joins its Children after commit.
        var late=New("cascade-late-child");late.ParentHolonId=root.Id;Assert.IsFalse(deleter.Add(late).IsError);
        var lateSaved=await deleter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(lateSaved.IsError,lateSaved.Message);
        CollectionAssert.AreEquivalent(new[]{child.Id,navChild.Id,late.Id},loaded.Children.Select(row=>row.Id).ToArray());checks++;
        // A12: extra-lazy count, membership and element access without loading the collection.
        var count=await deleter.CountChildrenAsync(loaded);Assert.IsFalse(count.IsError,count.Message);Assert.AreEqual(3,count.Result);
        Assert.IsTrue((await deleter.ContainsChildAsync(loaded,child.Id)).Result);
        Assert.IsFalse((await deleter.ContainsChildAsync(loaded,other.Id)).Result);checks++;
        var firstChild=await deleter.ChildAtAsync(loaded,0);Assert.IsFalse(firstChild.IsError,firstChild.Message);
        Assert.AreEqual(child.Id,firstChild.Result.Id);
        Assert.IsNull((await deleter.ChildAtAsync(loaded,5)).Result);checks++;
        // A07 delete-orphan: removing a child from the loaded collection deletes it on SaveChanges.
        Assert.IsFalse(deleter.SetDeleteOrphans(true,softDelete:false).IsError);
        loaded.Children.Remove(loaded.Children.Single(row=>row.Id==late.Id));
        var orphaned=await deleter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        Assert.IsFalse(orphaned.IsError,orphaned.ErrorCode+": "+orphaned.Message);
        Assert.AreEqual(0,(await Stored(late.Id)).Count,"The orphan is deleted.");
        Assert.AreEqual(1,(await Stored(root.Id)).Count,"The parent stays.");checks++;
        // A14 batch fetch: one native query fills the Children of several parents.
        using(var batch=CosmicSession.Create(manager,adapter,access).Result)
        {
            var p1=(await batch.FindAsync<Holon>(root.Id)).Result;var p2=(await batch.FindAsync<Holon>(child.Id)).Result;
            var queriesBefore=batch.Statistics.QueryCount;
            var fetched=await batch.LoadChildrenAsync<Holon>(new IHolon[]{p1,p2});
            Assert.IsFalse(fetched.IsError,fetched.ErrorCode+": "+fetched.Message);Assert.AreEqual(3,fetched.Result);
            CollectionAssert.AreEquivalent(new[]{child.Id,navChild.Id},p1.Children.Select(row=>row.Id).ToArray());
            CollectionAssert.AreEquivalent(new[]{grandchild.Id},p2.Children.Select(row=>row.Id).ToArray());
            Assert.AreSame(p2,p1.Children.Single(row=>row.Id==child.Id),"Batch-loaded rows join the identity map.");
            Assert.AreEqual(queriesBefore+1,batch.Statistics.QueryCount,"Exactly one native query fetched both collections.");
        }
        checks++;
        // A11 lazy loading: Children loads on first access, not on materialization or dirty checks.
        using(var lazy=CosmicSession.Create(manager,adapter,access).Result)
        {
            Assert.IsFalse(lazy.SetLazyLoading(true).IsError);
            var lazyRoot=(await lazy.FindAsync<Holon>(root.Id)).Result;
            Assert.IsInstanceOfType(lazyRoot.Children,typeof(CosmicLazyChildren));
            Assert.AreEqual(CosmicEntityState.Unchanged,lazy.State(lazyRoot).Result);
            Assert.IsFalse(((CosmicLazyChildren)lazyRoot.Children).IsLoaded,"Materialization and dirty checks do not load children.");
            CollectionAssert.AreEquivalent(new[]{child.Id,navChild.Id},lazyRoot.Children.Select(row=>row.Id).ToArray());
            Assert.AreSame(lazyRoot.Children[0],(await lazy.FindAsync<Holon>(lazyRoot.Children[0].Id)).Result,"Lazily loaded children are tracked.");
        }
        checks++;
        // A09 filtered include: native filter, ordering and paging of one navigation.
        using(var filtered=CosmicSession.Create(manager,adapter,access).Result)
        {
            var filteredRoot=(await filtered.FindAsync<Holon>(root.Id)).Result;
            var only=await filtered.LoadChildrenAsync<Holon,string>(filteredRoot,row=>row.Name.StartsWith("cascade-nav",StringComparison.Ordinal),row=>row.Name);
            Assert.IsFalse(only.IsError,only.ErrorCode+": "+only.Message);
            CollectionAssert.AreEqual(new[]{navChild.Id},filteredRoot.Children.Select(row=>row.Id).ToArray());
            var top=await filtered.LoadChildrenAsync<Holon,string>(filteredRoot,row=>row.IsActive,row=>row.Name,descending:true,take:1);
            Assert.IsFalse(top.IsError,top.ErrorCode+": "+top.Message);
            CollectionAssert.AreEqual(new[]{navChild.Id},top.Result.Select(row=>row.Id).ToArray(),"Ordered descending by name, first page of one.");
        }
        checks++;
        var bounded=await deleter.RemoveCascadeAsync(loaded,softDelete:false,maximumDescendants:1);
        Assert.IsTrue(bounded.IsError);Assert.AreEqual("COSMIC_CASCADE_LIMIT_EXCEEDED",bounded.ErrorCode);checks++;
        var queued=await deleter.RemoveCascadeAsync(loaded,softDelete:false);
        Assert.IsFalse(queued.IsError,queued.ErrorCode+": "+queued.Message);Assert.AreEqual(4,queued.Result);checks++;
        var clock=System.Diagnostics.Stopwatch.StartNew();
        var deleted=await deleter.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()));
        timings.Add("cascade-commit",clock.ElapsedMilliseconds);
        Assert.IsFalse(deleted.IsError,deleted.ErrorCode+": "+deleted.Message);checks++;
        CollectionAssert.AreEquivalent(new[]{other.Id},(await Stored(root.Id,child.Id,grandchild.Id,late.Id,navChild.Id,other.Id)).Select(row=>row.Id).ToArray());checks++;
        return new NativeFixtureJourneyProof(checks,4,new Dictionary<string,bool>{
            ["cascadeDeleteVerified"]=true,["cascadeBoundRefusedVerified"]=true,["siblingPreservedVerified"]=true,["explicitChildLoadingVerified"]=true,["loadedCollectionFixUpVerified"]=true,["extraLazyCollectionVerified"]=true,["navigationCascadeInsertVerified"]=true,["deleteOrphanVerified"]=true,["batchFetchVerified"]=true,["lazyLoadingVerified"]=true,["filteredIncludeVerified"]=true,["parentCycleRefusedVerified"]=true});
    }
}

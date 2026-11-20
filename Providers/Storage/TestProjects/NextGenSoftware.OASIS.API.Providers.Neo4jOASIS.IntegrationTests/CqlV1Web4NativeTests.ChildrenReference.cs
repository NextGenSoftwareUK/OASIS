using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;
public sealed partial class CqlV1Web4NativeTests
{
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task SessionLoadsCompleteAuthorizedChildrenAndRejectsPartialReplacement(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,childrenReferenceJourney:true);

    private static async Task<int> VerifyChildrenReferenceJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,
            StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}
        }};
        var registry=new ProviderManager(null,dna) {
            IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false
        };
        var manager=new HolonManager(provider,dna,registry);
        var actor=Guid.NewGuid();var foreignActor=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(foreignActor).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name,Guid owner)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=owner,IsPublic=false,IsActive=true,IsNewHolon=true};
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var parent=New("collection parent",actor);ownedIds.Add(parent.Id);
        Assert.IsFalse(session.Add(parent).IsError);
        Saved(await Timed("create-parent",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var first=New("first persisted",actor);var second=New("second persisted",actor);
        first.ParentHolonId=parent.Id;second.ParentHolonId=parent.Id;
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Assert.IsFalse(session.Add(first).IsError);Assert.IsFalse(session.Add(second).IsError);
        Saved(await Timed("create-owned-children",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var hidden=New("private foreign child",foreignActor);hidden.ParentHolonId=parent.Id;ownedIds.Add(hidden.Id);
        Assert.IsFalse(foreign.Add(hidden).IsError);
        Saved(await Timed("create-private-child",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        parent.Children=new List<IHolon>();
        var original=parent.Children;
        var overflow=await Timed("reject-partial-replacement",()=>session.LoadChildrenAsync<Holon>(parent,1));
        Assert.IsTrue(overflow.IsError);Assert.AreEqual("COSMIC_SESSION_COLLECTION_LIMIT",overflow.ErrorCode);
        Assert.AreSame(original,parent.Children);Assert.AreEqual(0,parent.Children.Count);
        first.Name="pending child edit";
        var loaded=await Timed("load-complete-visible-collection",()=>session.LoadChildrenAsync<Holon>(parent,2));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.IsTrue(loaded.IsLoaded);
        Assert.AreEqual(2,loaded.Result.Count);Assert.AreEqual(2,parent.Children.Count);
        Assert.AreSame(first,loaded.Result.Single(row=>row.Id==first.Id));
        Assert.AreSame(second,loaded.Result.Single(row=>row.Id==second.Id));
        Assert.AreSame(first,parent.Children.Single(row=>row.Id==first.Id));
        Assert.AreEqual("pending child edit",first.Name);
        Assert.IsFalse(parent.Children.Any(row=>row.Id==hidden.Id));
        Assert.AreEqual(CosmicEntityState.Detached,session.State(hidden).Result);
        var stored=await Timed("collection-read-does-not-flush",()=>session.GetDatabaseValuesAsync(first));
        Assert.IsFalse(stored.IsError,stored.Message);Assert.AreEqual("first persisted",stored.Result.Name);
        var foreignParent=HolonChangeTracking.Clone(parent);
        Assert.IsFalse(foreign.Attach(foreignParent).IsError);
        var denied=await Timed("source-authorization-rechecked",()=>foreign.LoadChildrenAsync<Holon>(foreignParent,2));
        Assert.IsTrue(denied.IsError);Assert.AreEqual("COSMIC_SESSION_REFERENCE_UNAVAILABLE",denied.ErrorCode);
        var empty=await Timed("load-empty-complete-collection",()=>session.LoadChildrenAsync<Holon>(second,2));
        Assert.IsFalse(empty.IsError,empty.Message+empty.Exception);Assert.IsTrue(empty.IsLoaded);
        Assert.AreEqual(0,empty.Result.Count);Assert.AreEqual(0,second.Children.Count);
        return 1;
    }
}

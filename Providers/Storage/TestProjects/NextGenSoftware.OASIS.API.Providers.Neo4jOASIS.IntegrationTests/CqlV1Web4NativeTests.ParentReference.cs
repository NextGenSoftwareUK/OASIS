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
    public Task SessionLoadsParentThroughNativeAccessAndCanonicalIdentity(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,parentReferenceJourney:true);

    private static async Task<int> VerifyParentReferenceJourney(IOASISStorageProvider provider,
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
        var parent=New("parent persisted",actor);var child=New("child persisted",actor);
        child.ParentHolonId=parent.Id;
        ownedIds.Add(parent.Id);ownedIds.Add(child.Id);
        Assert.IsFalse(session.Add(parent).IsError);
        Saved(await Timed("create-owned-parent",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.IsFalse(session.Add(child).IsError);
        Saved(await Timed("create-owned-child",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var hidden=New("private foreign parent",foreignActor);ownedIds.Add(hidden.Id);
        Assert.IsFalse(foreign.Add(hidden).IsError);
        Saved(await Timed("create-private-parent",()=>foreign.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));

        child.ParentHolon=null;parent.Name="pending parent edit";
        var loaded=await Timed("load-canonical-parent",()=>session.LoadParentAsync<Holon>(child));
        Assert.IsFalse(loaded.IsError,loaded.Message+loaded.Exception);Assert.IsTrue(loaded.IsLoaded);
        Assert.AreSame(parent,loaded.Result);Assert.AreSame(parent,child.ParentHolon);
        Assert.AreEqual("pending parent edit",parent.Name);
        Assert.AreEqual(CosmicEntityState.Modified,session.State(parent).Result);
        var storedParent=await Timed("parent-read-does-not-flush",()=>session.GetDatabaseValuesAsync(parent));
        Assert.IsFalse(storedParent.IsError,storedParent.Message);
        Assert.AreEqual("parent persisted",storedParent.Result.Name);

        child.ParentHolonId=hidden.Id;
        var privateParent=await Timed("private-target-not-exposed",()=>session.LoadParentAsync<Holon>(child));
        Assert.IsFalse(privateParent.IsError,privateParent.Message+privateParent.Exception);
        Assert.IsFalse(privateParent.IsLoaded);Assert.IsNull(privateParent.Result);Assert.IsNull(child.ParentHolon);
        Assert.AreEqual(hidden.Id,child.ParentHolonId);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(hidden).Result);
        var storedChild=await Timed("reference-read-does-not-flush",()=>session.GetDatabaseValuesAsync(child));
        Assert.IsFalse(storedChild.IsError,storedChild.Message);
        Assert.AreEqual(parent.Id,storedChild.Result.ParentHolonId);
        child.ParentHolonId=parent.Id;

        var foreignChild=HolonChangeTracking.Clone(child);
        Assert.IsFalse(foreign.Attach(foreignChild).IsError);
        var denied=await Timed("source-authorization-rechecked",()=>foreign.LoadParentAsync<Holon>(foreignChild));
        Assert.IsTrue(denied.IsError);
        Assert.AreEqual("COSMIC_SESSION_REFERENCE_UNAVAILABLE",denied.ErrorCode);

        Assert.IsFalse(session.Detach(parent).IsError);
        child.ParentHolon=null;
        var fresh=await Timed("load-detached-parent-fresh",()=>session.LoadParentAsync<Holon>(child));
        Assert.IsFalse(fresh.IsError,fresh.Message+fresh.Exception);Assert.IsTrue(fresh.IsLoaded);
        Assert.AreNotSame(parent,fresh.Result);Assert.AreSame(fresh.Result,child.ParentHolon);
        Assert.AreEqual("parent persisted",fresh.Result.Name);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(fresh.Result).Result);
        return 1;
    }
}

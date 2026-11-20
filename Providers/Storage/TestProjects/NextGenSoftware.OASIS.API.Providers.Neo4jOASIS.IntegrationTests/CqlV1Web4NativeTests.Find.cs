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
    public Task SessionFindUsesCanonicalCacheAndFreshNativeIdentityAuthorization(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,findJourney:true);

    private static async Task<int> VerifyFindJourney(IOASISStorageProvider provider,
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
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var foreign=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        Task<OASISResult<IEnumerable<IHolon>>> Create(Holon row)=>manager.SaveOwnedHolonsAsync(new[]{row},actor,
            new CosmicWriteOptions(Guid.NewGuid()),saveChildren:false,recursive:false,providerType:native.QueryProviderType);
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var pending=New("local only");var pendingId=pending.Id;
        Assert.IsFalse(session.Add(pending).IsError);
        var local=await session.FindAsync<Holon>(pendingId);
        Assert.IsFalse(local.IsError,local.Message);Assert.AreSame(pending,local.Result);
        pending.Id=Guid.NewGuid();
        var changedKey=await session.FindAsync<Holon>(pendingId);
        Assert.IsTrue(changedKey.IsError);Assert.AreEqual("COSMIC_SESSION_IDENTITY_CHANGED",changedKey.ErrorCode);
        pending.Id=pendingId;Assert.IsFalse(session.Remove(pending).IsError);
        var first=New("find persisted");ownedIds.Add(first.Id);
        Saved(await Timed("create-root",()=>Create(first)));
        var found=await Timed("first-native-find",()=>session.FindAsync<Holon>(first.Id));
        Assert.IsFalse(found.IsError,found.Message+found.Exception);Assert.IsTrue(found.IsLoaded);
        Assert.AreNotSame(first,found.Result);Assert.AreEqual("find persisted",found.Result.Name);
        var cached=found.Result;cached.Name="pending cached edit";
        var again=await Timed("cached-find-fresh-id-authorization",()=>session.FindAsync<Holon>(first.Id));
        Assert.IsFalse(again.IsError,again.Message+again.Exception);Assert.IsTrue(again.IsLoaded);
        Assert.AreSame(cached,again.Result);Assert.AreEqual("pending cached edit",again.Result.Name);
        Assert.AreEqual(CosmicEntityState.Modified,session.State(cached).Result);
        var stored=await session.GetDatabaseValuesAsync(cached);
        Assert.IsFalse(stored.IsError,stored.Message);Assert.AreEqual("find persisted",stored.Result.Name);
        var privateCopy=HolonChangeTracking.Clone(cached);
        Assert.IsFalse(foreign.Attach(privateCopy).IsError);
        var denied=await Timed("cached-foreign-identity-not-authorized",()=>foreign.FindAsync<Holon>(first.Id));
        Assert.IsFalse(denied.IsError,denied.Message+denied.Exception);
        Assert.IsFalse(denied.IsLoaded);Assert.IsNull(denied.Result);
        var appeared=New("appeared after absent lookup");ownedIds.Add(appeared.Id);
        var missing=await Timed("absent-key-not-cached",()=>session.FindAsync<Holon>(appeared.Id));
        Assert.IsFalse(missing.IsError,missing.Message);Assert.IsFalse(missing.IsLoaded);Assert.IsNull(missing.Result);
        Saved(await Timed("create-previously-absent-key",()=>Create(appeared)));
        var appearedResult=await Timed("find-previously-absent-key",()=>session.FindAsync<Holon>(appeared.Id));
        Assert.IsFalse(appearedResult.IsError,appearedResult.Message);Assert.IsTrue(appearedResult.IsLoaded);
        Assert.AreEqual("appeared after absent lookup",appearedResult.Result.Name);
        Assert.IsFalse(session.Detach(cached).IsError);
        var detached=await Timed("detached-key-loads-fresh",()=>session.FindAsync<Holon>(first.Id));
        Assert.IsFalse(detached.IsError,detached.Message+detached.Exception);Assert.IsTrue(detached.IsLoaded);
        Assert.AreNotSame(cached,detached.Result);Assert.AreEqual("find persisted",detached.Result.Name);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(cached).Result);
        Assert.AreEqual(CosmicEntityState.Unchanged,session.State(detached.Result).Result);
        return 1;
    }
}

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
    [DataTestMethod]
    [DataRow("MongoDBOASIS")] [DataRow("SQLLiteDBOASIS")] [DataRow("Neo4jOASIS")]
    public Task StatelessGetUsesFreshAuthorizedNativeIdentity(string providerName)
        =>RunGroupedDistinctNativeFixture(providerName,false,statelessGetJourney:true);
    private static async Task<int> VerifyStatelessGetJourney(IOASISStorageProvider provider,
        List<Guid> ownedIds,Dictionary<string,long> timings)
    {
        var native=(ICosmicQueryStorageProvider)provider;
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
            HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {LogSwitchingProviders=false}}};
        var registry=new ProviderManager(null,dna) {IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var manager=new HolonManager(provider,dna,registry);var actor=Guid.NewGuid();
        using var managed=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var session=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try{return await operation();}finally{timings.Add(name,clock.ElapsedMilliseconds);}
        }
        var entity=new Holon {Id=Guid.NewGuid(),Name="stateless key original",CreatedByAvatarId=actor,
            IsPublic=false,IsActive=true,IsNewHolon=true};ownedIds.Add(entity.Id);
        Assert.IsFalse(managed.Add(entity).IsError);
        var saved=await Timed("create",()=>managed.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);Assert.IsTrue(saved.IsSaved);
        var first=await Timed("get-initial",()=>session.GetAsync<Holon>(entity.Id));
        Assert.IsFalse(first.IsError,first.Message+first.Exception);Assert.IsTrue(first.IsLoaded);
        Assert.AreEqual(entity.Id,first.Result.Id);Assert.AreEqual("stateless key original",first.Result.Name);
        first.Result.Name="untracked change";
        var again=await Timed("get-fresh",()=>session.GetAsync<Holon>(entity.Id));
        Assert.IsFalse(again.IsError,again.Message);Assert.AreNotSame(first.Result,again.Result);
        Assert.AreEqual("stateless key original",again.Result.Name);
        entity.Name="stateless key persisted update";
        var updated=await Timed("update",()=>managed.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(updated.IsError,updated.Message+updated.Exception);Assert.IsTrue(updated.IsSaved);
        var current=await Timed("get-updated",()=>session.GetAsync<Holon>(entity.Id));
        Assert.IsFalse(current.IsError,current.Message);Assert.AreEqual(entity.Name,current.Result.Name);
        var synchronous=await Timed("get-sync",()=>Task.FromResult(session.Get<Holon>(entity.Id)));
        Assert.IsFalse(synchronous.IsError,synchronous.Message);Assert.IsTrue(synchronous.IsLoaded);
        Assert.AreEqual(entity.Id,synchronous.Result.Id);Assert.AreEqual(entity.Name,synchronous.Result.Name);
        Assert.AreNotSame(current.Result,synchronous.Result);
        var absent=await Timed("get-absent",()=>session.GetAsync<Holon>(Guid.NewGuid()));
        Assert.IsFalse(absent.IsError,absent.Message);Assert.IsFalse(absent.IsLoaded);Assert.IsNull(absent.Result);
        using var foreign=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        var hidden=await Timed("get-foreign",()=>foreign.GetAsync<Holon>(entity.Id));
        Assert.IsFalse(hidden.IsError,hidden.Message);Assert.IsFalse(hidden.IsLoaded);Assert.IsNull(hidden.Result);
        Assert.AreEqual("CQL_QUERY_CANCELLED",(await session.GetAsync<Holon>(entity.Id,new CancellationToken(true))).ErrorCode);
        session.Dispose();Assert.AreEqual("COSMIC_STATELESS_DISPOSED",session.Get<Holon>(entity.Id).ErrorCode);
        return 6;
    }
}

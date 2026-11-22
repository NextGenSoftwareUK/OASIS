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
    public Task StatelessNativeLinqKeepsFreshRowsAndSharedManagedIdentitySemantics(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,statelessLinqJourney:true);

    private static async Task<int> VerifyStatelessLinqJourney(IOASISStorageProvider provider,
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
        using var managed=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        using var stateless=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        var first=New("stateless linq first");var second=New("stateless linq second");
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Assert.IsFalse(managed.Add(first).IsError);Assert.IsFalse(managed.Add(second).IsError);
        var created=await Timed("managed-create-pair",()=>managed.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(created.IsError,created.Message+created.Exception);Assert.IsTrue(created.IsSaved);
        var ids=new[]{first.Id,second.Id};var firstId=first.Id;
        var opened=stateless.Linq<Holon>();Assert.IsFalse(opened.IsError,opened.Message);
        var scope=opened.Result.Where(row=>ids.Contains(row.Id));
        var rowsQuery=scope.OrderBy(row=>row.Name).Take(2);
        var rows=await Timed("stateless-linq-native-rows",()=>rowsQuery.ToCosmicListAsync());
        Assert.IsFalse(rows.IsError,rows.Message+rows.Exception);Assert.AreEqual(2,rows.Result.Count);
        var local=rows.Result.Single(row=>row.Id==firstId);
        Assert.AreNotSame(first,local);local.Name="untracked local edit";
        var again=await Timed("stateless-linq-fresh-rows",()=>rowsQuery.AsCosmicNoTracking().ToCosmicListAsync());
        Assert.IsFalse(again.IsError,again.Message);Assert.AreEqual(2,again.Result.Count);
        var fresh=again.Result.Single(row=>row.Id==firstId);
        Assert.AreNotSame(local,fresh);Assert.AreEqual("stateless linq first",fresh.Name);
        await Timed("stateless-linq-native-terminals",()=>{
            Assert.AreEqual(2,scope.Count());
            var single=scope.Single(row=>row.Id==firstId);
            Assert.AreNotSame(local,single);Assert.AreNotSame(fresh,single);
            Assert.AreEqual("stateless linq first",single.Name);return Task.FromResult(true);
        });
        var projected=await Timed("stateless-linq-native-projection",
            ()=>scope.OrderBy(row=>row.Name).Select(row=>row.Name).ToCosmicListAsync());
        Assert.IsFalse(projected.IsError,projected.Message);
        CollectionAssert.AreEqual(new[]{"stateless linq first","stateless linq second"},projected.Result.ToArray());
        var tracking=Assert.ThrowsException<CosmicLinqException>(()=>rowsQuery.AsCosmicTracking());
        Assert.AreEqual("COSMIC_STATELESS_TRACKING_UNSUPPORTED",tracking.Code);

        first.Name="managed pending edit";
        await Timed("shared-managed-identity-and-no-flush",async ()=>{
            var managedRoot=managed.Linq<Holon>().Result;
            Assert.AreSame(first,managedRoot.Single(row=>row.Id==firstId));
            var tracked=await managedRoot.Where(row=>row.Id==firstId).ToCosmicListAsync();
            Assert.IsFalse(tracked.IsError,tracked.Message);Assert.AreSame(first,tracked.Result.Single());
            var untracked=await managedRoot.AsCosmicNoTracking().Where(row=>row.Id==firstId).ToCosmicListAsync();
            Assert.IsFalse(untracked.IsError,untracked.Message);Assert.AreNotSame(first,untracked.Result.Single());
            Assert.AreEqual("stateless linq first",untracked.Result.Single().Name);
            Assert.AreEqual("managed pending edit",first.Name);
            var stored=await managed.GetDatabaseValuesAsync(first);
            Assert.IsFalse(stored.IsError,stored.Message);Assert.AreEqual("stateless linq first",stored.Result.Name);
            Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(first,native.QueryProviderType),
                CosmicConcurrency.GetProviderRevisionId(stored.Result,native.QueryProviderType));
            return true;
        });
        using var foreign=CosmicStatelessSession.Create(manager,native,
            CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result).Result;
        await Timed("foreign-stateless-native-linq",async ()=>{
            var foreignQuery=foreign.Linq<Holon>().Result.Where(row=>ids.Contains(row.Id));
            Assert.AreEqual(0,foreignQuery.Count());
            var hidden=await foreignQuery.ToCosmicListAsync();
            Assert.IsFalse(hidden.IsError,hidden.Message);Assert.AreEqual(0,hidden.Result.Count);
            return true;
        });
        stateless.Dispose();
        await Timed("closed-linq-execution-refused",async ()=>{
            var closed=await rowsQuery.ToCosmicListAsync();
            Assert.IsTrue(closed.IsError);Assert.AreEqual("COSMIC_STATELESS_DISPOSED",closed.ErrorCode);
            var scalar=Assert.ThrowsException<CosmicLinqException>(()=>scope.Count());
            Assert.AreEqual("COSMIC_STATELESS_DISPOSED",scalar.Code);
            return true;
        });
        return 1;
    }
}

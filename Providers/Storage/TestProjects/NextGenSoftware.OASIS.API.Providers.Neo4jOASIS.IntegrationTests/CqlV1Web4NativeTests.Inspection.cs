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
    public Task SessionInspectionAndDefaultReadOnlyPreservePendingIntent(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,inspectionJourney:true);

    private static async Task<int> VerifyInspectionJourney(IOASISStorageProvider provider,
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
        var actor=Guid.NewGuid();
        using var session=CosmicSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        Holon New(string name)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        Assert.IsFalse(session.GetDefaultReadOnly().Result);
        Assert.AreEqual(0,session.GetEntries().Result.Count);Assert.IsFalse(session.HasChanges().Result);
        Assert.IsTrue(session.SetDefaultReadOnly(true).Result);
        var first=New("inspection first");var second=New("inspection second");
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Assert.IsFalse(session.Add(first).IsError);Assert.IsFalse(session.Add(second).IsError);
        Assert.IsFalse(session.IsReadOnly(first).Result,"The load default cannot suppress an explicit Add.");
        var added=session.GetEntries();Assert.IsFalse(added.IsError,added.Message);
        Assert.AreEqual(2,added.Result.Count);Assert.IsTrue(added.Result.All(e=>e.State==CosmicEntityState.Added));
        Assert.IsTrue(session.HasChanges().Result);
        Saved(await Timed("create",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.IsFalse(session.HasChanges().Result);
        Assert.IsTrue(added.Result.All(e=>e.State==CosmicEntityState.Added),"Entry state must be a snapshot.");
        var firstId=first.Id;var secondId=second.Id;
        Assert.IsFalse(session.Clear().IsError);Assert.IsTrue(session.GetDefaultReadOnly().Result);
        var query=session.Linq<Holon>();Assert.IsFalse(query.IsError,query.Message);
        var loaded=await Timed("default-read-only-query",()=>query.Result
            .Where(row=>row.Id==firstId || row.Id==secondId).ToCosmicListAsync());
        Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(2,loaded.Result.Count);
        Assert.IsTrue(session.GetEntries().Result.All(e=>e.IsReadOnly && e.State==CosmicEntityState.Unchanged));
        var root=loaded.Result.Single(row=>row.Id==firstId);root.Name="ignored local edit";
        Assert.IsFalse(session.HasChanges().Result);
        var noOp=await Timed("read-only-no-flush",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid())));
        Assert.IsFalse(noOp.IsError,noOp.Message);Assert.IsFalse(noOp.IsSaved);
        var unchanged=await Timed("native-unchanged",()=>session.GetDatabaseValuesAsync(root));
        Assert.IsFalse(unchanged.IsError,unchanged.Message);Assert.AreEqual("inspection first",unchanged.Result.Name);
        Assert.IsFalse(session.SetDefaultReadOnly(false).Result);
        Assert.IsTrue(session.IsReadOnly(root).Result,"Changing the default must not rebase an existing root.");
        Assert.IsFalse(session.Clear().IsError);
        var mutable=await Timed("mutable-find",()=>session.FindAsync<Holon>(firstId));
        Assert.IsFalse(mutable.IsError,mutable.Message);Assert.IsTrue(mutable.IsLoaded);
        Assert.IsFalse(session.IsReadOnly(mutable.Result).Result);
        var before=session.GetEntries().Result.Single();
        mutable.Result.Name="inspection stored";Assert.IsTrue(session.HasChanges().Result);
        Assert.AreEqual(CosmicEntityState.Modified,session.GetEntries().Result.Single().State);
        Assert.AreEqual(CosmicEntityState.Unchanged,before.State);
        Saved(await Timed("mutable-save",()=>session.SaveChangesAsync(new CosmicWriteOptions(Guid.NewGuid()))));
        var changed=await Timed("native-changed",()=>session.GetDatabaseValuesAsync(mutable.Result));
        Assert.IsFalse(changed.IsError,changed.Message);Assert.AreEqual("inspection stored",changed.Result.Name);
        Assert.IsFalse(session.HasChanges().Result);
        Assert.IsTrue(session.SetDefaultReadOnly(true).Result);
        Assert.IsFalse(session.Attach(second).IsError);Assert.IsFalse(session.IsReadOnly(second).Result);
        var local=New("uncommitted");Assert.IsFalse(session.Add(local).IsError);
        Assert.IsFalse(session.IsReadOnly(local).Result);Assert.IsTrue(session.HasChanges().Result);
        Assert.IsTrue(session.Detach(local).Result);Assert.IsFalse(session.HasChanges().Result);
        session.Dispose();
        Assert.IsTrue(session.GetEntries().IsError);Assert.IsTrue(session.HasChanges().IsError);
        Assert.IsTrue(session.GetDefaultReadOnly().IsError);Assert.IsTrue(session.SetDefaultReadOnly(false).IsError);
        return 1;
    }
}

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
    public Task SessionSyncOperationsShareNativeAsyncOwnersAndCommitSemantics(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,syncJourney:true);

    private static async Task<int> VerifySyncJourney(IOASISStorageProvider provider,
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
        var stateless=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        T Timed<T>(string name,Func<T> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        async Task<T> TimedAsync<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        Holon New(string name)=>new() {Id=Guid.NewGuid(),Name=name,
            CreatedByAvatarId=actor,IsPublic=false,IsActive=true,IsNewHolon=true};
        CosmicWriteOptions Intent(params IHolon[] roots)=>new(Guid.NewGuid(),
            roots.ToDictionary(root=>root.Id,root=>CosmicConcurrency.GetProviderRevisionId(root,session.Authority)));
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var first=New("sync parent");var second=New("sync child");second.ParentHolonId=first.Id;
        ownedIds.Add(first.Id);ownedIds.Add(second.Id);
        Timed("sync-create-root-pair",()=>{
            Assert.IsFalse(session.Add(first).IsError);
            Saved(session.SaveChanges(new CosmicWriteOptions(Guid.NewGuid())));
            Assert.IsFalse(session.Add(second).IsError);
            Saved(session.SaveChanges(new CosmicWriteOptions(Guid.NewGuid())));
            return true;
        });
        var ids=new[]{first.Id,second.Id};
        var query=session.Query<Holon>().Result.Where(row=>ids.Contains(row.Id)).Result;
        var read=Timed("sync-native-read",()=>session.Read(query));
        Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(2,read.Result.Count);
        Assert.AreSame(first,read.Result.Single(row=>row.Id==first.Id));
        Timed("sync-primary-navigation",()=>{
            var parent=session.LoadParent<Holon>(second);
            Assert.IsFalse(parent.IsError,parent.Message);Assert.AreSame(first,parent.Result);
            var children=session.LoadChildren<Holon>(first,2);
            Assert.IsFalse(children.IsError,children.Message);Assert.AreEqual(1,children.Result.Count);
            Assert.AreSame(second,children.Result[0]);return true;
        });
        first.Name="sync updated";
        Saved(Timed("sync-managed-update",()=>session.SaveChanges(new CosmicWriteOptions(Guid.NewGuid()))));
        var asynchronous=await TimedAsync("async-native-value-agreement",()=>session.ReadAsync(query,trackResults:false));
        Assert.IsFalse(asynchronous.IsError,asynchronous.Message+asynchronous.Exception);
        Assert.AreEqual("sync updated",asynchronous.Result.Single(row=>row.Id==first.Id).Name);
        Assert.AreNotSame(first,asynchronous.Result.Single(row=>row.Id==first.Id));
        var generated=New("sync generated");generated.Id=Guid.Empty;
        Saved(Timed("sync-stateless-generated-insert",()=>stateless.Insert(new[]{generated},new CosmicWriteOptions(Guid.NewGuid()))));
        Assert.AreNotEqual(Guid.Empty,generated.Id);ownedIds.Add(generated.Id);
        Assert.IsTrue(CosmicConcurrency.GetProviderRevisionId(generated,session.Authority).HasValue);
        var stale=HolonChangeTracking.Clone(generated);
        generated.Name="sync explicit update";
        Saved(Timed("sync-stateless-update",()=>stateless.Save(new[]{generated},Intent(generated))));
        stale.Name="stale must not win";
        var conflict=Timed("sync-stale-refusal",()=>stateless.Save(new[]{stale},Intent(stale)));
        Assert.IsTrue(conflict.IsError);Assert.IsFalse(conflict.IsSaved);
        Assert.IsFalse(string.IsNullOrWhiteSpace(conflict.ErrorCode));
        var generatedId=generated.Id;
        var generatedQuery=stateless.Query<Holon>().Result.Where(row=>row.Id==generatedId).Result;
        var nativeRow=Timed("sync-stateless-read",()=>stateless.Read(generatedQuery));
        Assert.IsFalse(nativeRow.IsError,nativeRow.Message);Assert.AreEqual(1,nativeRow.Result.Count);
        Assert.AreEqual("sync explicit update",nativeRow.Result[0].Name);Assert.AreNotSame(generated,nativeRow.Result[0]);
        var cancelled=New("cancelled sync insert");cancelled.Id=Guid.Empty;
        var refused=stateless.Insert(new[]{cancelled},new CosmicWriteOptions(Guid.NewGuid()),
            cancellationToken:new CancellationToken(true));
        Assert.IsTrue(refused.IsError);Assert.AreEqual("COSMIC_STATELESS_CANCELLED",refused.ErrorCode);
        Assert.AreEqual(Guid.Empty,cancelled.Id);
        var deleted=Timed("sync-stateless-delete",()=>stateless.Delete(new[]{generated},Intent(generated),softDelete:false));
        Saved(deleted);Assert.IsTrue(deleted.IsDeleted);
        Assert.IsFalse(session.Remove(first,softDelete:false).IsError);
        Assert.IsFalse(session.Remove(second,softDelete:false).IsError);
        var rootsDeleted=Timed("sync-managed-hard-delete",()=>session.SaveChanges(new CosmicWriteOptions(Guid.NewGuid())));
        Saved(rootsDeleted);Assert.IsTrue(rootsDeleted.IsDeleted);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(first).Result);
        Assert.AreEqual(CosmicEntityState.Detached,session.State(second).Result);
        var allIds=ownedIds.ToArray();
        var emptyQuery=stateless.Query<Holon>().Result.Where(row=>allIds.Contains(row.Id)).Result;
        var empty=Timed("sync-read-after-delete",()=>stateless.Read(emptyQuery));
        Assert.IsFalse(empty.IsError,empty.Message);Assert.AreEqual(0,empty.Result.Count);
        await TimedAsync("native-hard-delete-absence",async ()=>{
            foreach(var id in allIds)
            {
                var absent=await provider.LoadHolonAsync(id,loadChildren:false,recursive:false);
                Assert.IsNull(absent.Result);
                Assert.IsTrue(!absent.IsError || absent.ErrorCode==OASISResultErrorCodes.RecordNotFound,absent.Message);
            }
            return true;
        });
        return 1;
    }
}

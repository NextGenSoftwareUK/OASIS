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
    public Task StatelessSessionClosePreservesActiveOutcomeAndBorrowedProvider(string providerName)
        => RunGroupedDistinctNativeFixture(providerName,false,statelessLifetimeJourney:true);

    private static async Task<int> VerifyStatelessLifetimeJourney(IOASISStorageProvider provider,
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
        using var session=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var query=session.Query<Holon>().Result;
        async Task<T> Timed<T>(string name,Func<Task<T>> operation)
        {
            var clock=System.Diagnostics.Stopwatch.StartNew();
            try {return await operation();}
            finally {timings.Add(name,clock.ElapsedMilliseconds);}
        }
        CosmicWriteOptions Intent(IHolon item)=>new(Guid.NewGuid(),
            new Dictionary<Guid,Guid?> {{item.Id,CosmicConcurrency.GetProviderRevisionId(item,native.QueryProviderType)}});
        void Saved(OASISResult<IEnumerable<IHolon>> result)
        {Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsSaved);}
        var root=new Holon {Id=Guid.Empty,Name="lifetime original",CreatedByAvatarId=actor,
            IsPublic=false,IsActive=true,IsNewHolon=true};
        using var release=new ManualResetEventSlim(false);
        var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        IEnumerable<IHolon> Selected()
        {
            entered.TrySetResult(true);
            if(!release.Wait(TimeSpan.FromSeconds(15)))throw new TimeoutException("Owned test enumeration was not released.");
            yield return root;
        }
        await Timed("busy-disposal-refused-create",async ()=>{
            var inserting=Task.Run(()=>session.InsertAsync(Selected(),new CosmicWriteOptions(Guid.NewGuid())));
            OASISResult<IEnumerable<IHolon>> created=null;
            try
            {
                await entered.Task.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.ThrowsException<InvalidOperationException>(()=>session.Dispose());
                var busy=await session.ReadAsync(query);
                Assert.IsTrue(busy.IsError);Assert.AreEqual("COSMIC_STATELESS_BUSY",busy.ErrorCode);
            }
            finally
            {
                release.Set();created=await inserting;
                if(root.Id!=Guid.Empty && !ownedIds.Contains(root.Id))ownedIds.Add(root.Id);
            }
            Saved(created);Assert.AreNotEqual(Guid.Empty,root.Id);return true;
        });
        session.Dispose();session.Dispose();
        Assert.AreEqual("COSMIC_STATELESS_DISPOSED",session.Query<Holon>().ErrorCode);
        var enumerations=0;
        IEnumerable<IHolon> ClosedRoots(){enumerations++;yield return root;}
        void Closed<T>(OASISResult<T> result)
        {Assert.IsTrue(result.IsError);Assert.AreEqual("COSMIC_STATELESS_DISPOSED",result.ErrorCode);}
        Closed(await session.ReadAsync(query));
        Closed(await session.InsertAsync(ClosedRoots(),new CosmicWriteOptions(Guid.NewGuid())));
        Closed(await session.SaveAsync(ClosedRoots(),Intent(root)));
        Closed(await session.DeleteAsync(ClosedRoots(),Intent(root),softDelete:false));
        Closed(session.Save(ClosedRoots(),Intent(root)));
        Assert.AreEqual(0,enumerations,"Closed sessions must refuse before enumerating caller roots.");
        using var fresh=CosmicStatelessSession.Create(manager,native,CosmicQueryReadAccess.OwnerOrPublic(actor).Result).Result;
        var id=root.Id;
        var freshQuery=fresh.Query<Holon>().Result.Where(row=>row.Id==id).Result;
        var loaded=await Timed("fresh-borrower-native-read",()=>fresh.ReadAsync(freshQuery));
        Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(1,loaded.Result.Count);
        Assert.AreEqual("lifetime original",loaded.Result[0].Name);
        Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(root,native.QueryProviderType),
            CosmicConcurrency.GetProviderRevisionId(loaded.Result[0],native.QueryProviderType));
        root.Name="fresh borrower update";
        await Timed("fresh-borrower-native-update",async ()=>{
            Saved(await fresh.SaveAsync(new[]{root},Intent(root)));
            var changed=await fresh.ReadAsync(freshQuery);
            Assert.IsFalse(changed.IsError,changed.Message);Assert.AreEqual("fresh borrower update",changed.Result.Single().Name);
            return true;
        });
        await Timed("fresh-borrower-native-delete",()=>{
            var deleted=fresh.Delete(new[]{root},Intent(root),softDelete:false);
            Saved(deleted);Assert.IsTrue(deleted.IsDeleted);return Task.FromResult(true);
        });
        await Timed("native-hard-delete-absence",async ()=>{
            var absent=await provider.LoadHolonAsync(id,loadChildren:false,recursive:false);
            Assert.IsNull(absent.Result);
            Assert.IsTrue(!absent.IsError || absent.ErrorCode==OASISResultErrorCodes.RecordNotFound,absent.Message);
            return true;
        });
        return 1;
    }
}

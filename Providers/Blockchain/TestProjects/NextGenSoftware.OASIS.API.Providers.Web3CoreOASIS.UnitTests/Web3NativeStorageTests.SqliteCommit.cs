using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using SqlContext=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context.DataContext;
using DetailRepository=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories.AvatarDetailRepository;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private sealed class AfterCommitFault(IOException cause) : SaveChangesInterceptor
    {
        public override int SavedChanges(SaveChangesCompletedEventData data,int result)=>throw cause;
        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData data,int result,CancellationToken token=default)
            =>ValueTask.FromException<int>(cause);
    }
    private sealed class CommitFaultContext(string connection,AfterCommitFault interceptor) : SqlContext(connection)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {base.OnConfiguring(options);options.AddInterceptors(interceptor);}
    }
    private sealed class CommitFaultSqlite : SQLLiteDBOASIS.SQLLiteDBOASIS
    {
        public CommitFaultContext Context {get;}
        private readonly DetailRepository repository;
        private readonly SQLLiteDBOASIS.Persistence.Repositories.HolonRepository holons;
        public CommitFaultSqlite(string connection,IOException cause) : base(connection)
        {Context=new CommitFaultContext(connection,new AfterCommitFault(cause));repository=new DetailRepository(Context);holons=new(Context);}
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail detail)=>repository.SaveAvatarDetail(detail);
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)=>repository.SaveAvatarDetailAsync(detail);
        public override OASISResult<IHolon> SaveHolon(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolon(holon,saveChildren,recursive,maxChildDepth,continueOnError);
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError);
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> roots,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolons(roots,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError);
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> roots,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolonsAsync(roots,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError);
    }
    private static async Task VerifySqliteCommitFenceAsync(PolygonOASIS.PolygonOASIS polygon,SQLLiteDBOASIS.SQLLiteDBOASIS healthySqlite,Web3CoreOASIS native,
        Web4UsageExecution executor,string user,UsageEndpointPolicy policy,MongoUsageOutboxStore store)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var db=(DbContext)typeof(SQLLiteDBOASIS.SQLLiteDBOASIS).GetField("_appDataContext",flags)!.GetValue(healthySqlite)!;
        var cause=new IOException("Owned notification failure AFTER actual SQLite SaveChanges commit.");
        var faulty=new CommitFaultSqlite(db.Database.GetDbConnection().ConnectionString,cause);
        try
        {
            Assert.IsFalse((await faulty.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(faulty);manager.RegisterProvider(polygon);
            var providers=new[] {new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS),new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.PolygonOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var sdk=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!.GetValue(polygon)!;
            foreach(var asynchronous in new[] {false,true})
            foreach(var existing in new[] {false,true})
            {
                var handle=await executor.BeginAsync("local-fixture-bearer",user,"sqlite-commit-unknown-"+asynchronous+"-"+existing,new string('a',64),"local post-commit failure fixture",policy);
                var context=new PreparedStorageExecution(handle,"sqlite-commit",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                var detail=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,XP=377,Karma=68};detail.Inventory.Add(new Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="committed before notification failure"});
                if(existing)
                {
                    var seeded=await healthySqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=detail.Id,IsActive=true,XP=1,Karma=1});
                    Assert.IsFalse(seeded.IsError,seeded.Message);Assert.IsTrue(seeded.IsSaved);
                }
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0;
                var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=context};
                var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                Assert.IsTrue(saved.IsError);Assert.IsFalse(saved.IsSaved);Assert.IsNull(saved.Result);
                var failure=saved.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
                Assert.AreEqual(detail.Id,failure.RecordId);Assert.AreSame(cause,failure.InnerException);Assert.AreEqual(string.Empty,failure.OriginalTransactionId);
                Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0);
                var committed=await healthySqlite.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(committed.IsError,committed.Message);
                Assert.AreEqual(377L,committed.Result.XP);Assert.AreEqual("committed before notification failure",committed.Result.Inventory.Single().Name);
                Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(detail.Id.ToString()))));
                var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
            }
            foreach(var asynchronous in new[] {false,true})
            foreach(var batch in new[] {false,true})
            foreach(var existing in new[] {false,true})
            {
                var root=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="committed graph root"};
                var child=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="committed graph child",ParentHolonId=root.Id};
                root.Children.Add(child);
                var second=new Holon {Id=Guid.NewGuid(),IsActive=true,Name="committed second root"};
                IHolon[] roots=batch ? [root,second]:[root];
                if(existing)
                    foreach(var item in roots)
                    {
                        var seeded=await healthySqlite.SaveHolonAsync(new Holon {Id=item.Id,IsActive=true,Name="before update"},saveChildren:false);
                        Assert.IsFalse(seeded.IsError,seeded.Message);
                    }
                var handle=await executor.BeginAsync("local-fixture-bearer",user,$"sqlite-graph-commit-{asynchronous}-{batch}-{existing}",new string('a',64),"local post-commit graph fixture",policy);
                var context=new PreparedStorageExecution(handle,"sqlite-graph-commit",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0;
                var request=new Core.StorageOperationRequest {Operation=batch ? "SaveHolons":"SaveHolon",Payload=batch ? roots:root,SaveChildren=true,Recursive=true,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=context};
                var saved=asynchronous ? await router.RouteRequestAsync<object>(request):router.RouteRequest<object>(request);
                Assert.IsTrue(saved.IsError);Assert.IsFalse(saved.IsSaved);Assert.IsNull(saved.Result);
                var failure=saved.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
                Assert.AreSame(cause,failure.InnerException);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                var expected=roots.Append(child).Select(item=>item.Id).ToArray();
                CollectionAssert.AreEquivalent(expected,failure.RecordIds.ToArray());
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0);
                foreach(var item in roots.Append(child))
                {
                    var committed=await healthySqlite.LoadHolonAsync(item.Id,loadChildren:false);
                    Assert.IsFalse(committed.IsError,committed.Message);Assert.AreEqual(item.Name,committed.Result.Name);
                    Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetHolonByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(item.Id.ToString()))));
                }
                var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();
                Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
            }
        }
        finally {faulty.Context.Dispose();faulty.Dispose();}
    }
}

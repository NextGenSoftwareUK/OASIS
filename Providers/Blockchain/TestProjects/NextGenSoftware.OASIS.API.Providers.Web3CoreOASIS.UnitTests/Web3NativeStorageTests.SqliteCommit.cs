using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Newtonsoft.Json.Linq;
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
    private sealed class CommitFaultContext(string connection,IInterceptor interceptor) : SqlContext(connection)
    {
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {base.OnConfiguring(options);options.AddInterceptors(interceptor);}
    }
    private sealed class AfterTransactionCommitFault(IOException cause) : DbTransactionInterceptor
    {
        public override void TransactionCommitted(System.Data.Common.DbTransaction transaction,TransactionEndEventData data)=>throw cause;
        public override Task TransactionCommittedAsync(System.Data.Common.DbTransaction transaction,TransactionEndEventData data,CancellationToken token=default)
            =>Task.FromException(cause);
    }
    private sealed class DeleteCommitFaultSqlite : SQLLiteDBOASIS.SQLLiteDBOASIS,IOASISStorageProvider,IAvatarTombstoneStorageProvider
    {
        public CommitFaultContext Context {get;}
        private readonly DetailRepository repository;
        private readonly SQLLiteDBOASIS.Persistence.Repositories.AvatarRepository avatars;
        public DeleteCommitFaultSqlite(string connection,IOException cause) : base(connection)
        {Context=new(connection,new AfterTransactionCommitFault(cause));repository=new(Context);avatars=new(Context);}
        public new OASISResult<bool> DeleteAvatarDetail(Guid id,bool softDelete=true)=>repository.DeleteAvatarDetail(id,softDelete);
        public new Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id,bool softDelete=true)=>repository.DeleteAvatarDetailAsync(id,softDelete);
        public override OASISResult<bool> DeleteAvatar(Guid id,bool softDelete=true)=>avatars.DeleteAvatar(id,softDelete);
        public override Task<OASISResult<bool>> DeleteAvatarAsync(Guid id,bool softDelete=true)=>avatars.DeleteAvatarAsync(id,softDelete);
        public new OASISResult<bool> ApplyAvatarTombstone(Core.Objects.AvatarTombstone tombstone)=>avatars.ApplyAvatarTombstone(tombstone);
        public new Task<OASISResult<bool>> ApplyAvatarTombstoneAsync(Core.Objects.AvatarTombstone tombstone)=>avatars.ApplyAvatarTombstoneAsync(tombstone);
    }
    private sealed class CommitFaultSqlite : SQLLiteDBOASIS.SQLLiteDBOASIS
    {
        public CommitFaultContext Context {get;}
        private readonly DetailRepository repository;
        private readonly SQLLiteDBOASIS.Persistence.Repositories.HolonRepository holons;
        private readonly SQLLiteDBOASIS.Persistence.Repositories.AvatarRepository avatars;
        public CommitFaultSqlite(string connection,IOException cause) : base(connection)
        {Context=new CommitFaultContext(connection,new AfterCommitFault(cause));repository=new DetailRepository(Context);holons=new(Context);avatars=new(Context);}
        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail detail)=>repository.SaveAvatarDetail(detail);
        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)=>repository.SaveAvatarDetailAsync(detail);
        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)=>avatars.SaveAvatar(avatar);
        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)=>avatars.SaveAvatarAsync(avatar);
        public override OASISResult<IHolon> SaveHolon(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolon(holon,saveChildren,recursive,maxChildDepth,continueOnError);
        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolonAsync(holon,saveChildren,recursive,maxChildDepth,continueOnError);
        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> roots,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolons(roots,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError);
        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> roots,bool saveChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool saveChildrenOnProvider=false)
            =>holons.SaveHolonsAsync(roots,saveChildren,recursive,maxChildDepth,curentChildDepth,continueOnError);
        public override OASISResult<IHolon> DeleteHolon(Guid id)=>holons.DeleteHolon(id,false);
        public override OASISResult<IHolon> DeleteHolon(string key)=>holons.DeleteHolon(key,false);
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)=>holons.DeleteHolonAsync(id,false);
        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string key)=>holons.DeleteHolonAsync(key,false);
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
            foreach(var existing in new[] {false,true})
            {
                var avatar=new Avatar {Id=Guid.NewGuid(),IsActive=true,Username="commit-"+Guid.NewGuid().ToString("N"),FirstName="committed avatar",AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                if(existing)
                {
                    var seeded=await healthySqlite.SaveAvatarAsync(avatar);Assert.IsFalse(seeded.IsError,seeded.Message);
                    avatar.FirstName="committed updated avatar";
                }
                var handle=await executor.BeginAsync("local-fixture-bearer",user,$"sqlite-avatar-commit-{asynchronous}-{existing}",new string('a',64),"local post-commit avatar fixture",policy);
                var context=new PreparedStorageExecution(handle,"sqlite-avatar-commit",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0;
                var request=new Core.StorageOperationRequest {Operation="SaveAvatar",Payload=avatar,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=context};
                var saved=asynchronous ? await router.RouteRequestAsync<IAvatar>(request):router.RouteRequest<IAvatar>(request);
                Assert.IsTrue(saved.IsError);Assert.IsFalse(saved.IsSaved);Assert.IsNull(saved.Result);
                var failure=saved.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
                Assert.AreSame(cause,failure.InnerException);CollectionAssert.AreEqual(new[] {avatar.Id},failure.RecordIds.ToArray());
                Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0);
                var committed=await healthySqlite.LoadAvatarAsync(avatar.Id);Assert.IsFalse(committed.IsError,committed.Message);
                Assert.AreEqual(avatar.FirstName,committed.Result.FirstName);
                Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(avatar.Id.ToString()))));
                var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();
                Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
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
            foreach(var asynchronous in new[] {false,true})
            foreach(var byKey in new[] {false,true})
            foreach(var soft in new[] {false,true})
            {
                var holon=new Holon {Id=Guid.NewGuid(),Name="committed holon deletion",IsActive=true};
                var key="delete-"+holon.Id.ToString("N");holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.SQLLiteDBOASIS]=key;
                Assert.IsFalse((await healthySqlite.SaveHolonAsync(holon,saveChildren:false)).IsError);
                Assert.IsFalse((await polygon.SaveHolonAsync(holon,saveChildren:false)).IsError);
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0;
                OASISResult<IHolon> result;
                if(soft)
                {
                    var repository=new SQLLiteDBOASIS.Persistence.Repositories.HolonRepository(faulty.Context);
                    result=byKey ? asynchronous ? await repository.DeleteHolonAsync(key,true):repository.DeleteHolon(key,true)
                        : asynchronous ? await repository.DeleteHolonAsync(holon.Id,true):repository.DeleteHolon(holon.Id,true);
                }
                else
                {
                    var handle=await executor.BeginAsync("local-fixture-bearer",user,$"sqlite-holon-delete-{asynchronous}-{byKey}",new string('a',64),"local post-commit holon deletion",policy);
                    var context=new PreparedStorageExecution(handle,"sqlite-holon-delete",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                    var request=new Core.StorageOperationRequest {Operation=byKey ? "DeleteHolonByProviderKey":"DeleteHolon",HolonId=holon.Id,ProviderKey=key,SoftDelete=false,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=context};
                    result=asynchronous ? await router.RouteRequestAsync<IHolon>(request):router.RouteRequest<IHolon>(request);
                    var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
                }
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsNull(result.Result);
                var failure=result.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
                Assert.AreSame(cause,failure.InnerException);Assert.AreEqual(holon.Id,failure.RecordId);
                Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0);
                using var control=new SqlContext(db.Database.GetDbConnection().ConnectionString);
                var stored=await control.Holons.AsNoTracking().SingleOrDefaultAsync(row=>row.Id==holon.Id.ToString());
                if(soft) {Assert.IsNotNull(stored);Assert.IsFalse(stored.IsActive);Assert.AreNotEqual(DateTime.MinValue,stored.DeletedDate);}
                else Assert.IsNull(stored);
                var untouched=await polygon.LoadHolonAsync(holon.Id,loadChildren:false);Assert.IsFalse(untouched.IsError,untouched.Message);Assert.IsTrue(untouched.Result.IsActive);
                // Explicit fixture cleanup only AFTER proving routing left this native copy untouched.
                Assert.IsFalse((await polygon.DeleteHolonAsync(holon.Id)).IsError);
            }
        }
        finally {faulty.Context.Dispose();faulty.Dispose();}
        var deletionCause=new IOException("Owned notification failure AFTER actual SQLite transaction commit.");
        var deletion=new DeleteCommitFaultSqlite(db.Database.GetDbConnection().ConnectionString,deletionCause);
        try
        {
            Assert.IsFalse((await deletion.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(deletion);manager.RegisterProvider(polygon);
            var providers=new[] {new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS),new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.PolygonOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var sdk=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!.GetValue(polygon)!;
            foreach(var asynchronous in new[] {false,true})
            foreach(var soft in new[] {false,true})
            foreach(var pair in new[] {false,true})
            {
                var detail=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,XP=81};
                detail.Inventory.Add(new Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="detail deletion dependent"});
                Assert.IsFalse((await healthySqlite.SaveAvatarDetailAsync(detail)).IsError);
                Assert.IsFalse((await polygon.SaveAvatarDetailAsync(detail)).IsError);
                if(pair)
                {
                    var avatar=new Avatar {Id=detail.Id,IsActive=true,Username="delete-pair-"+detail.Id.ToString("N"),AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                    Assert.IsFalse((await healthySqlite.SaveAvatarAsync(avatar)).IsError);
                    Assert.IsFalse((await polygon.SaveAvatarAsync(avatar)).IsError);
                }
                var handle=await executor.BeginAsync("local-fixture-bearer",user,$"sqlite-detail-delete-commit-{asynchronous}-{soft}-{pair}",new string('a',64),"local post-commit deletion fixture",policy);
                var context=new PreparedStorageExecution(handle,"sqlite-detail-delete-commit",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0;
                var request=new Core.StorageOperationRequest {Operation=pair ? "DeleteAvatar":"DeleteAvatarDetail",AvatarId=detail.Id,SoftDelete=soft,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=context};
                var result=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);
                var failure=result.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
                Assert.AreSame(deletionCause,failure.InnerException);Assert.AreEqual(detail.Id,failure.RecordId);
                Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0);
                using var control=new SqlContext(db.Database.GetDbConnection().ConnectionString);
                var stored=await control.AvatarDetails.AsNoTracking().SingleOrDefaultAsync(row=>row.Id==detail.Id.ToString());
                if(soft) {Assert.IsNotNull(stored);Assert.IsFalse(stored.IsActive);Assert.AreNotEqual(DateTime.MinValue,stored.DeletedDate);}
                else Assert.IsNull(stored);
                if(pair)
                {
                    var storedAvatar=await control.Avatars.AsNoTracking().SingleOrDefaultAsync(row=>row.Id==detail.Id.ToString());
                    if(soft) {Assert.IsNotNull(storedAvatar);Assert.IsFalse(storedAvatar.IsActive);Assert.AreEqual(stored!.DeletedDate,storedAvatar.DeletedDate);}
                    else Assert.IsNull(storedAvatar);
                    var nativeAvatar=await polygon.LoadAvatarAsync(detail.Id);Assert.IsFalse(nativeAvatar.IsError,nativeAvatar.Message);
                    Assert.IsTrue(nativeAvatar.Result.IsActive);
                }
                var untouched=await polygon.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(untouched.IsError,untouched.Message);
                Assert.IsTrue(untouched.Result.IsActive);Assert.AreEqual(81L,untouched.Result.XP);
                var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();
                Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
            }
            foreach(var asynchronous in new[] {false,true})
            {
                var id=Guid.NewGuid();
                var avatar=new Avatar {Id=id,IsActive=true,Username="replica-commit-"+id.ToString("N"),AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                Assert.IsFalse((await healthySqlite.SaveAvatarAsync(avatar)).IsError);
                Assert.IsFalse((await healthySqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=93})).IsError);
                var tombstone=new Core.Objects.AvatarTombstone(id,DateTime.UtcNow.AddTicks(17),Guid.NewGuid());
                IAvatarTombstoneStorageProvider capability=deletion;
                var applied=asynchronous ? await capability.ApplyAvatarTombstoneAsync(tombstone):capability.ApplyAvatarTombstone(tombstone);
                Assert.IsTrue(applied.IsError);Assert.IsFalse(applied.Result);Assert.IsFalse(applied.IsSaved);Assert.IsFalse(applied.IsDeleted);
                var failure=applied.Exception as SQLLiteDBOASIS.SqliteMutationReconciliationException;Assert.IsNotNull(failure);
                Assert.AreEqual(id,failure.RecordId);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                Assert.IsTrue(ReferenceEquals(deletionCause,failure.InnerException)
                    || failure.InnerException is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(deletionCause));
                using var control=new SqlContext(db.Database.GetDbConnection().ConnectionString);
                var storedAvatar=await control.Avatars.AsNoTracking().SingleAsync(row=>row.Id==id.ToString());
                var storedDetail=await control.AvatarDetails.AsNoTracking().SingleAsync(row=>row.Id==id.ToString());
                Assert.IsFalse(storedAvatar.IsActive);Assert.IsFalse(storedDetail.IsActive);
                Assert.AreEqual(tombstone.DeletedAtUtc,storedAvatar.DeletedDate);Assert.AreEqual(tombstone.DeletedAtUtc,storedDetail.DeletedDate);
                Assert.AreEqual(tombstone.DeletedByAvatarId.ToString(),storedAvatar.DeletedByAvatarId);
                Assert.AreEqual(storedAvatar.DeletedByAvatarId,storedDetail.DeletedByAvatarId);
            }
            foreach(var asynchronous in new[] {false,true})
            {
                var id=Guid.NewGuid();
                var avatar=new Avatar {Id=id,IsActive=true,Username="routed-replica-commit-"+id.ToString("N"),AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                var detail=new AvatarDetail {Id=id,IsActive=true,XP=94};
                foreach(IOASISStorageProvider provider in new IOASISStorageProvider[] {healthySqlite,polygon})
                {Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);}
                var handle=await executor.BeginAsync("local-fixture-bearer",user,"sqlite-replica-commit-"+asynchronous,new string('a',64),"local routed replica post-commit fixture",policy);
                var context=new PreparedStorageExecution(handle,"sqlite-replica-commit",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS,PreparedExecution=context};
                var deleted=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
                Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.Result);Assert.IsTrue(deleted.IsWarning);
                var diagnostic=router.LastReplicationDiagnostic;Assert.AreEqual(1,diagnostic.FailedCount);
                var failure=diagnostic.Attempts.Single();Assert.AreEqual(Core.Enums.ProviderType.SQLLiteDBOASIS,failure.Provider);
                Assert.IsFalse(failure.Succeeded);Assert.IsTrue(failure.ReconciliationRequired);Assert.AreEqual(0,failure.OriginalTransactionIds.Count);
                var serialized=Newtonsoft.Json.Linq.JObject.Parse(deleted.MetaData["hyperDriveReplicationDiagnostic"])["Attempts"]!.Single();
                Assert.IsTrue(serialized["ReconciliationRequired"]!.Value<bool>());Assert.AreEqual(0,serialized["OriginalTransactionIds"]!.Count());
                Assert.IsTrue(Core.Helpers.AvatarTombstoneEvidence.TryGet(deleted,out var tombstone));
                using var control=new SqlContext(db.Database.GetDbConnection().ConnectionString);
                var storedAvatar=await control.Avatars.AsNoTracking().SingleAsync(row=>row.Id==id.ToString());
                var storedDetail=await control.AvatarDetails.AsNoTracking().SingleAsync(row=>row.Id==id.ToString());
                Assert.IsFalse(storedAvatar.IsActive);Assert.IsFalse(storedDetail.IsActive);
                Assert.AreEqual(tombstone.DeletedAtUtc,storedAvatar.DeletedDate);Assert.AreEqual(tombstone.DeletedAtUtc,storedDetail.DeletedDate);
                var attempts=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts;
                Assert.AreEqual(2,attempts.Count);Assert.AreEqual(1,attempts.Count(a=>a.Provider=="PolygonOASIS" && a.NativePreparation!=null));
                Assert.AreEqual(1,attempts.Count(a=>a.Provider=="SQLLiteDBOASIS" && a.NativePreparation==null));
            }
        }
        finally {deletion.Context.Dispose();deletion.Dispose();}
        await VerifySqliteWalletCommitFenceAsync(healthySqlite);
    }
}

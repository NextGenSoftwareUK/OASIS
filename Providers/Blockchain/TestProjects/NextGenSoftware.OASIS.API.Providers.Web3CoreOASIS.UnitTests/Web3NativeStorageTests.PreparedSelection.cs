using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.Utilities;
using Microsoft.EntityFrameworkCore;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyPreparedSelectionAsync(PolygonOASIS.PolygonOASIS polygon,SQLLiteDBOASIS.SQLLiteDBOASIS sqlite,Web3CoreOASIS native,
        Core.Managers.ProviderManager manager,Core.Managers.OASISHyperDrive.OASISHyperDrive router,
        Web4UsageExecution executor,string user,UsageEndpointPolicy policy,MongoUsageOutboxStore store)
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var field=typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!;var healthy=(Web3CoreOASIS)field.GetValue(polygon)!;
        var types=new[] {Core.Enums.ProviderType.PolygonOASIS,Core.Enums.ProviderType.SQLLiteDBOASIS};var sequence=0;
        Task<UsageExecutionHandle> Begin(string phase)=>executor.BeginAsync("local-fixture-bearer",user,"prepared-"+phase+"-"+(++sequence),new string('f',64),"local prepared selection fixture",policy);
        PreparedStorageExecution Context(UsageExecutionHandle handle)=>new(handle,"selected-workflow",
            new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,healthy.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
        manager.IsAutoReplicationEnabled=false;manager.IsAutoLoadBalanceEnabled=true;manager.IsAutoFailOverEnabled=false;
        Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(types.Select(type=>new EnumValue<Core.Enums.ProviderType>(type))).IsError);
        var roundRobin=new Dictionary<Core.Enums.ProviderType,int>();
        var cases=Enum.GetValues<Core.Enums.LoadBalancingStrategy>().SelectMany(strategy=>new[] {(strategy,false),(strategy,true)})
            .Concat(Enumerable.Range(0,6).Select(index=>(Core.Enums.LoadBalancingStrategy.RoundRobin,index%2==0))).ToArray();
        for(var index=0;index<cases.Length;index++)
        {
            var (strategy,asynchronous)=cases[index];var handle=await Begin("balance");var id=Guid.NewGuid();
            var before=types.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
            var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true,XP=701},PreparedExecution=Context(handle)};
            var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request,strategy):router.RouteRequest<IAvatarDetail>(request,strategy);
            Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
            var diagnostic=manager.LastProviderSelectionDiagnostic;Assert.AreEqual(strategy,diagnostic.RequestedStrategy);Assert.AreEqual(2,diagnostic.Candidates.Count);
            var selected=diagnostic.SelectedProvider;
            foreach(var type in types) Assert.AreEqual(before[type]+(selected==type ? 1:0),manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
            var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();
            Assert.AreEqual(selected.ToString(),attempt.Provider);Assert.AreEqual(selected==Core.Enums.ProviderType.PolygonOASIS,attempt.NativePreparation!=null);
            var selectedStore=selected==Core.Enums.ProviderType.PolygonOASIS ? (IOASISStorageProvider)polygon:sqlite;
            var other=selected==Core.Enums.ProviderType.PolygonOASIS ? (IOASISStorageProvider)sqlite:polygon;
            Assert.AreEqual(701L,(await selectedStore.LoadAvatarDetailAsync(id)).Result.XP);Assert.IsNull((await other.LoadAvatarDetailAsync(id)).Result);
            if(index>=cases.Length-6) roundRobin[selected]=roundRobin.GetValueOrDefault(selected)+1;
        }
        Assert.AreEqual(3,roundRobin[Core.Enums.ProviderType.PolygonOASIS]);Assert.AreEqual(3,roundRobin[Core.Enums.ProviderType.SQLLiteDBOASIS]);
        manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoFailOverEnabled=true;
        Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types.Select(type=>new EnumValue<Core.Enums.ProviderType>(type))).IsError);
        var address=(string)typeof(Web3CoreOASISBaseProvider).GetField("_contractAddress",flags)!.GetValue(polygon)!;
        field.SetValue(polygon,new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",address,Web3CoreOASISBaseProviderHelper.Abi,31337));
        try
        {
            foreach(var asynchronous in new[] {false,true})
            {
                var handle=await Begin("preflight");var id=Guid.NewGuid();
                var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true,XP=905},PreferredProvider=Core.Enums.ProviderType.PolygonOASIS,PreparedExecution=Context(handle)};
                var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(905L,(await sqlite.LoadAvatarDetailAsync(id)).Result.XP);
                var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();
                Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
                Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()))));
            }
        }
        finally {field.SetValue(polygon,healthy);}
        manager.IsAutoReplicationEnabled=true;
        await using(var proxy=new Web3AcceptedWriteProxy())
        {
            field.SetValue(polygon,new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",address,Web3CoreOASISBaseProviderHelper.Abi,31337));
            try
            {
                foreach(var asynchronous in new[] {false,true})
                {
                    var handle=await Begin("unknown");var id=Guid.NewGuid();var accepted=proxy.AcceptedHashes.Count;
                    var backupCalls=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                    var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true,XP=582},PreferredProvider=Core.Enums.ProviderType.PolygonOASIS,PreparedExecution=Context(handle)};
                    var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                    Assert.IsTrue(saved.IsError);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(saved.Exception));
                    Assert.AreEqual(accepted+1,proxy.AcceptedHashes.Count);Assert.AreEqual(backupCalls,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                    Assert.IsNull((await sqlite.LoadAvatarDetailAsync(id)).Result);
                    var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();
                    Assert.AreEqual("PolygonOASIS",attempt.Provider);Assert.AreEqual(proxy.AcceptedHashes.Last(),attempt.ProviderRequestId);
                    Assert.AreEqual(582L,polygon.DecodePrivateEntity<AvatarDetail>("detail",await native.GetAvatarDetailByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()))).XP);
                }
            }
            finally {field.SetValue(polygon,healthy);}
        }
        manager.IsAutoReplicationEnabled=false;
        var reverseId=Guid.NewGuid();
        Assert.IsFalse((await polygon.SaveAvatarDetailAsync(new AvatarDetail {Id=reverseId,IsActive=true,XP=777})).IsError);
        Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=reverseId,IsActive=true,XP=999})).IsError);
        Assert.IsFalse((await router.RouteRequestAsync<IAvatarDetail>(new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=reverseId,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS})).IsError);
        var db=(DbContext)typeof(SQLLiteDBOASIS.SQLLiteDBOASIS).GetField("_appDataContext",flags)!.GetValue(sqlite)!;
        var connectionString=db.Database.GetDbConnection().ConnectionString;
        var source=new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString).DataSource;
        Assert.AreEqual(Path.GetFullPath("TestResults/Web3CoreRouting/routing.db"),Path.GetFullPath(source),"Only the owned fixed test database may be locked.");
        var priorTimeout=db.Database.GetCommandTimeout();db.Database.SetCommandTimeout(1);
        var reverseWrites=new List<Guid>();
        using(var control=new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
        {
            await control.OpenAsync();using var command=control.CreateCommand();command.CommandTimeout=1;
            command.CommandText="PRAGMA journal_mode";var priorJournal=(string)(await command.ExecuteScalarAsync())!;
            Assert.IsTrue(priorJournal is "wal" or "delete");var locked=false;
            try
            {
                command.CommandText="PRAGMA journal_mode=DELETE";await command.ExecuteScalarAsync();
                command.CommandText="BEGIN EXCLUSIVE";await command.ExecuteNonQueryAsync();locked=true;
                var failedRead=await sqlite.LoadAvatarDetailAsync(reverseId);
                Assert.IsTrue(failedRead.IsError);Assert.IsInstanceOfType(failedRead.Exception,typeof(Microsoft.Data.Sqlite.SqliteException));
                Assert.AreEqual(5,((Microsoft.Data.Sqlite.SqliteException)failedRead.Exception).SqliteErrorCode);
                foreach(var asynchronous in new[] {false,true})
                {
                    var handle=await Begin("sqlite-read-outage");var before=types.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    var request=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=reverseId,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=Context(handle)};
                    var loaded=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                    Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(777L,loaded.Result.XP);
                    foreach(var type in types) Assert.AreEqual(before[type]+1,manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    Assert.AreEqual(0,(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Count,"Read-only routing must not invent mutation attempts.");
                }
                foreach(var asynchronous in new[] {false,true})
                {
                    var handle=await Begin("sqlite-prewrite-outage");var id=Guid.NewGuid();reverseWrites.Add(id);
                    var before=types.ToDictionary(type=>type,type=>manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true,XP=333},PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=Context(handle)};
                    var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                    Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(id,saved.Result.Id);
                    foreach(var type in types) Assert.AreEqual(before[type]+1,manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
                    var attempts=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts;
                    Assert.AreEqual(2,attempts.Count);Assert.AreEqual(2,attempts.Select(attempt=>attempt.ClientRequestId).Distinct().Count());
                    Assert.AreEqual("SQLLiteDBOASIS",attempts[0].Provider);Assert.IsNull(attempts[0].NativePreparation);
                    Assert.AreEqual("PolygonOASIS",attempts[1].Provider);Assert.IsNotNull(attempts[1].NativePreparation);
                    Assert.AreEqual(333L,polygon.DecodePrivateEntity<AvatarDetail>("detail",await native.GetAvatarDetailByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()))).XP);
                }
            }
            finally
            {
                if(locked) {command.CommandText="ROLLBACK";await command.ExecuteNonQueryAsync();}
                if(priorJournal=="wal") {command.CommandText="PRAGMA journal_mode=WAL";await command.ExecuteScalarAsync();}
                db.Database.SetCommandTimeout(priorTimeout);
            }
        }
        Assert.AreEqual(999L,(await sqlite.LoadAvatarDetailAsync(reverseId)).Result.XP);
        foreach(var id in reverseWrites) Assert.IsNull((await sqlite.LoadAvatarDetailAsync(id)).Result,"Pre-write SQLite lock failure must leave no row after unlock.");
    }
}

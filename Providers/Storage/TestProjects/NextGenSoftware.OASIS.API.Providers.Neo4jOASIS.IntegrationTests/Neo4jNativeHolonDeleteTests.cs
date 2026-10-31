using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using SQLiteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false,false)] [DataRow(true,false,false)]
    [DataRow(false,true,false)] [DataRow(true,true,false)]
    [DataRow(false,true,true)] [DataRow(true,true,true)]
    public async Task NativeHolonDeleteRejectsCommittedConcurrentSnapshotOrSelectorChange(bool synchronous,bool byKey,bool changeKey)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"holon-delete-race.db");
        Assert.IsFalse(File.Exists(path),"Existing race fixture requires reconciliation, not reset.");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var control=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await control.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");
        using var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var id=Guid.NewGuid();var verified=false;
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await neo.SaveHolonAsync(new Holon {Id=id,Name="observed old native"},saveChildren:false)).IsError);
            Assert.IsFalse((await backup.SaveHolonAsync(new Holon {Id=id,Name="untouched backup"},saveChildren:false)).IsError);
            var updated=(await neo.LoadHolonAsync(id,loadChildren:false)).Result;
            var original=(await (await control.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot",new {id=id.ToString()})).SingleAsync())["snapshot"].As<string>();
            if(!changeKey) updated.Name="committed concurrent winner";
            var snapshot=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(updated);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(neo);manager.RegisterProvider(backup);
            var stores=new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            var router=new OASISHyperDrive(manager);
            var request=new StorageOperationRequest {Operation=byKey ? "DeleteHolonByProviderKey":"DeleteHolon",HolonId=id,ProviderKey=id.ToString(),SoftDelete=false,PreferredProvider=ProviderType.Neo4jOASIS};
            using var held=await control.BeginTransactionAsync();
            Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>>? deletion=null;
            var committed=false;
            try
            {
                await (await held.RunAsync("MATCH (h:Holon {Id:$id}) SET h.__heldTestLock=true REMOVE h.__heldTestLock RETURN h.Id",new {id=id.ToString()})).ConsumeAsync();
                deletion=synchronous ? Task.Run(()=>router.RouteRequest<IHolon>(request)):router.RouteRequestAsync<IHolon>(request);
                var deadline=DateTime.UtcNow.AddSeconds(10);var blocked=false;
                do
                {
                    using var inspection=raw.AsyncSession();
                    var rows=await (await inspection.RunAsync("SHOW TRANSACTIONS YIELD currentQuery,status WHERE currentQuery CONTAINS '__oasisHolonLock' AND currentQuery CONTAINS 'DETACH DELETE' RETURN status")).ToListAsync();
                    blocked=rows.Any(row=>row["status"].As<string>().StartsWith("Blocked",StringComparison.OrdinalIgnoreCase));
                    if(!blocked) await Task.Delay(50);
                } while(!blocked && !deletion.IsCompleted && DateTime.UtcNow<deadline);
                Assert.IsTrue(blocked,"Actual delete must reach the native write lock before concurrent update commits.");
                var query=changeKey ? "MATCH (h:Holon {Id:$id}) SET h.ProviderKey=$key RETURN h.Id"
                    :"MATCH (h:Holon {Id:$id}) SET h.SnapshotJson=$snapshot RETURN h.Id";
                await (await held.RunAsync(query,new {id=id.ToString(),key="concurrently-renamed-owned-key",snapshot})).ConsumeAsync();
                await held.CommitAsync();committed=true;
                var result=await deletion.WaitAsync(TimeSpan.FromSeconds(15));
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,typeof(StorageLifecycleConflictException));
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
                Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception),"Acknowledged compare failure rolled back, not an unknown commit.");
                Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                using var read=raw.AsyncSession();
                var retained=(await (await read.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS snapshot,h.ProviderKey AS key",new {id=id.ToString()})).SingleAsync());
                Assert.AreEqual(changeKey ? original:snapshot,retained["snapshot"].As<string>());
                Assert.AreEqual(changeKey ? "concurrently-renamed-owned-key":id.ToString(),retained["key"].As<string>());
                Assert.AreEqual("untouched backup",(await backup.LoadHolonAsync(id,loadChildren:false)).Result.Name);
                verified=true;
            }
            finally
            {
                if(!committed) await held.RollbackAsync();
                if(deletion!=null) await deletion.WaitAsync(TimeSpan.FromSeconds(15));
            }
        }
        finally
        {
            await neo.DeActivateProviderAsync();backup.Dispose();
            if(verified)
            {
                await (await control.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h",new {id=id.ToString()})).ConsumeAsync();
                File.Delete(path);
            }
        }
    }

    [DataTestMethod]
    [DataRow(false,false,false,false)] [DataRow(true,false,false,false)]
    [DataRow(false,true,false,false)] [DataRow(true,true,false,false)]
    [DataRow(false,false,true,false)] [DataRow(true,false,true,false)]
    [DataRow(false,true,true,false)] [DataRow(true,true,true,false)]
    [DataRow(false,false,false,true)] [DataRow(true,false,false,true)]
    [DataRow(false,true,false,true)] [DataRow(true,true,false,true)]
    public async Task NativeHolonDeleteRetainsSnapshotReplicatesAndFencesLostCommit(bool synchronous,bool byKey,bool sqlitePrimary,bool lostCommit)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"holon-delete.db");
        Assert.IsFalse(File.Exists(path),"Retained delete fixture requires reconciliation; refusing reset.");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (h:Holon) RETURN count(h) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");
        using var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var verified=false;var id=Guid.NewGuid();
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            foreach(var store in new IOASISStorageProvider[] {neo,backup})
            {
                var record=new Holon {Id=id,Name=ReferenceEquals(store,neo) ? "native original":"sqlite original",Description="preserved deleted snapshot",Version=7};
                record.MetaData["owned-marker"]="preserved metadata";
                Assert.IsFalse((await store.SaveHolonAsync(record,saveChildren:false)).IsError);
            }
            var expected=(await (sqlitePrimary ? (IOASISStorageProvider)backup:neo).LoadHolonAsync(id,loadChildren:false)).Result;
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=true};
            manager.RegisterProvider(neo);manager.RegisterProvider(backup);
            var stores=new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(stores).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(stores).IsError);
            var router=new OASISHyperDrive(manager);
            var field=typeof(Provider).GetField("_driver",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var healthy=(IDriver)field.GetValue(neo)!;var commits=0;
            var cause=new IOException("Owned loss after actual hard-delete commit.");
            if(lostCommit) field.SetValue(neo,Neo4jCommitFaultProxy.Wrap(healthy,cause,()=>commits++));
            NextGenSoftware.OASIS.Common.OASISResult<IHolon> result;
            try
            {
                var request=new StorageOperationRequest {Operation=byKey ? "DeleteHolonByProviderKey":"DeleteHolon",HolonId=id,ProviderKey=id.ToString(),SoftDelete=false,
                    PreferredProvider=sqlitePrimary ? ProviderType.SQLLiteDBOASIS:ProviderType.Neo4jOASIS};
                result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
            }
            finally {field.SetValue(neo,healthy);}
            if(lostCommit)
            {
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,typeof(Neo4jMutationReconciliationException));
                Assert.AreSame(cause,result.Exception.InnerException);Assert.AreEqual(1,commits);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
                Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                Assert.AreEqual("sqlite original",(await backup.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            }
            else
            {
                Assert.IsFalse(result.IsError,result.Message);Assert.IsFalse(result.IsWarning,result.Message);
                Assert.IsTrue(result.IsDeleted);Assert.AreEqual(1,result.DeletedCount);
                if(!sqlitePrimary) Assert.IsTrue(result.IsSaved);
                Assert.AreEqual(expected.Name,result.Result.Name);Assert.AreEqual(expected.Description,result.Result.Description);
                Assert.AreEqual(expected.Version,result.Result.Version);
                Assert.AreEqual(expected.MetaData["owned-marker"].ToString(),result.Result.MetaData["owned-marker"].ToString());
                Assert.IsNull((await backup.LoadHolonAsync(id,loadChildren:false)).Result);
            }
            Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new {id=id.ToString()})).SingleAsync())["count"].As<long>());
            var missing=byKey ? await neo.DeleteHolonAsync(id.ToString()):await neo.DeleteHolonAsync(id);
            Assert.IsTrue(missing.IsError);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);
            Assert.IsFalse(missing.IsDeleted);Assert.IsNull(missing.Result);
            verified=true;
        }
        finally
        {
            await neo.DeActivateProviderAsync();backup.Dispose();
            if(verified) File.Delete(path);
        }
    }
}

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

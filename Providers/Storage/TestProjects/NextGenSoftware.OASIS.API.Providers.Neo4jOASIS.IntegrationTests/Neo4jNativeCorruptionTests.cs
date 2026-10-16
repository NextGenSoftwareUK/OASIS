using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
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
    [DataRow(false,"Avatar")] [DataRow(true,"Avatar")]
    [DataRow(false,"AvatarDetail")] [DataRow(true,"AvatarDetail")]
    [DataRow(false,"Holon")] [DataRow(true,"Holon")]
    public async Task NativeCorruptSnapshotsNeverFallBackToStaleSqlite(bool synchronous,string label)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        Assert.IsTrue(new[] {"Avatar","AvatarDetail","Holon"}.Contains(label));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"corruption.db");
        Assert.IsFalse(File.Exists(path),"Existing failed fixture requires reconciliation, not reset.");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail OR n:Holon RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");
        using var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var id=Guid.NewGuid();var verified=false;
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            if(label=="Avatar") Assert.IsFalse((await neo.SaveAvatarAsync(new Avatar {Id=id,Username="owned primary"})).IsError);
            else if(label=="AvatarDetail") Assert.IsFalse((await neo.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned primary",XP=7})).IsError);
            else Assert.IsFalse((await neo.SaveHolonAsync(new Holon {Id=id,Name="owned primary"},saveChildren:false)).IsError);
            Assert.IsFalse((await backup.SaveAvatarAsync(new Avatar {Id=id,Username="stale avatar",AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError);
            Assert.IsFalse((await backup.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="stale detail",XP=41})).IsError);
            Assert.IsFalse((await backup.SaveHolonAsync(new Holon {Id=id,Name="stale holon"},saveChildren:false)).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true};
            manager.RegisterProvider(neo);manager.RegisterProvider(backup);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            async Task Check<T>(string operation)
            {
                var request=new StorageOperationRequest {Operation=operation,HolonId=id,AvatarId=id,LoadChildren=false,PreferredProvider=ProviderType.Neo4jOASIS};
                var result=synchronous ? router.RouteRequest<T>(request):await router.RouteRequestAsync<T>(request);
                Assert.IsTrue(result.IsError,result.Message);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            foreach(var snapshot in new[] {"not-json",$"{{\"Id\":\"{id}\",\"Version\":\"not-an-integer\"}}",
                $"{{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"Id\":\"{id}\"}}"})
            {
                await (await session.RunAsync("MATCH (n:"+label+" {Id:$id}) SET n.SnapshotJson=$snapshot RETURN n.Id",new {id=id.ToString(),snapshot})).ConsumeAsync();
                if(label=="Avatar") {await Check<IAvatar>("LoadAvatar");await Check<IEnumerable<IAvatar>>("LoadAllAvatars");}
                else if(label=="AvatarDetail") {await Check<IAvatarDetail>("LoadAvatarDetail");await Check<IEnumerable<IAvatarDetail>>("LoadAllAvatarDetails");}
                else {await Check<IHolon>("LoadHolon");await Check<IEnumerable<IHolon>>("LoadAllHolons");}
                Assert.AreEqual(snapshot,(await (await session.RunAsync("MATCH (n:"+label+" {Id:$id}) RETURN n.SnapshotJson AS snapshot",new {id=id.ToString()})).SingleAsync())["snapshot"].As<string>());
            }
            Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual("stale avatar",(await backup.LoadAvatarAsync(id)).Result.Username);
            Assert.AreEqual(41L,Convert.ToInt64((await backup.LoadAvatarDetailAsync(id)).Result.XP));
            Assert.AreEqual("stale holon",(await backup.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            verified=true;
        }
        finally
        {
            await neo.DeActivateProviderAsync();backup.Dispose();
            if(verified)
            {
                await (await session.RunAsync("MATCH (n:"+label+" {Id:$id}) DETACH DELETE n",new {id=id.ToString()})).ConsumeAsync();
                File.Delete(path);
            }
        }
    }
}

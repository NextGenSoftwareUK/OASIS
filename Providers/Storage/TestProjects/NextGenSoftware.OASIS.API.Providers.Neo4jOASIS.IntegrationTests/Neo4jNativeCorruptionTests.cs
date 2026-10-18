using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
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
    [DataRow(false,"Avatar",false)] [DataRow(true,"Avatar",false)]
    [DataRow(false,"AvatarDetail",false)] [DataRow(true,"AvatarDetail",false)]
    [DataRow(false,"Holon",false)] [DataRow(true,"Holon",false)]
    [DataRow(false,"Avatar",true)] [DataRow(true,"Avatar",true)]
    [DataRow(false,"AvatarDetail",true)] [DataRow(true,"AvatarDetail",true)]
    [DataRow(false,"Holon",true)] [DataRow(true,"Holon",true)]
    public async Task NativeCorruptSnapshotsNeverFallBackToStaleSqlite(bool synchronous,string label,bool nativeIdentity)
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
            if(label=="Avatar") Assert.IsFalse((await neo.SaveAvatarAsync(new Avatar {Id=id,Username="owned primary",Email="primary@example.invalid"})).IsError);
            else if(label=="AvatarDetail") Assert.IsFalse((await neo.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned primary",Email="primary@example.invalid",XP=7})).IsError);
            else Assert.IsFalse((await neo.SaveHolonAsync(new Holon {Id=id,Name="owned primary"},saveChildren:false)).IsError);
            Assert.IsFalse((await backup.SaveAvatarAsync(new Avatar {Id=id,Username="stale avatar",Email="stale@example.invalid",AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError);
            Assert.IsFalse((await backup.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="stale detail",Email="stale@example.invalid",XP=41})).IsError);
            Assert.IsFalse((await backup.SaveHolonAsync(new Holon {Id=id,Name="stale holon"},saveChildren:false)).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(neo);manager.RegisterProvider(backup);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            async Task Check<T>(string operation,string? selector=null)
            {
                var request=new StorageOperationRequest {Operation=operation,HolonId=id,AvatarId=operation=="LoadAllHolons" ? Guid.Empty:id,LoadChildren=false,PreferredProvider=ProviderType.Neo4jOASIS};
                request.Username=selector;request.Email=selector;request.SoftDelete=false;
                if(operation=="Search") request.SearchParams=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase> {
                    new SearchTextGroup {SearchQuery="owned",SearchAvatars=label=="Avatar",SearchHolons=label=="Holon",HolonType=HolonType.All}}};
                var result=synchronous ? router.RouteRequest<T>(request):await router.RouteRequestAsync<T>(request);
                Assert.IsTrue(result.IsError,result.Message);Assert.IsFalse(result.IsLoaded);Assert.IsFalse(result.IsDeleted);Assert.IsFalse(result.IsSaved);
                if(typeof(T)==typeof(bool)) Assert.AreEqual(false,(object)result.Result!);else Assert.IsNull(result.Result);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                if(!nativeIdentity) Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            var originalSnapshot=(await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) RETURN n.SnapshotJson AS snapshot",new {key=id.ToString()})).SingleAsync())["snapshot"].As<string>();
            var corruptions=nativeIdentity ? new object?[] {null,42L,"invalid-id",Guid.Empty.ToString(),Guid.NewGuid().ToString()}
                :new object?[] {"not-json",$"{{\"Id\":\"{id}\",\"Version\":\"not-an-integer\"}}",
                $"{{\"$type\":\"System.IO.FileInfo, System.Private.CoreLib\",\"Id\":\"{id}\"}}"};
            foreach(var corruption in corruptions)
            {
                var property=nativeIdentity ? "Id":"SnapshotJson";
                await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) SET n."+property+"=$corruption RETURN n.ProviderKey",new {key=id.ToString(),corruption})).ConsumeAsync();
                if(label=="Avatar") {if(!nativeIdentity) await Check<IAvatar>("LoadAvatar");await Check<IEnumerable<IAvatar>>("LoadAllAvatars");}
                else if(label=="AvatarDetail") {if(!nativeIdentity) await Check<IAvatarDetail>("LoadAvatarDetail");await Check<IEnumerable<IAvatarDetail>>("LoadAllAvatarDetails");}
                else {if(!nativeIdentity) await Check<IHolon>("LoadHolon");await Check<IEnumerable<IHolon>>("LoadAllHolons");}
                if(nativeIdentity && label!="AvatarDetail") await Check<ISearchResults>("Search");
                var retained=(await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) RETURN n.Id AS id,n.SnapshotJson AS snapshot",new {key=id.ToString()})).SingleAsync());
                Assert.AreEqual(nativeIdentity ? originalSnapshot:corruption,retained["snapshot"]);
                if(nativeIdentity) Assert.AreEqual(corruption,retained["id"]);
            }
            if(nativeIdentity && label!="Holon")
            {
                await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) SET n.Id=$key RETURN n.Id",new {key=id.ToString()})).ConsumeAsync();
                foreach(var field in new[] {"Username","Email"})
                {
                    var original=field=="Username" ? "owned primary":"primary@example.invalid";
                    var stale=field=="Email" ? "stale@example.invalid":label=="Avatar" ? "stale avatar":"stale detail";
                    foreach(var corruption in new object?[] {null,42L,stale})
                    {
                        await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) SET n."+field+"=$corruption RETURN n.ProviderKey",new {key=id.ToString(),corruption})).ConsumeAsync();
                        if(label=="Avatar") {await Check<IAvatar>("LoadAvatar");await Check<IEnumerable<IAvatar>>("LoadAllAvatars");}
                        else {await Check<IAvatarDetail>("LoadAvatarDetail");await Check<IEnumerable<IAvatarDetail>>("LoadAllAvatarDetails");}
                        if(corruption is string selector)
                        {
                            if(label=="Avatar")
                            {
                                await Check<IAvatar>(field=="Username" ? "LoadAvatarByUsername":"LoadAvatarByEmail",selector);
                                await Check<bool>(field=="Username" ? "DeleteAvatarByUsername":"DeleteAvatarByEmail",selector);
                            }
                            else await Check<IAvatarDetail>(field=="Username" ? "LoadAvatarDetailByUsername":"LoadAvatarDetailByEmail",selector);
                        }
                        var retained=(await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) RETURN n."+field+" AS value,n.SnapshotJson AS snapshot",new {key=id.ToString()})).SingleAsync());
                        Assert.AreEqual(corruption,retained["value"]);Assert.AreEqual(originalSnapshot,retained["snapshot"].As<string>());
                    }
                    await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) SET n."+field+"=$value RETURN n.ProviderKey",new {key=id.ToString(),value=original})).ConsumeAsync();
                    if(label=="Avatar") Assert.IsFalse((await neo.LoadAvatarAsync(id)).IsError);
                    else Assert.IsFalse((await neo.LoadAvatarDetailAsync(id)).IsError);
                }
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
                await (await session.RunAsync("MATCH (n:"+label+" {ProviderKey:$key}) DETACH DELETE n",new {key=id.ToString()})).ConsumeAsync();
                File.Delete(path);
            }
        }
    }
}

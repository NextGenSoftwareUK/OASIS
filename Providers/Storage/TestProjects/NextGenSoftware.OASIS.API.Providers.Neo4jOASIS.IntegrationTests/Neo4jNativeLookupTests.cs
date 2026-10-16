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
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using SQLiteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeAvatarAbsenceAndAmbiguityNeverFallBackToStaleSqlite(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"lookup-outcomes.db");Assert.IsFalse(File.Exists(path),"Existing failed lookup fixture requires reconciliation.");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");
        using var backup=new SQLiteProvider($"Data Source={path};Pooling=False");
        var ids=new[] {Guid.NewGuid(),Guid.NewGuid()};var verified=false;
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);Assert.IsFalse((await backup.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await backup.SaveAvatarAsync(new Avatar {Id=ids[0],Username="owned lookup",Email="lookup@example.invalid",FirstName="stale backup",AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError);
            Assert.IsFalse((await backup.SaveAvatarDetailAsync(new AvatarDetail {Id=ids[0],Username="owned lookup",Email="lookup@example.invalid",XP=41})).IsError);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings()}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true};manager.RegisterProvider(neo);manager.RegisterProvider(backup);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new OASISHyperDrive(manager);
            async Task Check<T>(string operation,bool ambiguous)
            {
                var request=new StorageOperationRequest {Operation=operation,AvatarId=ids[0],ProviderKey=ids[0].ToString(),Username="owned lookup",Email="lookup@example.invalid",PreferredProvider=ProviderType.Neo4jOASIS};
                var result=synchronous ? router.RouteRequest<T>(request):await router.RouteRequestAsync<T>(request);
                Assert.AreEqual(ambiguous,result.IsError,result.Message);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);
                Assert.AreEqual(ambiguous ? OASISResultErrorCodes.AmbiguousIdentity:OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
                if(ambiguous) Assert.IsInstanceOfType(result.Exception,typeof(StorageIdentityAmbiguousException));
                Assert.AreEqual(0L,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            }
            foreach(var operation in new[] {"LoadAvatar","LoadAvatarByProviderKey","LoadAvatarByUsername","LoadAvatarByEmail"}) await Check<IAvatar>(operation,false);
            foreach(var operation in new[] {"LoadAvatarDetail","LoadAvatarDetailByUsername","LoadAvatarDetailByEmail"}) await Check<IAvatarDetail>(operation,false);
            foreach(var id in ids)
            {
                Assert.IsFalse((await neo.SaveAvatarAsync(new Avatar {Id=id,Username="owned lookup",Email="lookup@example.invalid",FirstName="native "+id})).IsError);
                Assert.IsFalse((await neo.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned lookup",Email="lookup@example.invalid",XP=7})).IsError);
            }
            foreach(var operation in new[] {"LoadAvatarByUsername","LoadAvatarByEmail"}) await Check<IAvatar>(operation,true);
            foreach(var operation in new[] {"LoadAvatarDetailByUsername","LoadAvatarDetailByEmail"}) await Check<IAvatarDetail>(operation,true);
            Assert.AreEqual(4L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
            Assert.AreEqual("native "+ids[0],(await neo.LoadAvatarAsync(ids[0])).Result.FirstName);
            Assert.AreEqual(7L,Convert.ToInt64((await neo.LoadAvatarDetailAsync(ids[0])).Result.XP));
            Assert.AreEqual("stale backup",(await backup.LoadAvatarAsync(ids[0])).Result.FirstName);
            Assert.AreEqual(41L,Convert.ToInt64((await backup.LoadAvatarDetailAsync(ids[0])).Result.XP));
            verified=true;
        }
        finally
        {
            await neo.DeActivateProviderAsync();backup.Dispose();
            if(verified)
            {
                await (await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:AvatarDetail) AND n.Id IN $ids DETACH DELETE n",new {ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                File.Delete(path);
            }
        }
    }
}

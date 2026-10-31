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
using SQLite=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeNearbyV2UsesActualStoresAndRejectsCorruption(bool synchronous,bool avatars)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK");Assert.IsFalse(string.IsNullOrWhiteSpace(work));
        var path=Path.Combine(Path.GetFullPath(work!),"nearby-routing.db");Assert.IsFalse(File.Exists(path),"Preserve unresolved fixture; no reset.");
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:Holon RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");using var sqlite=new SQLite($"Data Source={path};Pooling=False");
        var id=Guid.NewGuid();var verified=false;
        var field=typeof(Provider).GetField("_driver",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        IDriver? healthy=null;
        try
        {
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);healthy=(IDriver)field.GetValue(neo)!;
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            IHolonBase Record(string label)
            {
                IHolonBase row=avatars ? new Avatar{Username=label,Email=label.Replace(" ","")+"@example.invalid",AvatarType=new EnumValue<AvatarType>(AvatarType.User)}:new Holon();
                row.Id=id;row.Name=label;row.IsActive=true;row.HolonType=avatars ? HolonType.Avatar:HolonType.Park;
                row.MetaData=new(){{"Latitude","30.0005"},{"Longitude","20"}};return row;
            }
            async Task Save(IOASISStorageProvider store,IHolonBase row)
            {
                var error=avatars ? (await store.SaveAvatarAsync((IAvatar)row)).IsError:(await store.SaveHolonAsync((IHolon)row,saveChildren:false)).IsError;Assert.IsFalse(error);
            }
            string Label(IHolonBase row)=>avatars ? ((IAvatar)row).Username:row.Name;
            var native=Record("neo nearby");await Save(neo,native);await Save(sqlite,Record("sqlite nearby"));
            var dna=new OASISDNA();dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
            dna.OASIS.SubscriptionConfig=new SubscriptionConfig{PlanType="Gold"};
            var types=new[]{new EnumValue<ProviderType>(ProviderType.Neo4jOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            ProviderManager Manager()
            {
                var manager=new ProviderManager(null,dna);manager.RegisterProvider(neo);manager.RegisterProvider(sqlite);
                Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
                Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
                Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);return manager;
            }
            var request=new StorageOperationRequest{Operation=avatars ? "GetAvatarsNearMe":"GetHolonsNearMe",GeoLatitude=30000000,GeoLongitude=20000000,RadiusInMeters=100,HolonType=HolonType.Park};
            async Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolonBase>>> Route(ProviderManager manager,LoadBalancingStrategy strategy=LoadBalancingStrategy.Auto)
            {
                var router=new OASISHyperDrive(manager);return synchronous ? router.RouteRequest<IEnumerable<IHolonBase>>(request,strategy):await router.RouteRequestAsync<IEnumerable<IHolonBase>>(request,strategy);
            }
            foreach(var type in types)
            {
                request.PreferredProvider=type.Value;var result=await Route(Manager());Assert.IsFalse(result.IsError,result.Message);
                Assert.AreEqual(type.Value==ProviderType.Neo4jOASIS ? "neo nearby":"sqlite nearby",Label(result.Result.Single()));
            }
            double Measure(IOASISNETProvider store)
            {
                var watch=System.Diagnostics.Stopwatch.StartNew();var result=avatars ? store.GetAvatarsNearMe(30000000,20000000,100).IsError:store.GetHolonsNearMe(30000000,20000000,100,HolonType.Park).IsError;
                watch.Stop();Assert.IsFalse(result);return watch.Elapsed.TotalMilliseconds;
            }
            var neoMs=Measure(neo);var sqliteMs=Measure(sqlite);var balance=Manager();balance.IsAutoLoadBalanceEnabled=true;
            balance.PerformanceMonitor.RecordRequest(ProviderType.Neo4jOASIS,true,neoMs);balance.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS,true,sqliteMs);
            request.PreferredProvider=ProviderType.Default;var selected=await Route(balance,LoadBalancingStrategy.Performance);
            Assert.IsFalse(selected.IsError,selected.Message);
            var decision=balance.LastProviderSelectionDiagnostic;
            Assert.AreEqual(LoadBalancingStrategy.Performance,decision.RequestedStrategy);
            Assert.AreEqual(LoadBalancingStrategy.Performance,decision.EffectiveStrategy);
            Assert.AreEqual(2,decision.Candidates.Count);Assert.IsTrue(decision.Candidates.All(candidate=>candidate.HasPerformanceMetrics));
            Assert.AreEqual(decision.SelectedProvider==ProviderType.Neo4jOASIS ? "neo nearby":"sqlite nearby",Label(selected.Result.Single()));
            request.PreferredProvider=ProviderType.Neo4jOASIS;
            using(var outage=GraphDatabase.Driver("bolt://127.0.0.1:17688",AuthTokens.Basic("neo4j","oasis-local-test-only"),config=>config.WithConnectionTimeout(TimeSpan.FromSeconds(1)).WithMaxTransactionRetryTime(TimeSpan.Zero)))
            {
                field.SetValue(neo,outage);
                try
                {
                    var failover=Manager();failover.IsAutoFailOverEnabled=true;var result=await Route(failover);
                    Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual("sqlite nearby",Label(result.Result.Single()));
                    Assert.AreEqual(ProviderType.SQLLiteDBOASIS,failover.LastFailoverDiagnostic.SelectedProvider);
                }
                finally {field.SetValue(neo,healthy);}
            }
            native.MetaData.Remove("Longitude");await Save(neo,native);
            var corruption=Manager();corruption.IsAutoFailOverEnabled=true;corruption.IsAutoReplicationEnabled=true;
            var rejected=await Route(corruption);Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result);Assert.IsInstanceOfType(rejected.Exception,typeof(InvalidDataException));
            Assert.AreEqual(0,corruption.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            IHolonBase persisted=avatars ? (await neo.LoadAvatarAsync(id)).Result:(await neo.LoadHolonAsync(id,loadChildren:false)).Result;
            IHolonBase secondary=avatars ? (await sqlite.LoadAvatarAsync(id)).Result:(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result;
            Assert.IsFalse(persisted.MetaData.ContainsKey("Longitude"));Assert.AreEqual("sqlite nearby",Label(secondary));verified=true;
        }
        finally
        {
            if(healthy!=null) field.SetValue(neo,healthy);await neo.DeActivateProviderAsync();await sqlite.DeActivateProviderAsync();sqlite.Dispose();
            if(verified){await (await session.RunAsync("MATCH (n) WHERE (n:Avatar OR n:Holon) AND n.Id = $id DETACH DELETE n",new{id=id.ToString()})).ConsumeAsync();File.Delete(path);}
        }
    }
}

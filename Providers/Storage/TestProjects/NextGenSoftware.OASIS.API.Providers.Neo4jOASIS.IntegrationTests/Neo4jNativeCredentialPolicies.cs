using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using SQLiteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    private static async Task VerifyCredentialPoliciesAsync(Provider neo,string key,bool synchronous)
    {
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"credential-policies.db");Assert.IsFalse(File.Exists(path),"Retained credential fixture requires inspection.");
        using(var sqlite=new SQLiteProvider($"Data Source={path};Pooling=False"))
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var types=new[] {ProviderType.Neo4jOASIS,ProviderType.SQLLiteDBOASIS};
            var providers=types.Select(type=>new EnumValue<ProviderType>(type)).ToArray();
            ProviderManager Context(bool balance,bool replicate=false)
            {
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
                var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=balance,IsAutoReplicationEnabled=replicate,IsAutoFailOverEnabled=!balance&&!replicate};
                manager.RegisterProvider(neo);manager.RegisterProvider(sqlite);
                Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
                Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
                Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
                return manager;
            }
            Avatar Create(Guid id,string credential)=>new() {Id=id,Username="owned-credential-"+id,IsActive=true,
                AvatarType=new EnumValue<AvatarType>(AvatarType.User),VerificationToken=credential,ResetToken=credential,
                RefreshTokens=new() {new RefreshToken {Token=credential}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=id,PublicKey=credential,PrivateKey=credential}}}}};
            var native=Create(Guid.NewGuid(),key);var backup=Create(Guid.NewGuid(),key);
            foreach(var pair in new[] {(Store:(IOASISStorageProvider)neo,Avatar:native),(Store:(IOASISStorageProvider)sqlite,Avatar:backup)})
            {
                Assert.IsFalse((await pair.Store.SaveAvatarAsync(pair.Avatar)).IsError);
                Assert.IsFalse((await pair.Store.SaveAvatarDetailAsync(new AvatarDetail {Id=pair.Avatar.Id,IsActive=true})).IsError);
            }
            var operations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
            StorageOperationRequest Request(string operation,string credential,ProviderType preferred=ProviderType.Default)=>new() {Operation=operation,
                VerificationToken=credential,ResetToken=credential,RefreshToken=credential,PublicKey=credential,PrivateKey=credential,PreferredProvider=preferred};
            var balancing=Context(true);var router=new OASISHyperDrive(balancing);
            foreach(var operation in operations)
            {
                var beforeNative=balancing.PerformanceMonitor.GetMetrics(types[0])?.TotalRequests??0;
                var beforeSqlite=balancing.PerformanceMonitor.GetMetrics(types[1])?.TotalRequests??0;
                var selected=new HashSet<Guid>();
                for(var index=0;index<2;index++)
                {
                    var request=Request(operation,key);
                    var read=synchronous ? router.RouteRequest<IAvatar>(request,LoadBalancingStrategy.RoundRobin):await router.RouteRequestAsync<IAvatar>(request,LoadBalancingStrategy.RoundRobin);
                    Assert.IsFalse(read.IsError,read.Message);selected.Add(read.Result.Id);
                }
                CollectionAssert.AreEquivalent(new[] {native.Id,backup.Id},selected.ToArray());
                Assert.AreEqual(beforeNative+1,balancing.PerformanceMonitor.GetMetrics(types[0]).TotalRequests);
                Assert.AreEqual(beforeSqlite+1,balancing.PerformanceMonitor.GetMetrics(types[1]).TotalRequests);
            }
            var field=typeof(Provider).GetField("_driver",BindingFlags.Instance|BindingFlags.NonPublic)!;
            var healthy=field.GetValue(neo);
            using(var unavailable=GraphDatabase.Driver("bolt://127.0.0.1:17688",AuthTokens.Basic("neo4j","oasis-local-test-only"),
                config=>config.WithConnectionTimeout(TimeSpan.FromSeconds(1)).WithMaxTransactionRetryTime(TimeSpan.Zero)))
            {
                field.SetValue(neo,unavailable);
                try
                {
                    foreach(var operation in operations)
                    {
                        // Independent policy contexts test execution, not circuit-breaker recovery.
                        var failover=Context(false);var fallback=new OASISHyperDrive(failover);
                        var request=Request(operation,key,types[0]);
                        var read=synchronous ? fallback.RouteRequest<IAvatar>(request):await fallback.RouteRequestAsync<IAvatar>(request);
                        Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(backup.Id,read.Result.Id);
                        Assert.AreEqual(1L,failover.PerformanceMonitor.GetMetrics(types[0]).TotalRequests);
                        Assert.AreEqual(1L,failover.PerformanceMonitor.GetMetrics(types[1]).TotalRequests);
                    }
                }
                finally {field.SetValue(neo,healthy);}
            }
            var replication=Context(false,true);var replicator=new OASISHyperDrive(replication);
            var id=Guid.NewGuid();string? previous=null;
            async Task CheckStore(IOASISStorageProvider store,string credential,bool absent)
            {
                var reads=new[] {
                    synchronous ? store.LoadAvatarByVerificationToken(credential):await store.LoadAvatarByVerificationTokenAsync(credential),
                    synchronous ? store.LoadAvatarByResetToken(credential):await store.LoadAvatarByResetTokenAsync(credential),
                    synchronous ? store.LoadAvatarByRefreshToken(credential):await store.LoadAvatarByRefreshTokenAsync(credential),
                    synchronous ? store.LoadAvatarByPublicKey(credential):await store.LoadAvatarByPublicKeyAsync(credential),
                    synchronous ? store.LoadAvatarByPrivateKey(credential):await store.LoadAvatarByPrivateKeyAsync(credential)};
                foreach(var read in reads)
                    if(absent) {Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,read.ErrorCode);Assert.IsNull(read.Result);Assert.IsFalse(read.IsLoaded);}
                    else {Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(id,read.Result.Id);Assert.AreEqual(id,read.Result.ProviderWallets[ProviderType.SolanaOASIS].Single().Id);Assert.AreEqual(credential,read.Result.RefreshTokens.Single().Token);}
            }
            foreach(var primary in types)
            {
                var credential=key+"-replicated-"+primary;
                var request=new StorageOperationRequest {Operation="SaveAvatar",Payload=Create(id,credential),PreferredProvider=primary};
                var saved=synchronous ? replicator.RouteRequest<IAvatar>(request):await replicator.RouteRequestAsync<IAvatar>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                var detailRequest=new StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true},PreferredProvider=primary};
                var detail=synchronous ? replicator.RouteRequest<IAvatarDetail>(detailRequest):await replicator.RouteRequestAsync<IAvatarDetail>(detailRequest);
                Assert.IsFalse(detail.IsError,detail.Message);Assert.IsFalse(detail.IsWarning,detail.Message);
                foreach(var store in new IOASISStorageProvider[] {neo,sqlite}) {await CheckStore(store,credential,false);if(previous!=null) await CheckStore(store,previous,true);}
                previous=credential;
            }
            var deletion=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=types[0]};
            var deleted=synchronous ? replicator.RouteRequest<bool>(deletion):await replicator.RouteRequestAsync<bool>(deletion);
            Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);
            foreach(var store in new IOASISStorageProvider[] {neo,sqlite})
            {
                await CheckStore(store,previous!,true);
                Assert.IsFalse((await store.DeleteAvatarAsync(id,false)).IsError);
            }
            Assert.IsFalse((await neo.DeleteAvatarAsync(native.Id,false)).IsError);
            Assert.IsFalse((await sqlite.DeleteAvatarAsync(backup.Id,false)).IsError);
            await sqlite.DeActivateProviderAsync();
        }
        File.Delete(path);
    }
}

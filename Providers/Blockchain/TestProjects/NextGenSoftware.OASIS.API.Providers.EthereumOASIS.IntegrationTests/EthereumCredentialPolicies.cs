using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    private static async Task VerifyCredentialPoliciesAsync(EthereumOASIS ethereum,
        NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS sqlite,string key,bool synchronous)
    {
        var types=new[] {ProviderType.EthereumOASIS,ProviderType.SQLLiteDBOASIS};
        var parent=Guid.NewGuid();var walletId=Guid.NewGuid();
        Avatar Create(Guid id,string credential)=>new() {Id=id,Username="owned-policy-"+id,IsActive=true,ParentHolonId=parent,
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),VerificationToken=credential,ResetToken=credential,
            RefreshTokens=new() {new RefreshToken {Token=credential}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=walletId,PublicKey=credential,PrivateKey=credential}}}}};
        var native=Create(Guid.NewGuid(),key);var backup=Create(Guid.NewGuid(),key);
        foreach(var pair in new[] {(Store:(IOASISStorageProvider)ethereum,Avatar:native),(Store:(IOASISStorageProvider)sqlite,Avatar:backup)})
        {
            Assert.IsFalse((await pair.Store.SaveAvatarAsync(pair.Avatar)).IsError);
            Assert.IsFalse((await pair.Store.SaveAvatarDetailAsync(new AvatarDetail {Id=pair.Avatar.Id,IsActive=true})).IsError);
        }
        ProviderManager Context(bool balance,bool replicate=false)
        {
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=balance,IsAutoReplicationEnabled=replicate,IsAutoFailOverEnabled=!balance&&!replicate};
            manager.RegisterProvider(ethereum);manager.RegisterProvider(sqlite);
            var providers=types.Select(type=>new NextGenSoftware.Utilities.EnumValue<ProviderType>(type)).ToArray();
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);return manager;
        }
        StorageOperationRequest Request(string operation,ProviderType preferred=ProviderType.Default)=>new() {Operation=operation,PreferredProvider=preferred,
            VerificationToken=key,ResetToken=key,RefreshToken=key,PublicKey=key,PrivateKey=key};
        var operations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
        var balancing=Context(true);var router=new OASISHyperDrive(balancing);
        foreach(var operation in operations)
        {
            var before=types.Select(type=>balancing.PerformanceMonitor.GetMetrics(type)?.TotalRequests??0).ToArray();var selected=new HashSet<Guid>();
            for(var index=0;index<2;index++)
            {
                var request=Request(operation);
                var read=synchronous ? router.RouteRequest<IAvatar>(request,LoadBalancingStrategy.RoundRobin):await router.RouteRequestAsync<IAvatar>(request,LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);selected.Add(read.Result.Id);
            }
            CollectionAssert.AreEquivalent(new[] {native.Id,backup.Id},selected.ToArray());
            for(var index=0;index<2;index++) Assert.AreEqual(before[index]+1,balancing.PerformanceMonitor.GetMetrics(types[index]).TotalRequests);
            foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>())
            {
                before=types.Select(type=>balancing.PerformanceMonitor.GetMetrics(type).TotalRequests).ToArray();var request=Request(operation);
                var read=synchronous ? router.RouteRequest<IAvatar>(request,strategy):await router.RouteRequestAsync<IAvatar>(request,strategy);
                Assert.IsFalse(read.IsError,read.Message);var diagnostic=balancing.LastProviderSelectionDiagnostic;
                Assert.AreEqual(strategy,diagnostic.RequestedStrategy);Assert.AreEqual(2,diagnostic.Candidates.Count);
                Assert.AreEqual(diagnostic.SelectedProvider==types[0] ? native.Id:backup.Id,read.Result.Id);
                for(var index=0;index<2;index++) Assert.AreEqual(before[index]+(diagnostic.SelectedProvider==types[index] ? 1:0),balancing.PerformanceMonitor.GetMetrics(types[index]).TotalRequests);
            }
        }
        var healthy=ethereum.Web3Client;
        ethereum.Web3Client=new Nethereum.Web3.Web3("http://127.0.0.1:18546");
        try
        {
            foreach(var operation in operations)
            {
                // Independent contexts exercise each real SDK failure, not circuit-breaker recovery.
                var failover=Context(false);var fallback=new OASISHyperDrive(failover);var request=Request(operation,types[0]);
                var read=synchronous ? fallback.RouteRequest<IAvatar>(request):await fallback.RouteRequestAsync<IAvatar>(request);
                Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(backup.Id,read.Result.Id);
                Assert.AreEqual(1L,failover.PerformanceMonitor.GetMetrics(types[0]).TotalRequests);
                Assert.AreEqual(1L,failover.PerformanceMonitor.GetMetrics(types[1]).TotalRequests);
            }
        }
        finally {ethereum.Web3Client=healthy;}
        var replication=Context(false,true);var replicator=new OASISHyperDrive(replication);var id=Guid.NewGuid();string? previous=null;
        async Task Check(IOASISStorageProvider store,string value,bool absent)
        {
            var reads=new[] {
                synchronous ? store.LoadAvatarByVerificationToken(value):await store.LoadAvatarByVerificationTokenAsync(value),
                synchronous ? store.LoadAvatarByResetToken(value):await store.LoadAvatarByResetTokenAsync(value),
                synchronous ? store.LoadAvatarByRefreshToken(value):await store.LoadAvatarByRefreshTokenAsync(value),
                synchronous ? store.LoadAvatarByPublicKey(value):await store.LoadAvatarByPublicKeyAsync(value),
                synchronous ? store.LoadAvatarByPrivateKey(value):await store.LoadAvatarByPrivateKeyAsync(value)};
            foreach(var read in reads)
                if(absent) {Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,read.ErrorCode);Assert.IsNull(read.Result);Assert.IsFalse(read.IsLoaded);}
                else {Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(id,read.Result.Id);Assert.AreEqual(parent,read.Result.ParentHolonId);
                    Assert.AreEqual(walletId,read.Result.ProviderWallets[ProviderType.SolanaOASIS].Single().Id);Assert.AreEqual(value,read.Result.RefreshTokens.Single().Token);}
        }
        foreach(var primary in types)
        {
            var value=key+"-replicated-"+primary;
            var request=new StorageOperationRequest {Operation="SaveAvatar",Payload=Create(id,value),PreferredProvider=primary};
            var saved=synchronous ? replicator.RouteRequest<IAvatar>(request):await replicator.RouteRequestAsync<IAvatar>(request);
            Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
            var detailRequest=new StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true},PreferredProvider=primary};
            var detail=synchronous ? replicator.RouteRequest<IAvatarDetail>(detailRequest):await replicator.RouteRequestAsync<IAvatarDetail>(detailRequest);
            Assert.IsFalse(detail.IsError,detail.Message);Assert.IsFalse(detail.IsWarning,detail.Message);
            foreach(var store in new IOASISStorageProvider[] {ethereum,sqlite}) {await Check(store,value,false);if(previous!=null) await Check(store,previous,true);}
            previous=value;
        }
        var deletion=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=types[0]};
        var deleted=synchronous ? replicator.RouteRequest<bool>(deletion):await replicator.RouteRequestAsync<bool>(deletion);
        Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);
        foreach(var store in new IOASISStorageProvider[] {ethereum,sqlite}) {await Check(store,previous!,true);Assert.IsFalse((await store.DeleteAvatarAsync(id,false)).IsError);}
        Assert.IsFalse((await ethereum.DeleteAvatarAsync(native.Id,false)).IsError);
        Assert.IsFalse((await sqlite.DeleteAvatarAsync(backup.Id,false)).IsError);
    }
}

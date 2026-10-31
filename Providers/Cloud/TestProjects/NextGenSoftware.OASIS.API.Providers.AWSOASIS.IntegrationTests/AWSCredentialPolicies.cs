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

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    private async Task VerifyNativeCredentialPoliciesAsync(IOASISStorageProvider sqlite,bool synchronous)
    {
        var types=new[] {ProviderType.AWSOASIS,ProviderType.SQLLiteDBOASIS};
        var stores=new IOASISStorageProvider[] {_provider,sqlite};
        var id=Guid.NewGuid();var walletId=Guid.NewGuid();var parent=Guid.NewGuid();
        string? previous=null;var credential="owned-policy-"+id;
        var operations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
        ProviderManager Context(bool balance,bool replicate=false)
        {
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=balance,IsAutoReplicationEnabled=replicate,IsAutoFailOverEnabled=!balance&&!replicate};
            foreach(var store in stores) manager.RegisterProvider(store);
            var providers=types.Select(type=>new NextGenSoftware.Utilities.EnumValue<ProviderType>(type)).ToArray();
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);return manager;
        }
        async Task<OASISResult<T>> Route<T>(OASISHyperDrive router,StorageOperationRequest request,LoadBalancingStrategy strategy=LoadBalancingStrategy.Auto)
            =>synchronous ? router.RouteRequest<T>(request,strategy):await router.RouteRequestAsync<T>(request,strategy);
        StorageOperationRequest Request(string operation)=>new() {Operation=operation,VerificationToken=credential,ResetToken=credential,RefreshToken=credential,PublicKey=credential,PrivateKey=credential};
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
                    Assert.AreEqual(walletId,read.Result.ProviderWallets[ProviderType.SolanaOASIS].Single().Id);}
        }
        var replicator=new OASISHyperDrive(Context(false,true));
        foreach(var primary in types)
        {
            credential="owned-policy-"+id+"-"+primary;
            var avatar=new Avatar {Id=id,Username="owned-policy-"+id,IsActive=true,ParentHolonId=parent,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),VerificationToken=credential,ResetToken=credential,
                RefreshTokens=new() {new RefreshToken {Token=credential}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=walletId,PublicKey=credential,PrivateKey=credential}}}}};
            var saved=await Route<IAvatar>(replicator,new() {Operation="SaveAvatar",Payload=avatar,PreferredProvider=primary});
            Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
            var detail=await Route<IAvatarDetail>(replicator,new() {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true},PreferredProvider=primary});
            Assert.IsFalse(detail.IsError,detail.Message);Assert.IsFalse(detail.IsWarning,detail.Message);
            foreach(var store in stores) {await Check(store,credential,false);if(previous!=null) await Check(store,previous,true);}
            previous=credential;
        }
        var balancing=Context(true);var router=new OASISHyperDrive(balancing);
        foreach(var operation in operations)
        {
            var before=types.Select(type=>balancing.PerformanceMonitor.GetMetrics(type)?.TotalRequests??0).ToArray();
            for(var index=0;index<2;index++) {var read=await Route<IAvatar>(router,Request(operation),LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(id,read.Result.Id);}
            for(var index=0;index<2;index++) Assert.AreEqual(before[index]+1,balancing.PerformanceMonitor.GetMetrics(types[index]).TotalRequests);
        }
        var field=typeof(NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.DynamoDBOASIS).GetField("_client",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var healthy=field.GetValue(_provider);
        using var closed=new Amazon.DynamoDBv2.AmazonDynamoDBClient(new Amazon.Runtime.BasicAWSCredentials("owned-key","owned-secret"),
            new Amazon.DynamoDBv2.AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18001",AuthenticationRegion="us-east-1",UseHttp=true,MaxErrorRetry=0,Timeout=TimeSpan.FromSeconds(2)});
        try
        {
            field.SetValue(_provider,closed);
            foreach(var operation in operations)
            {
                // Independent contexts test actual SDK failures, not circuit recovery.
                var manager=Context(false);var request=Request(operation);request.PreferredProvider=types[0];
                var read=await Route<IAvatar>(new OASISHyperDrive(manager),request);
                Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(id,read.Result.Id);
                foreach(var type in types) Assert.AreEqual(1L,manager.PerformanceMonitor.GetMetrics(type).TotalRequests);
            }
        }
        finally {field.SetValue(_provider,healthy);}
        var deleted=await Route<bool>(replicator,new() {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=types[0]});
        Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);
        foreach(var store in stores) {await Check(store,credential,true);Assert.IsFalse((await store.DeleteAvatarAsync(id,false)).IsError);}
    }
}

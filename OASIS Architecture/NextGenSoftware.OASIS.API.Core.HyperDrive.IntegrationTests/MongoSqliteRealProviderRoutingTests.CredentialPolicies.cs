using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyCredentialRoutingPoliciesAsync(MongoDBOASIS mongo,IOASISStorageProvider sqlite,bool synchronous)
    {
        var key="owned-credential-routing-"+Guid.NewGuid().ToString("N");
        Avatar Create()=>new() {Id=Guid.NewGuid(),Username=Guid.NewGuid().ToString("N"),IsActive=true,
            AvatarType=new EnumValue<AvatarType>(AvatarType.User),VerificationToken=key,ResetToken=key,
            RefreshTokens=new() {new RefreshToken {Token=key}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
        var native=Create();var backup=Create();
        foreach(var pair in new[] {(Store:(IOASISStorageProvider)mongo,Avatar:native),(Store:sqlite,Avatar:backup)})
        {
            (await pair.Store.SaveAvatarAsync(pair.Avatar)).IsError.Should().BeFalse();
            (await pair.Store.SaveAvatarDetailAsync(new AvatarDetail {Id=pair.Avatar.Id,IsActive=true})).IsError.Should().BeFalse();
        }
        var types=new[] {ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS};
        ProviderManager Context(bool balance)
        {
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=balance,IsAutoFailOverEnabled=!balance,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
            var providers=types.Select(type=>new EnumValue<ProviderType>(type)).ToArray();
            manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError.Should().BeFalse();
            manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError.Should().BeFalse();return manager;
        }
        StorageOperationRequest Request(string operation,ProviderType preferred=ProviderType.Default)=>new() {Operation=operation,
            VerificationToken=key,ResetToken=key,RefreshToken=key,PublicKey=key,PrivateKey=key,PreferredProvider=preferred};
        var operations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
        var balancing=Context(true);var router=new OASISHyperDrive(balancing);
        foreach(var operation in operations)
        {
            var selected=new HashSet<Guid>();
            var before=types.Select(type=>balancing.PerformanceMonitor.GetMetrics(type)?.TotalRequests??0).ToArray();
            for(var index=0;index<2;index++)
            {
                var request=Request(operation);
                var read=synchronous ? router.RouteRequest<IAvatar>(request,LoadBalancingStrategy.RoundRobin):await router.RouteRequestAsync<IAvatar>(request,LoadBalancingStrategy.RoundRobin);
                read.IsError.Should().BeFalse(read.Message);selected.Add(read.Result.Id);
            }
            selected.Should().BeEquivalentTo(new[] {native.Id,backup.Id});
            for(var index=0;index<2;index++) balancing.PerformanceMonitor.GetMetrics(types[index]).TotalRequests.Should().Be(before[index]+1);
            foreach(var strategy in Enum.GetValues<LoadBalancingStrategy>())
            {
                before=types.Select(type=>balancing.PerformanceMonitor.GetMetrics(type).TotalRequests).ToArray();
                var request=Request(operation);
                var read=synchronous ? router.RouteRequest<IAvatar>(request,strategy):await router.RouteRequestAsync<IAvatar>(request,strategy);
                read.IsError.Should().BeFalse(read.Message);
                var diagnostic=balancing.LastProviderSelectionDiagnostic;diagnostic.RequestedStrategy.Should().Be(strategy);diagnostic.Candidates.Count.Should().Be(2);
                read.Result.Id.Should().Be(diagnostic.SelectedProvider==types[0] ? native.Id:backup.Id);
                for(var index=0;index<2;index++) balancing.PerformanceMonitor.GetMetrics(types[index]).TotalRequests.Should().Be(before[index]+(diagnostic.SelectedProvider==types[index] ? 1:0));
            }
        }
        // Real SDK socket failure, not a synthetic repository exception or changed activation state.
        var healthy=mongo.Database.MongoDB;
        var unreachable=new MongoClient("mongodb://127.0.0.1:27030/?directConnection=true&serverSelectionTimeoutMS=250&connectTimeoutMS=250&retryReads=false");
        mongo.Database.MongoDB=unreachable.GetDatabase(mongo.DBName);
        try
        {
            foreach(var operation in operations)
            {
                // Fresh contexts prove each operation executes failover; no circuit recovery claim.
                var failover=Context(false);var fallback=new OASISHyperDrive(failover);var request=Request(operation,types[0]);
                var read=synchronous ? fallback.RouteRequest<IAvatar>(request):await fallback.RouteRequestAsync<IAvatar>(request);
                read.IsError.Should().BeFalse(read.Message);read.Result.Id.Should().Be(backup.Id);
                failover.PerformanceMonitor.GetMetrics(types[0]).TotalRequests.Should().Be(1);
                failover.PerformanceMonitor.GetMetrics(types[1]).TotalRequests.Should().Be(1);
            }
        }
        finally {mongo.Database.MongoDB=healthy;}
        (await mongo.DeleteAvatarAsync(native.Id,false)).IsError.Should().BeFalse();
        (await sqlite.DeleteAvatarAsync(backup.Id,false)).IsError.Should().BeFalse();
    }
}

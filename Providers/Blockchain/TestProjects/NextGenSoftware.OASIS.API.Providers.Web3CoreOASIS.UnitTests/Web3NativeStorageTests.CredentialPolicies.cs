using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyCredentialBalanceAndOutageAsync(Web3CoreOASISBaseProvider provider,
        NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS sqlite,string key,Guid nativeId,Guid sqliteId,bool synchronous)
    {
        var nativeType=provider.ProviderType.Value;
        var providers=new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(nativeType),new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
        ProviderManager Context(bool balance)
        {
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=balance,IsAutoFailOverEnabled=!balance,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            return manager;
        }
        StorageOperationRequest Request(string operation,bool preferred) => new() {Operation=operation,VerificationToken=key,ResetToken=key,RefreshToken=key,
            PublicKey=key,PrivateKey=key,PreferredProvider=preferred ? nativeType:ProviderType.Default};
        var operations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
        var balancing=Context(true);var router=new OASISHyperDrive(balancing);
        foreach(var operation in operations)
        {
            var nativeBefore=balancing.PerformanceMonitor.GetMetrics(nativeType)?.TotalRequests ?? 0;
            var sqliteBefore=balancing.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var selected=new HashSet<Guid>();
            for(var i=0;i<2;i++)
            {
                var request=Request(operation,false);
                var read=synchronous ? router.RouteRequest<IAvatar>(request,LoadBalancingStrategy.RoundRobin):await router.RouteRequestAsync<IAvatar>(request,LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);Assert.IsNotNull(read.Result);selected.Add(read.Result.Id);
            }
            CollectionAssert.AreEquivalent(new[] {nativeId,sqliteId},selected.ToArray());
            Assert.AreEqual(nativeBefore+1,balancing.PerformanceMonitor.GetMetrics(nativeType).TotalRequests);
            Assert.AreEqual(sqliteBefore+1,balancing.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests);
        }
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var field=typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!;
        var healthy=field.GetValue(provider);
        var address=(string)typeof(Web3CoreOASISBaseProvider).GetField("_contractAddress",flags)!.GetValue(provider)!;
        var unreachable=new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",address,Web3CoreOASISBaseProviderHelper.Abi,31337);
        field.SetValue(provider,unreachable);
        try
        {
            foreach(var operation in operations)
            {
                // Independent policy context per operation: no invented circuit-breaker recovery or health reset.
                var failover=Context(false);var fallbackRouter=new OASISHyperDrive(failover);
                var nativeBefore=failover.PerformanceMonitor.GetMetrics(nativeType)?.TotalRequests ?? 0;
                var sqliteBefore=failover.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                var request=Request(operation,true);
                var read=synchronous ? fallbackRouter.RouteRequest<IAvatar>(request):await fallbackRouter.RouteRequestAsync<IAvatar>(request);
                Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(sqliteId,read.Result?.Id);
                Assert.AreEqual(nativeBefore+1,failover.PerformanceMonitor.GetMetrics(nativeType).TotalRequests);
                Assert.AreEqual(sqliteBefore+1,failover.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests);
            }
        }
        finally {field.SetValue(provider,healthy);}
    }
}

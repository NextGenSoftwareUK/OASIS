using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false,false,false)] [InlineData(true,false,false)] [InlineData(false,true,false)]
    [InlineData(false,false,true)] [InlineData(true,false,true)]
    public async Task ExhaustedOperationAttemptBudgetStopsRemainingRoutesAndPreservesCause(bool synchronous,bool explicitReplication,bool failover)
    {
        var primary=CreateActiveProvider();var replica=CreateActiveProvider(ProviderType.LocalFileOASIS,"budget-replica");
        var later=CreateActiveProvider(ProviderType.IPFSOASIS,"must-not-be-visited");var holon=new Holon {Id=Guid.NewGuid()};
        var saved=failover?new OASISResult<IHolon>{IsError=true,Message="controlled primary availability failure"}:new OASISResult<IHolon>(holon){IsSaved=true};
        primary.Setup(p=>p.SaveHolon(holon,true,true,0,true,false)).Returns(saved);
        primary.Setup(p=>p.SaveHolonAsync(holon,true,true,0,true,false)).ReturnsAsync(saved);
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=failover,IsAutoReplicationEnabled=!failover,IsAutoLoadBalanceEnabled=false};
        foreach(var provider in new[]{primary,replica,later})providers.RegisterProvider(provider.Object);
        var types=new[]{ProviderType.MongoDBOASIS,ProviderType.LocalFileOASIS,ProviderType.IPFSOASIS};
        var entries=types.Select(type=>new EnumValue<ProviderType>(type)).ToArray();
        providers.SetAndReplaceAutoReplicationListForProviders(entries).IsError.Should().BeFalse();
        providers.SetAndReplaceAutoFailOverListForProviders(entries).IsError.Should().BeFalse();
        var cause=new UsageProtocolException("PROVIDER_ATTEMPT_BUDGET_EXCEEDED","controlled durable limit reached");
        var store=new Mock<IUsageOutboxStore>();var calls=0;
        store.Setup(x=>x.BeginProviderAttemptAsync(It.IsAny<string>(),It.IsAny<string>(),It.IsAny<UsageProviderAttempt>(),It.IsAny<CancellationToken>()))
            .Returns((string id,string owner,UsageProviderAttempt attempt,CancellationToken token)=>Interlocked.Increment(ref calls)==1?Task.CompletedTask:Task.FromException(cause));
        var execution=new UsageExecutionHandle(new UsageOutboxOperation(),store.Object,new Web4UsageLedgerOptions(),new SubscriptionAuthorizationResult(),Mock.Of<IUsageReservationAuthority>());
        var policy=new PreparedStorageExecution(execution,"attempt-budget",new Dictionary<ProviderType,string>(),types);
        var router=new OASISHyperDrive(providers);var request=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,PreferredProvider=ProviderType.MongoDBOASIS};
        await policy.RunAsync(async()=>{
            if(explicitReplication){
                var result=await router.ReplicateRequestAsync<IHolon>(request);
                result.IsWarning.Should().BeTrue();result.Exception.Should().BeSameAs(cause);
            }else{
                var result=synchronous?router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
                result.IsError.Should().Be(failover);result.Exception.Should().BeSameAs(cause);
                if(!failover)result.IsWarning.Should().BeTrue();
            }
            return true;
        });
        calls.Should().Be(2,"a global exhausted operation budget cannot improve at another replica");
        later.Invocations.Should().NotContain(call=>call.Method.Name=="SaveHolon"||call.Method.Name=="SaveHolonAsync");
        if(!failover)router.LastReplicationDiagnostic.Attempts.Count.Should().Be(explicitReplication?2:1);
    }
}

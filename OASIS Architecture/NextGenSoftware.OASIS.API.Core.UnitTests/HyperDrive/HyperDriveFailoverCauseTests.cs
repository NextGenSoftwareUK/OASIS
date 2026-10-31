using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData("sync",0)] [InlineData("async",0)] [InlineData("explicit",0)]
    [InlineData("sync",1)] [InlineData("async",1)] [InlineData("explicit",1)]
    [InlineData("sync",2)] [InlineData("async",2)] [InlineData("explicit",2)]
    public async Task ExhaustedFailoverRetainsActualProviderCausesWithoutInventingExceptions(string mode,int causeCount)
    {
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=false,IsAutoReplicationEnabled=false};
        var primary=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"cause-primary");
        var secondary=CreateActiveProvider(ProviderType.MongoDBOASIS,"cause-secondary");
        var first=new IOException("original primary outage");var second=new IOException("original secondary outage");
        var id=Guid.NewGuid();
        var failures=new[]{new OASISResult<IHolon>{IsError=true,Message="primary unavailable",Exception=causeCount>0 ? first:null},
            new OASISResult<IHolon>{IsError=true,Message="secondary unavailable",Exception=causeCount>1 ? second:null}};
        var mocks=new[]{primary,secondary};
        for(var i=0;i<mocks.Length;i++)
        {
            var failure=failures[i];var provider=mocks[i];
            provider.Setup(x=>x.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(x=>x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            provider.Setup(x=>x.LoadHolon(id,true,true,0,true,false,0)).Returns(failure);
            provider.Setup(x=>x.LoadHolonAsync(id,true,true,0,true,false,0)).ReturnsAsync(failure);
            providers.RegisterProvider(provider.Object).Should().BeTrue();
        }
        providers.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providers.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS),new EnumValue<ProviderType>(ProviderType.MongoDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(providers);
        var request=new StorageOperationRequest{Operation="LoadHolon",HolonId=id,PreferredProvider=ProviderType.SQLLiteDBOASIS};
        var result=mode=="sync" ? router.RouteRequest<IHolon>(request):mode=="async" ? await router.RouteRequestAsync<IHolon>(request):await router.FailoverRequestAsync<IHolon>(request);
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.ErrorCode.Should().Be("HYPERDRIVE_FAILOVER_EXHAUSTED");
        router.LastFailoverDiagnostic.Exhausted.Should().BeTrue();router.LastFailoverDiagnostic.Attempts.Should().HaveCount(2);
        if(causeCount==0) result.Exception.Should().BeNull();
        else if(causeCount==1) result.Exception.Should().BeSameAs(first);
        else ((AggregateException)result.Exception).InnerExceptions.Should().Equal(first,second);
        foreach(var provider in mocks) provider.Invocations.Count(call=>call.Method.Name is "LoadHolon" or "LoadHolonAsync").Should().Be(1);
    }
}

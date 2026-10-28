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
    // A load-balanced read served by a replica that never received the write must not report
    // "not found": the primary is asked before the empty answer is returned.
    [Theory]
    [InlineData("sync")] [InlineData("async")]
    public async Task EmptyReplicaReadIsConfirmedAgainstThePrimary(string mode)
    {
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var providers=new ProviderManager(null,dna){IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=true,IsAutoReplicationEnabled=false};
        var primary=CreateActiveProvider(ProviderType.MongoDBOASIS,"read-primary");
        var replica=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"read-replica");
        var id=Guid.NewGuid();
        var stored=new Holon{Id=id,Name="PIPER_ROLE"};
        var hit=new OASISResult<IHolon>(stored);
        var miss=new OASISResult<IHolon>{IsError=false,Result=null};
        primary.Setup(x=>x.LoadHolon(id,true,true,0,true,false,0)).Returns(hit);
        primary.Setup(x=>x.LoadHolonAsync(id,true,true,0,true,false,0)).ReturnsAsync(hit);
        replica.Setup(x=>x.LoadHolon(id,true,true,0,true,false,0)).Returns(miss);
        replica.Setup(x=>x.LoadHolonAsync(id,true,true,0,true,false,0)).ReturnsAsync(miss);
        foreach(var provider in new[]{primary,replica})
        {
            provider.Setup(x=>x.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(x=>x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            providers.RegisterProvider(provider.Object).Should().BeTrue();
        }
        providers.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providers.AddProvidersToAutoLoadBalanceList(new[]{ProviderType.SQLLiteDBOASIS});
        var router=new OASISHyperDrive(providers);
        var request=new StorageOperationRequest{Operation="LoadHolon",HolonId=id};
        var result=mode=="sync" ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        result.Result.Id.Should().Be(id);
        replica.Invocations.Count(call=>call.Method.Name is "LoadHolon" or "LoadHolonAsync").Should().Be(1);
    }
}

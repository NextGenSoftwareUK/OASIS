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
    [Theory]
    [InlineData("sync", "outage")]
    [InlineData("async", "outage")]
    [InlineData("load-balance", "outage")]
    [InlineData("sync", "denied")]
    [InlineData("async", "denied")]
    [InlineData("load-balance", "denied")]
    [InlineData("sync", "no-wrapper")]
    [InlineData("async", "no-wrapper")]
    [InlineData("load-balance", "no-wrapper")]
    [InlineData("sync", "healthy-empty")]
    [InlineData("async", "healthy-empty")]
    [InlineData("load-balance", "healthy-empty")]
    public async Task ReplicaAbsenceRequiresASuccessfulPrimaryConfirmation(string mode, string condition)
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var providers = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = true, IsAutoReplicationEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "confirmation-primary");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "confirmation-replica");
        var id = Guid.NewGuid();
        Exception cause = condition == "outage" ? new IOException("primary connection unavailable")
            : condition == "denied" ? new UnauthorizedAccessException("primary read denied") : null;
        var expectedCode = condition == "outage" ? "CONTROLLED_PRIMARY_UNAVAILABLE"
            : condition == "denied" ? "CONTROLLED_PRIMARY_DENIED" : "HYPERDRIVE_PROVIDER_NO_RESULT";
        OASISResult<IHolon> confirmation = condition == "no-wrapper" ? null
            : condition == "healthy-empty" ? new OASISResult<IHolon>()
            : new OASISResult<IHolon> { IsError = true, ErrorCode = expectedCode, Message = cause.Message, Exception = cause };
        primary.Setup(value => value.LoadHolon(id, true, true, 0, true, false, 0)).Returns(confirmation);
        primary.Setup(value => value.LoadHolonAsync(id, true, true, 0, true, false, 0)).ReturnsAsync(confirmation);
        replica.Setup(value => value.LoadHolon(id, true, true, 0, true, false, 0)).Returns(new OASISResult<IHolon>());
        replica.Setup(value => value.LoadHolonAsync(id, true, true, 0, true, false, 0)).ReturnsAsync(new OASISResult<IHolon>());
        foreach (var provider in new[] { primary, replica })
        {
            provider.Setup(value => value.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(value => value.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            providers.RegisterProvider(provider.Object).Should().BeTrue();
        }
        providers.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providers.AddProvidersToAutoLoadBalanceList(new[] { ProviderType.SQLLiteDBOASIS });
        var request = new StorageOperationRequest { Operation = "LoadHolon", HolonId = id };
        var router = new OASISHyperDrive(providers);

        var result = mode == "sync" ? router.RouteRequest<IHolon>(request)
            : mode == "async" ? await router.RouteRequestAsync<IHolon>(request)
            : await router.LoadBalanceRequestAsync<IHolon>(request);

        result.Result.Should().BeNull();
        result.IsError.Should().Be(condition != "healthy-empty", "a replica miss cannot prove absence while its primary is unavailable or refuses the read");
        if (condition != "healthy-empty") result.ErrorCode.Should().Be(expectedCode);
        if (cause != null) result.Exception.GetBaseException().Should().BeSameAs(cause);
        foreach (var provider in new[] { primary, replica })
            provider.Invocations.Count(call => call.Method.Name is "LoadHolon" or "LoadHolonAsync").Should().Be(1);
    }

}

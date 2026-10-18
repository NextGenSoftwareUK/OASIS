using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData("sync",false)] [InlineData("async",false)] [InlineData("explicit",false)]
    [InlineData("sync",true)] [InlineData("async",true)] [InlineData("explicit",true)]
    public async Task V2CannotActivateDispatchOrFailOverFromKnownLegacyOnlyProvider(string mode,bool active)
    {
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false,IsAutoLoadBalanceEnabled=false};
        var legacy=LegacyProvider(active);var backup=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"mode-backup");
        var id=Guid.NewGuid();
        legacy.Setup(p=>p.ActivateProvider()).Returns(new OASISResult<bool>(true));
        legacy.Setup(p=>p.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        legacy.Setup(p=>p.LoadHolon(id,true,true,0,true,false,0)).Returns(new OASISResult<IHolon>(new Holon{Id=id}));
        legacy.Setup(p=>p.LoadHolonAsync(id,true,true,0,true,false,0)).ReturnsAsync(new OASISResult<IHolon>(new Holon{Id=id}));
        manager.RegisterProvider(legacy.Object).Should().BeTrue();manager.RegisterProvider(backup.Object).Should().BeTrue();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.ArbitrumOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest{Operation="LoadHolon",HolonId=id,PreferredProvider=ProviderType.ArbitrumOASIS};
        var result=mode=="sync" ? router.RouteRequest<IHolon>(request):mode=="async" ? await router.RouteRequestAsync<IHolon>(request):await router.FailoverRequestAsync<IHolon>(request);
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.ErrorCode.Should().Be("HYPERDRIVE_PROVIDER_RESTART_REQUIRED");
        legacy.Invocations.Where(call=>call.Method.Name is "ActivateProvider" or "ActivateProviderAsync" or "DeActivateProvider" or "DeActivateProviderAsync" or "LoadHolon" or "LoadHolonAsync").Should().BeEmpty();
        backup.Invocations.Where(call=>call.Method.Name is "LoadHolon" or "LoadHolonAsync").Should().BeEmpty();
        manager.GetProvider(ProviderType.ArbitrumOASIS).Should().BeSameAs(legacy.Object);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ModePreflightDoesNotChangeDnaPolicyOrProviderInstance(bool v2)
    {
        var dna=CreateDna(HyperDriveModes.Legacy);var manager=new ProviderManager(null,dna);var provider=LegacyProvider(true);
        manager.RegisterProvider(provider.Object).Should().BeTrue();var source=manager.EffectiveHyperDriveConfigurationSource;
        var result=manager.ValidateHyperDriveModeTransition(v2 ? HyperDriveModes.V2:HyperDriveModes.Legacy);
        result.IsError.Should().Be(v2);dna.OASIS.HyperDriveMode.Should().Be(HyperDriveModes.Legacy);
        manager.EffectiveHyperDriveConfigurationSource.Should().Be(source);
        manager.GetProvider(ProviderType.ArbitrumOASIS).Should().BeSameAs(provider.Object);
        provider.Invocations.Where(call=>call.Method.Name is "DeActivateProvider" or "DeActivateProviderAsync").Should().BeEmpty();
    }

    private static Mock<IOASISLegacyOnlyStorageProvider> LegacyProvider(bool active)
    {
        var provider=new Mock<IOASISLegacyOnlyStorageProvider>();provider.SetupAllProperties();
        provider.Object.ProviderType=new(ProviderType.ArbitrumOASIS);provider.Object.ProviderName="known-legacy-protocol";
        provider.Object.ProviderCategory=new(ProviderCategory.Storage);provider.Object.IsProviderActivated=active;
        return provider;
    }
}

using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OrdinaryReplicaFailureDoesNotPreventLaterReplicaSubmission(bool synchronous)
    {
        var primary=CreateActiveProvider();var failure=CreateActiveProvider(ProviderType.LocalFileOASIS,"ordinary-failure");
        var later=CreateActiveProvider(ProviderType.IPFSOASIS,"later");var holon=new Holon{Id=Guid.NewGuid()};
        foreach(var provider in new[]{primary,later})
        {
            provider.Setup(store=>store.SaveHolon(holon,false,true,0,true,false)).Returns(new OASISResult<IHolon>(holon){IsSaved=true});
            provider.Setup(store=>store.SaveHolonAsync(holon,false,true,0,true,false)).ReturnsAsync(new OASISResult<IHolon>(holon){IsSaved=true});
        }
        var refused=new OASISResult<IHolon>{IsError=true,Exception=new IOException("ordinary owned refusal"),Message="ordinary refusal"};
        failure.Setup(store=>store.SaveHolon(holon,false,true,0,true,false)).Returns(refused);
        failure.Setup(store=>store.SaveHolonAsync(holon,false,true,0,true,false)).ReturnsAsync(refused);
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoReplicationEnabled=true};
        foreach(var provider in new[]{primary,failure,later}) manager.RegisterProvider(provider.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.IPFSOASIS)});
        var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
        var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeFalse();result.IsSaved.Should().BeTrue();result.IsWarning.Should().BeTrue();
        router.LastReplicationDiagnostic.Attempts.Should().HaveCount(2);router.LastReplicationDiagnostic.Attempts.Last().Succeeded.Should().BeTrue();
        if(synchronous) later.Verify(store=>store.SaveHolon(holon,false,true,0,true,false),Times.Once);
        else later.Verify(store=>store.SaveHolonAsync(holon,false,true,0,true,false),Times.Once);
    }
}

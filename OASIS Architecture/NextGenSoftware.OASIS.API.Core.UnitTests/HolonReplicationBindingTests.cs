using Moq;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class HolonReplicationBindingTests
{
    [Fact]
    public void RouteMeaningIncludesBothNativeStoresAndPolicyWithStableRestartIdentity()
    {
        var source=new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS,new string('A',64),Guid.NewGuid());
        var target=new CosmicStorageIdentity(ProviderType.MongoDBOASIS,new string('B',64),Guid.NewGuid());
        var first=new HolonReplicationRouteBinding(source,target,"policy-one");
        Assert.Equal(first.TargetId,new HolonReplicationRouteBinding(source,target,"policy-one").TargetId);
        Assert.NotEqual(first.TargetId,new HolonReplicationRouteBinding(source,target,"policy-two").TargetId);
        Assert.NotEqual(first.TargetId,new HolonReplicationRouteBinding(source,new(target.Authority,target.EndpointFingerprint,Guid.NewGuid()),"policy-one").TargetId);
        Assert.NotEqual(first.SourceId,new HolonReplicationRouteBinding(new(source.Authority,source.EndpointFingerprint,Guid.NewGuid()),target,"policy-one").SourceId);
        Assert.NotEqual(first.TargetId,HolonReplicationRoutes.TargetId(target.Authority));
        Assert.InRange(first.TargetId.Length,1,256);Assert.InRange(first.SourceId.Length,1,256);
    }

    [Fact]
    public async Task NativeRegistrationPinsIdentityAcrossOutageUntilExplicitRebind()
    {
        var provider=new Mock<IOASISStorageProvider>();
        var native=provider.As<ICosmicStorageIdentityProvider>();
        var identity=new CosmicStorageIdentity(ProviderType.MongoDBOASIS,new string('A',64),Guid.NewGuid());
        provider.SetupGet(value=>value.ProviderType).Returns(new EnumValue<ProviderType>(identity.Authority));
        native.Setup(value=>value.CaptureStorageIdentityAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new OASISResult<CosmicStorageIdentity>(identity));
        var captured=await HolonReplicationRouteBinding.CapturePinnedAsync(provider.Object);
        native.Setup(value=>value.CaptureStorageIdentityAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("owned outage"));
        Assert.Same(captured,await HolonReplicationRouteBinding.CapturePinnedAsync(provider.Object));
        HolonReplicationRouteBinding.ForgetNativeBinding(provider.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>HolonReplicationRouteBinding.CapturePinnedAsync(provider.Object));
        Assert.False(HolonReplicationRouteBinding.IsPinned(provider.Object));
    }
}

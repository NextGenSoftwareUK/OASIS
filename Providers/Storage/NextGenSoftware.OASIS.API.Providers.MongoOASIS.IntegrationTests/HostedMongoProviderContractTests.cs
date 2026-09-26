using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class HostedMongoProviderContractTests
{
    [Fact]
    public void MongoProvider_AdvertisesEveryHostedSynchronizationContract()
    {
        var providerType = typeof(MongoProvider);

        Assert.True(typeof(IHostedHyperDriveProvider).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveSyncStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDrivePeerBindingStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveFanOutStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveMaintenanceStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveCommandStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveDomainMutationStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveDomainChangeCaptureStore).IsAssignableFrom(providerType));
        Assert.True(typeof(IHostedHyperDriveDomainBackfillStore).IsAssignableFrom(providerType));
    }
}

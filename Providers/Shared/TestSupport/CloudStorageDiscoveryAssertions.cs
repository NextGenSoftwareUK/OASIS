using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.Providers.TestSupport;

public static class CloudStorageDiscoveryAssertions
{
    // Capabilities describe supported services, not configured access or activation.
    public static void Verify(IOASISStorageProvider provider, ProviderCategory originalCategory)
    {
        var manager = new ProviderManager(null, new NextGenSoftware.OASIS.API.DNA.OASISDNA());
        var registered = manager.RegisterProvider(provider);
        if (!registered) throw new InvalidOperationException("The test provider could not be registered.");
        if (manager.GetCloudProviders().Count(value => ReferenceEquals(value, provider)) != 1
            || !manager.IsProviderCloud(provider.ProviderType.Value))
            throw new InvalidOperationException("Cloud storage must be discoverable exactly once without activation.");
        if (!manager.GetStorageProviders().Any(value => ReferenceEquals(value, provider))
            || provider.ProviderCategory.Value != originalCategory)
            throw new InvalidOperationException("Cloud discovery must preserve storage discovery and the legacy primary category.");
        foreach (var category in new[] { ProviderCategory.Storage, ProviderCategory.Cloud })
            if (provider.ProviderCapabilities.Count(value => value.Value == category) != 1)
                throw new InvalidOperationException("Storage/cloud capabilities must be explicit and unique.");
        if (provider.IsProviderActivated) throw new InvalidOperationException("Discovery must not activate the provider.");
    }
}

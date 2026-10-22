using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Read the existing host registry. Missing/inactive providers do not trigger activation or failover.</summary>
public sealed class CqlV1Providers : ICqlV1Providers
{
    public IOASISStorageProvider Resolve(ProviderType? requested)
    {
        var registry = ProviderManager.Instance;
        var provider = requested.HasValue ? registry.GetStorageProvider(requested.Value) : registry.CurrentStorageProvider;
        if (provider == null || !provider.IsProviderActivated) throw new InvalidOperationException("Provider unavailable.");
        return provider;
    }

    public HolonManager CreateManager(IOASISStorageProvider provider)
    {
        var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
        if (dna?.OASIS?.HyperDriveMode != NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2)
            throw new InvalidOperationException("CQL requires Hyperdrive V2.");
        return new HolonManager(provider, dna, ProviderManager.Instance);
    }
}

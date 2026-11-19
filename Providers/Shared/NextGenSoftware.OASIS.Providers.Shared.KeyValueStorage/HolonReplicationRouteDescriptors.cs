using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Provider-facing entry point for the canonical Core native route format.</summary>
public static class HolonReplicationRouteDescriptors
{
    public static string Capture(IEnumerable<HolonReplicationRouteBinding> routes)
        => NativeHolonReplicationRouteDescriptors.Capture(routes);

    public static IReadOnlyList<HolonReplicationRouteBinding> Read(string json)
        => NativeHolonReplicationRouteDescriptors.Read(json);
}

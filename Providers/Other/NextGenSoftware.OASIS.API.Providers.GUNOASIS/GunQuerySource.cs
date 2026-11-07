using System;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.GUNOASIS;

/// <summary>Bounded public reads from one co-located GUN Radisk teaching store.</summary>
public sealed class GunQuerySource(Uri sidecar, string readerToken, Guid probeId)
    : LoopbackPublicHolonQuerySource(PType.GUNOASIS, sidecar, readerToken, probeId)
{
}

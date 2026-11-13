using System;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using PType = NextGenSoftware.OASIS.API.Core.Enums.ProviderType;

namespace NextGenSoftware.OASIS.API.Providers.OrbitDBOASIS;

/// <summary>Bounded public reads from one co-located indexed OrbitDB teaching store.</summary>
public sealed class OrbitDbQuerySource(Uri sidecar, string readerToken, Guid probeId)
    : LoopbackPublicHolonQuerySource(PType.OrbitDBOASIS, sidecar, readerToken, probeId)
{
}

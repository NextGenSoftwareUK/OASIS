using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Immutable admitted graph intent. Native providers own its transaction and receipt storage.</summary>
public interface IAtomicHolonWritePlan
{
    IReadOnlyList<PlannedHolonWrite> Nodes {get;}
    IReadOnlyList<Guid> RootIds {get;}
    IReadOnlyDictionary<Guid,IReadOnlyList<Guid>> ChildIds {get;}
    string Fingerprint {get;}
    Guid OperationId {get;}
    ProviderType Authority {get;}
    ConditionalHolonOperation Operation {get;}
}

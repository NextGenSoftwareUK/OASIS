using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IBoundHolonReplicationOutboxSource
{
    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        if(!IsProviderActivated)return new OASISResult<IHolonReplicationReadinessOutbox>{IsError=true,ErrorCount=1,
            ErrorCode="COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED",Message="Activate the pinned source provider before capturing its native outbox."};
        try{return new OASISResult<IHolonReplicationReadinessOutbox>(new NeoHolonReplicationOutbox(_driver));}
        catch(Exception cause){return new OASISResult<IHolonReplicationReadinessOutbox>{IsError=true,ErrorCount=1,
            ErrorCode="COSMIC_OUTBOX_SOURCE_UNAVAILABLE",Message="The activated provider has no usable durable outbox binding.",Exception=cause};}
    }
    public OASISResult<IHolonReplicationRouteInventoryOutbox> GetBoundHolonReplicationOutbox(CosmicStorageIdentity expectedSource)
    {
        ArgumentNullException.ThrowIfNull(expectedSource);
        // Driver and captured endpoint belong to the same activation. Capture
        // that pair atomically; later operations retain this exact native owner.
        _lifecycleGate.Wait();
        try
        {
            if (!IsProviderActivated)
                return new() { IsError=true, ErrorCount=1, ErrorCode="COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED",
                    Message="Activate the native provider before capturing a bound outbox." };
            try { return new(new NeoHolonReplicationOutbox(_driver, expectedSource, _storageDriverEndpoint)); }
            catch (Exception cause)
            {
                return new() { IsError=true, ErrorCount=1, ErrorCode="COSMIC_OUTBOX_SOURCE_UNAVAILABLE",
                    Message="The activated provider cannot capture the expected native outbox.", Exception=cause };
            }
        }
        finally { _lifecycleGate.Release(); }
    }
}

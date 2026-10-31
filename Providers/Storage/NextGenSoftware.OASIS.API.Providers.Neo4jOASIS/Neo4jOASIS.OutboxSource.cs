using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : IHolonReplicationOutboxSource
{
    public OASISResult<IHolonReplicationReadinessOutbox> GetHolonReplicationOutbox()
    {
        if(!IsProviderActivated)return new OASISResult<IHolonReplicationReadinessOutbox>{IsError=true,ErrorCount=1,
            ErrorCode="COSMIC_OUTBOX_SOURCE_NOT_ACTIVATED",Message="Activate the pinned source provider before capturing its native outbox."};
        try{return new OASISResult<IHolonReplicationReadinessOutbox>(new NeoHolonReplicationOutbox(_driver));}
        catch(Exception cause){return new OASISResult<IHolonReplicationReadinessOutbox>{IsError=true,ErrorCount=1,
            ErrorCode="COSMIC_OUTBOX_SOURCE_UNAVAILABLE",Message="The activated provider has no usable durable outbox binding.",Exception=cause};}
    }
}

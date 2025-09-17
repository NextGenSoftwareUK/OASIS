﻿using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface provides methods to discover and interact with other nodes/peers on the distributed/decentralised network (ONET)
    // This will involve peer to peer communcation.
    public interface IOASISNETProvider : IOASISProvider
    {
        OASISResult<IEnumerable<IPlayer>> GetPlayersNearMe();
        OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(HolonType Type);
    }
}
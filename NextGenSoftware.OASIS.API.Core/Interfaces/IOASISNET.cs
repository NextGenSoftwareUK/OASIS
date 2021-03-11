using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // This interface provides methods to discover and interact with other nodes/peers on the distributed/decentralised network (ONET)
    // This will involve peer to peer communcation.
    public interface IOASISNET : IOASISProvider
    {
        IEnumerable<IPlayer> GetPlayersNearMe();
        IEnumerable<IHolon> GetHolonsNearMe(HolonType Type);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Player : Holon, IPlayer
    {
        public Player()
        {
            this.HolonType = HolonType.Player;
        }
    }
}

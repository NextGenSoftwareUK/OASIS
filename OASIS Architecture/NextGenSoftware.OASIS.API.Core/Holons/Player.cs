using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    [Obsolete("Player class is obsolete. Use Avatar instead.")]
    public class Player : Holon, IPlayer
    {
        public Player()
        {
            this.HolonType = HolonType.Player;
        }
        
        public string Username { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    /// <summary>
    /// Clan entity: a group of avatars with shared inventory (treasury).
    /// Stored as a holon with HolonType.Clan.
    /// </summary>
    public class Clan : Holon, IClan
    {
        public Clan()
        {
            HolonType = HolonType.Clan;
        }

        public Clan(Guid id) : base(id)
        {
            HolonType = HolonType.Clan;
        }

        public Guid OwnerAvatarId { get; set; }

        public IList<Guid> MemberIds { get; set; } = new List<Guid>();

        public IList<IInventoryItem> Inventory { get; set; } = new List<IInventoryItem>();
    }
}

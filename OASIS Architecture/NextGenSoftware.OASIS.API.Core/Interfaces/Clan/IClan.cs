using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    /// <summary>
    /// Clan entity: a group of avatars with shared inventory (e.g. treasury).
    /// </summary>
    public interface IClan : IHolon
    {
        /// <summary>Avatar Id of the clan owner/creator.</summary>
        Guid OwnerAvatarId { get; set; }

        /// <summary>Avatar Ids of clan members (including owner).</summary>
        IList<Guid> MemberIds { get; set; }

        /// <summary>Clan treasury / shared inventory.</summary>
        IList<IInventoryItem> Inventory { get; set; }
    }
}

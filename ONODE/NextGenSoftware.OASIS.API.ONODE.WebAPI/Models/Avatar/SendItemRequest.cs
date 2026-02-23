using System;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Request body for sending an inventory item to another avatar or to a clan.
    /// </summary>
    public class SendItemRequest
    {
        /// <summary>Target username or avatar Id (for send-to-avatar), or clan name (for send-to-clan).</summary>
        [Required]
        public string Target { get; set; } = string.Empty;

        /// <summary>Item name as stored in STAR inventory (e.g. silver_key, quake_pickup_weapon_shotgun).</summary>
        [Required]
        public string ItemName { get; set; } = string.Empty;

        /// <summary>Optional: specific inventory item Id (Guid) to send. When provided, this exact item is sent; otherwise items are matched by name.</summary>
        public Guid? ItemId { get; set; }

        /// <summary>Number of items to send (default 1).</summary>
        public int Quantity { get; set; } = 1;
    }
}

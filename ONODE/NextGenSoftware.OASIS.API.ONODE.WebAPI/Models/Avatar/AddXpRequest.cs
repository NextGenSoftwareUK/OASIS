using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Request body for adding experience points to the logged-in avatar (e.g. from game actions like killing monsters). Amount 0 returns current XP without change (used to refresh cache after beam-in).
    /// </summary>
    public class AddXpRequest
    {
        /// <summary>Amount of XP to add (0 = no change, just return current total).</summary>
        [Required]
        [Range(0, int.MaxValue)]
        public int Amount { get; set; }
    }
}

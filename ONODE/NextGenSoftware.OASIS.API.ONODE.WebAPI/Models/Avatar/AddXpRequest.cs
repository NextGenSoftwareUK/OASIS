using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Request body for adding experience points to the logged-in avatar (e.g. from game actions like killing monsters).
    /// </summary>
    public class AddXpRequest
    {
        /// <summary>Amount of XP to add. Must be positive.</summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Amount { get; set; }
    }
}

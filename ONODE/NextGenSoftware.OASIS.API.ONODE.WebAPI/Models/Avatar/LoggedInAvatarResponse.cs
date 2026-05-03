using System;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Logged-in avatar with XP for STAR API (GET avatar/current). Includes AvatarDetail.XP so clients can refresh XP cache.
    /// </summary>
    public class LoggedInAvatarResponse
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        /// <summary>Experience points from AvatarDetail. Used by STAR client to refresh XP after beam-in.</summary>
        public int XP { get; set; }
        /// <summary>Quest currently tracked in the game HUD. Persisted on AvatarDetail; restored after beam-in.</summary>
        [JsonPropertyName("activeQuestId")]
        public Guid? ActiveQuestId { get; set; }
        /// <summary>Objective currently active within the tracked quest. Persisted on AvatarDetail; restored after beam-in.</summary>
        [JsonPropertyName("activeObjectiveId")]
        public Guid? ActiveObjectiveId { get; set; }
    }
}

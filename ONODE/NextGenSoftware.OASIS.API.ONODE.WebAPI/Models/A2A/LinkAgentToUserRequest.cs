using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.A2A
{
    /// <summary>
    /// Request model for linking an agent to a user avatar
    /// </summary>
    public class LinkAgentToUserRequest
    {
        /// <summary>
        /// The owner avatar ID (User-type avatar that will own the agent)
        /// </summary>
        [Required]
        public string OwnerAvatarId { get; set; }
    }
}

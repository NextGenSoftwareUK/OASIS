using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    /// <summary>
    /// Minimal authentication response containing only essential fields for authentication.
    /// This reduces response size and prevents serialization issues with nested objects.
    /// </summary>
    public class MinimalAuthenticateResponse
    {
        /// <summary>
        /// JWT token for authenticated requests
        /// </summary>
        public string JwtToken { get; set; }

        /// <summary>
        /// Refresh token for obtaining new JWT tokens
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Avatar ID (GUID)
        /// </summary>
        public Guid AvatarId { get; set; }

        /// <summary>
        /// Username of the authenticated avatar
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Email address of the authenticated avatar
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Whether the avatar's email is verified
        /// </summary>
        public bool IsVerified { get; set; }

        /// <summary>
        /// Whether the avatar is currently beamed in
        /// </summary>
        public bool IsBeamedIn { get; set; }

        /// <summary>
        /// Last time the avatar beamed in
        /// </summary>
        public DateTime? LastBeamedIn { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Security
{
    /// <summary>
    /// Request body for the DID challenge-response authentication endpoint (POST /authenticate-did).
    /// </summary>
    public class AuthenticateWithDIDRequest
    {
        /// <summary>W3C DID of the avatar, e.g. did:oasis:&lt;avatarId&gt;</summary>
        [Required]
        public string DID { get; set; }

        /// <summary>Short-lived challenge string that was signed by the DID private key.</summary>
        [Required]
        public string Challenge { get; set; }

        /// <summary>
        /// Base64-encoded ECDsa P-256 IEEE P1363 signature (64 bytes: R[32]||S[32]).
        /// Produced by signing SHA-256(challenge) with the DID private key (P-256 curve).
        /// </summary>
        [Required]
        public string Signature { get; set; }
    }
}

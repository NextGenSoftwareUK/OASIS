namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaAvatarDto : SolanaBaseDto
{
    public Guid AvatarId { get; set; }
    public string Title { get; set; }
    public bool AcceptTerms { get; set; }
    public DateTime? PasswordReset { get; set; }
    public DateTime? Verified { get; set; }
    public DateTime? LastBeamedIn { get; set; }
    public DateTime? LastBeamedOut { get; set; }
    public bool IsBeamedIn { get; set; }
    public string DID { get; set; }
    public string DIDPublicKey { get; set; }
    public string HerzId { get; set; }
    public string HerzCountryCode { get; set; }
    public int HerzSequentialNumber { get; set; }
    public int HerzClearanceLevel { get; set; }
    public string HerzVoucherId { get; set; }
    public int HerzVouchesRemaining { get; set; }
    public DateTime? HerzIdAssignedDate { get; set; }
    public string HerzVoiceprintId { get; set; }
    public string HerzQeaProfile { get; set; }
    public string VoiceprintId { get; set; }
    public bool BiometricEnrolled { get; set; }
    public Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string> ProviderUsername { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string VerificationToken { get; set; }
    public string ResetToken { get; set; }
    public DateTime? ResetTokenExpires { get; set; }
    public string RefreshToken { get; set; }
    public List<NextGenSoftware.OASIS.API.Core.Objects.RefreshToken> RefreshTokens { get; set; } = new();
    public string ProviderWalletsJson { get; set; } = "{}";
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }
    public string AvatarType { get; set; }
    public string Description { get; set; }
    public Dictionary<string, object> MetaData { get; set; }
}

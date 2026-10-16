namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaAvatarDto : SolanaBaseDto
{
    public Guid AvatarId { get; set; }
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

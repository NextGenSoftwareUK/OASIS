namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaAvatarDto : SolanaBaseDto
{
    public Guid AvatarId { get; set; }
    public string UserName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
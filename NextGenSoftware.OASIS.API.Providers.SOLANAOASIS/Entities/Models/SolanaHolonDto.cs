namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public class SolanaHolonDto : SolanaBaseDto
{
    public Guid ParentOmniverseId { get; set; }
    public Guid ParentMultiverseId { get; set; }
    public Guid ParentUniverseId { get; set; }
}
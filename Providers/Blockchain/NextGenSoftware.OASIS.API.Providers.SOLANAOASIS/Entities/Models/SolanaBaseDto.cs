namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public abstract class SolanaBaseDto
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public Guid PreviousVersionId { get; set; }
    public bool IsDeleted { get; set; }
}
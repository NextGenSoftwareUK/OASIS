namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

public abstract class SolanaBaseDto
{
    // Persisted inside the encrypted memo, never inferred from optional payload fields.
    public string RecordKind => this switch
    {
        SolanaAvatarDto => "oasis.avatar.v1",
        SolanaAvatarDetailDto => "oasis.avatar-detail.v1",
        SolanaHolonDto => "oasis.holon.v1",
        _ => throw new InvalidOperationException("Unsupported Solana storage record type.")
    };
    public Guid Id { get; set; }
    public int Version { get; set; }
    public Guid PreviousVersionId { get; set; }
    public bool IsDeleted { get; set; }
}

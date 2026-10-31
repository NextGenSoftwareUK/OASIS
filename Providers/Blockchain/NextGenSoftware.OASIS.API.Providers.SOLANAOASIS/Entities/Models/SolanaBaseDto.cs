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
    // Part of the encrypted record, never a public lookup-index exemption.
    public Guid VersionId { get; set; }
    public bool IsPublic { get; set; }
    public Guid CreatedByAvatarId { get; set; }
    public Guid ModifiedByAvatarId { get; set; }
    public Guid DeletedByAvatarId { get; set; }
    public DateTime DeletedDate { get; set; }
    public Guid ParentHolonId { get; set; }
    public string CustomHolonType { get; set; }
    public Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string> ProviderUniqueStorageKey { get; set; } = new();
    public Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,string> PreviousVersionProviderUniqueStorageKey { get; set; } = new();
    public Dictionary<NextGenSoftware.OASIS.API.Core.Enums.ProviderType,Dictionary<string,string>> ProviderMetaData { get; set; } = new();
    public NextGenSoftware.OASIS.API.Core.Enums.ProviderType? CreatedProviderType { get; set; }
    public NextGenSoftware.OASIS.API.Core.Enums.OASISType? CreatedOASISType { get; set; }
    public int Version { get; set; }
    public Guid PreviousVersionId { get; set; }
    public bool IsDeleted { get; set; }

    internal bool HasValidCreationOrigin() =>
        (!CreatedProviderType.HasValue || Enum.IsDefined(typeof(NextGenSoftware.OASIS.API.Core.Enums.ProviderType), CreatedProviderType.Value)) &&
        (!CreatedOASISType.HasValue || Enum.IsDefined(typeof(NextGenSoftware.OASIS.API.Core.Enums.OASISType), CreatedOASISType.Value));
}

internal sealed class SolanaOriginValidationException : ArgumentException
{
    internal SolanaOriginValidationException() : base("Solana creation origin must contain defined provider and OASIS enum values, or be absent.") { }
}

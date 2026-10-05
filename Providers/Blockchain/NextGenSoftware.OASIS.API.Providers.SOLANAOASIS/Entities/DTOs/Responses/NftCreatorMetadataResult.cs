namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class NftCreatorMetadataResult
{
    public string PublicKey { get; set; }
    public bool Verified { get; set; }
    public byte Share { get; set; }

    public NftCreatorMetadataResult(Creator creator)
    {
        if (creator == null)
            return;

        PublicKey = creator.key;
        Verified = creator.verified;
        Share = creator.share;
    }
}
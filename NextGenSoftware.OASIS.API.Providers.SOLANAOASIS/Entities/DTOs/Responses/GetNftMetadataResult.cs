namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class GetNftMetadataResult
{
    public string Owner { get; set; } // should be Metadata program
    public string UpdateAuthority { get; set; }
    public string Mint { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Url { get; set; }
    public uint SellerFeeBasisPoints { get; set; }
    public IEnumerable<NftCreatorMetadataResult> Creators { get; set; }

    public GetNftMetadataResult(MetadataAccount metadataAccount)
    {
        ArgumentNullException.ThrowIfNull(metadataAccount);

        Owner = metadataAccount.owner.Key;  // should be Metadata program
        UpdateAuthority = metadataAccount.updateAuthority.Key;
        Mint = metadataAccount.mint;
        Name = metadataAccount.metadata.name;
        Symbol = metadataAccount.metadata.symbol;
        Url = metadataAccount.metadata.uri;
        SellerFeeBasisPoints = metadataAccount.metadata.sellerFeeBasisPoints;
        Creators = metadataAccount.metadata.creators.Select(creator => new NftCreatorMetadataResult(creator));
    }
}
namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Responses;

public sealed class GetNftResult
{
    public string Name { get; set; }
    public string Symbol { get; set; }
    public uint SellerFeeBasisPoints { get; set; }
    public string Url { get; set; }
    
    
    public GetNftResult(MetadataAccount metadataAccount)
    {
        ArgumentNullException.ThrowIfNull(metadataAccount);

        Name = metadataAccount.metadata.name;
        Symbol = metadataAccount.metadata.symbol;
        Url = metadataAccount.metadata.uri;
        SellerFeeBasisPoints = metadataAccount.metadata.sellerFeeBasisPoints;
    }
}
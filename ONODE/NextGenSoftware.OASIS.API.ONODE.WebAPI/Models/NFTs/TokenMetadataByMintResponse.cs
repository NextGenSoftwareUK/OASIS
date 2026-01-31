namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFTs;

/// <summary>
/// Response for GET /api/nft/metadata-by-mint.
/// Contains on-chain metadata (name, symbol, uri) plus image and description resolved from the token's metadata JSON.
/// Used to convert a memecoin/SPL token (e.g. from Solscan) into an NFT.
/// </summary>
public class TokenMetadataByMintResponse
{
    public string Mint { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Uri { get; set; }
    public string Image { get; set; }
    public string Description { get; set; }
}

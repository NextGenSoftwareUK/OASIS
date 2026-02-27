
namespace NextGenSoftware.OASIS.API.Core.Enums
{
    public enum NFTStandardType
    {
        ERC721,      // ETH chains only
        ERC1155,     // ETH chains only
        SPL,         // Solana — Metaplex NFT (supply=1, decimals=0, Metaplex PDA as mint authority)
        SPLFungible  // Solana — plain fungible SPL token (configurable decimals, OASIS wallet as mint authority)
    }
}
namespace Application.Contracts;

using Application.DTOs.FractionalNFT;

/// <summary>
/// Service for minting fractional ownership NFTs for real-world assets
/// </summary>
public interface IFractionalNFTService
{
    /// <summary>
    /// Mint a fractional NFT representing partial ownership of an asset
    /// </summary>
    Task<Result<MintFractionalNFTResponse>> MintFractionalNFTAsync(
        MintFractionalNFTRequest request,
        CancellationToken token = default);

    /// <summary>
    /// Calculate the number of tokens for a given fraction amount
    /// </summary>
    int CalculateTokenCount(decimal fractionAmount, int totalSupply);

    /// <summary>
    /// Convert UAT metadata to NFT metadata format
    /// </summary>
    Task<Result<NFTMetadata>> ConvertUATToNFTMetadataAsync(
        string assetId,
        decimal fractionAmount,
        CancellationToken token = default);

    /// <summary>
    /// Transfer NFT to buyer wallet
    /// </summary>
    Task<Result<TransferNFTResponse>> TransferNFTToBuyerAsync(
        string mintAddress,
        string buyerWallet,
        CancellationToken token = default);
}




namespace Common.Contracts.Nft;

/// <summary>
///     Contract for serializing off-chain NFT metadata into a format
///     suitable for decentralized storage solutions (e.g., IPFS, Arweave)
///     or centralized ones (e.g., CDN).
/// </summary>
public interface INftMetadataSerializer
{
    /// <summary>
    ///     Serializes the NFT object into a string representation and stores it at a URL.
    /// </summary>
    /// <param name="nft">The NFT object with base metadata.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>URI of the uploaded file or an error.</returns>
    Task<Result<string>> SerializeAsync(DTOs.Nft nft, CancellationToken token = default);
}

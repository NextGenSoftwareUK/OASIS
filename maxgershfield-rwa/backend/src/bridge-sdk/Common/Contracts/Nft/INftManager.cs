namespace Common.Contracts.Nft;

/// <summary>
///     Contract for minting and retrieving on-chain metadata for NFTs.
/// </summary>
/// <remarks>
///     This separates the business logic of minting from the specifics of the network (e.g., Solana, Radix).
/// </remarks>
public interface INftManager
{
    /// <summary>
    ///     Initiates the minting process for an NFT.
    /// </summary>
    /// <param name="nft">
    ///     The NFT object containing the metadata and attributes needed for minting. 
    ///     This includes properties like `name`, `symbol`, `royality`, and other metadata for the token.
    /// </param>
    /// <param name="token">
    ///     The cancellation token used to signal that the operation should be cancelled.
    ///     This token allows for graceful termination of the minting process, useful for long-running tasks or user interruptions.
    /// </param>
    /// <returns>
    ///     A result containing the transaction ID or an error message in case of failure.
    ///     The success value typically contains the mint address or transaction ID.
    /// </returns>
    Task<Result<NftMintingResponse>> MintAsync(DTOs.Nft nft, CancellationToken token = default);

    /// <summary>
    ///     Retrieves on-chain metadata for an NFT by its address.
    ///     Used for validating a successful mint or displaying token details.
    /// </summary>
    /// <param name="nftAddress">
    ///     The unique address (e.g., token ID or mint address) of the NFT on the blockchain.
    ///     This is used to query the on-chain data for the corresponding token.
    /// </param>
    /// <param name="token">
    ///     The cancellation token used to signal that the operation should be cancelled.
    ///     This allows for the operation to be aborted if needed, providing flexibility for the calling process.
    /// </param>
    /// <returns>
    ///     A result containing the NFT metadata if found, or an error message if retrieval fails.
    ///     The success value includes the full metadata of the NFT, such as name, symbol, and URL.
    /// </returns>
    Task<Result<DTOs.Nft>> GetMetadataAsync(string nftAddress, CancellationToken token = default);

    /// <summary>
    ///     Burns an NFT by its mint address.
    /// </summary>
    /// <param name="request">
    /// 
    /// </param>
    /// <param name="token">
    ///     Cancellation token for the operation.
    /// </param>
    /// <returns>
    ///     A result indicating whether the burn operation was successful.
    /// </returns>
    Task<Result<string>> BurnAsync(NftBurnRequest request, CancellationToken token = default);
}
namespace Common.DTOs;

public sealed record NftMintingResponse(
    string MintAccount,
    string TransactionHash,
    string Metadata,
    string Network);
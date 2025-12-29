namespace Common.DTOs;

public sealed record NftBurnRequest
{
    public required string MintAddress { get; init; }
    public required string OwnerPublicKey { get; init; }
    public required string OwnerPrivateKey { get; init; }
    public required string OwnerSeedPhrase { get; init; }
};
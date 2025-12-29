namespace Application.DTOs.RwaToken.Responses;

public readonly record struct UpdateRwaTokenResponse(
    Guid TokenId,
    string Title,
    decimal Price,
    string Network,
    int Royalty,
    string OwnerContact,
    string Metadata,
    string MintAccount,
    string TransactionHash,
    long Version
);
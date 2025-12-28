namespace Application.DTOs.RwaToken.Responses;

public sealed record GetRwaTokenDetailResponse(
    Guid TokenId,
    string Title,
    string AssetDescription,
    string ProofOfOwnershipDocument,
    string UniqueIdentifier,
    int Royalty,
    decimal Price,
    string? Network,
    string Image,
    string OwnerContact,
    NftAssetType AssetType,
    InsuranceStatus InsuranceStatus,
    GeoLocation? Geolocation,
    DateOnly ValuationDate,
    NftPropertyType PropertyType,
    double Area,
    int ConstructionYear,
    string Metadata,
    string MintAccount,
    string TransactionHash,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    string? OwnerEmail,
    string? OwnerUsername
);
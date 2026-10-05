namespace Application.DTOs.RwaToken.Responses;

public readonly record struct GetRwaTokensResponse(
    Guid TokenId,
    string Title,
    decimal Price,
    NftAssetType AssetType,
    InsuranceStatus InsuranceStatus,
    GeoLocation? Geolocation,
    string Image,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);
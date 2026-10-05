namespace Application.DTOs.RwaToken.Requests;

public record CreateRwaTokenRequest(
    string Title,
    string AssetDescription,
    string ProofOfOwnershipDocument,
    string UniqueIdentifier,
    int Royalty,
    decimal Price,
    string Network,
    string Image,
    string OwnerContact,
    NftAssetType AssetType,
    InsuranceStatus InsuranceStatus,
    GeoLocation Geolocation,
    DateOnly ValuationDate,
    NftPropertyType PropertyType,
    double Area,
    int ConstructionYear
);
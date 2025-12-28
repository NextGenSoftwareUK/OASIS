namespace Application.DTOs.RwaToken.Requests;

public record UpdateRwaTokenRequest(
    string Title,
    string AssetDescription,
    string ProofOfOwnershipDocument,
    int Royalty,
    decimal Price,
    string OwnerContact,
    NftAssetType AssetType,
    DateOnly ValuationDate
);
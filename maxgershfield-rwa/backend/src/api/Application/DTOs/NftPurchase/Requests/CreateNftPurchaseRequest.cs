namespace Application.DTOs.NftPurchase.Requests;

public readonly record struct CreateNftPurchaseRequest(
    Guid RwaId,
    string BuyerPubKey);
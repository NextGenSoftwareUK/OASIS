namespace Application.DTOs.NftPurchase.Requests;

public readonly record struct SendNftPurchaseTrRequest(
    string TransactionHash,
    string TransactionSignature);
namespace Application.DTOs.NftPurchase.Responses;

public readonly record struct SendTransactionResponse(
    string Status,
    string Message,
    int Code,
    SendTransactionData Data
);

public readonly record struct SendTransactionData(
    string TransactionId);
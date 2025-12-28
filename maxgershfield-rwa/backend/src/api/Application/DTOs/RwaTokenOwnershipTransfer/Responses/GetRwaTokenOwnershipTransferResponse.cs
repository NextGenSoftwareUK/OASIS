namespace Application.DTOs.RwaTokenOwnershipTransfer.Responses;

public record GetRwaTokenOwnershipTransferResponse(
    Guid RwaTokenId,
    Guid BuyerWalletId,
    string BuyerPublicKey,
    Guid SellerWalletId,
    string SellerPublicKey,
    decimal Price,
    DateTimeOffset TransactionDate,
    string TransactionHash);
namespace Application.Extensions.Mappers;

public static class RwaTokenOwnershipTransferMapper
{
    public static GetRwaTokenOwnershipTransferResponse ToRead(this RwaTokenOwnershipTransfer entity)
        => new(
            entity.RwaTokenId,
            entity.BuyerWalletId,
            entity.BuyerWallet.PublicKey,
            entity.SellerWalletId,
            entity.SellerWallet.PublicKey,
            entity.Price,
            entity.TransactionDate,
            entity.TransactionHash
        );
}
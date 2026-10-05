namespace Domain.Entities;

public sealed class RwaTokenOwnershipTransfer : BaseEntity
{
    public Guid RwaTokenId { get; set; }
    public RwaToken RwaToken { get; set; } = default!;

    public Guid BuyerWalletId { get; set; }
    public WalletLinkedAccount BuyerWallet { get; set; } = default!;

    public Guid SellerWalletId { get; set; }
    public VirtualAccount SellerWallet { get; set; } = default!;

    public decimal Price { get; set; }
    public DateTimeOffset TransactionDate { get; set; } = DateTimeOffset.UtcNow;
    public string TransactionHash { get; set; } = string.Empty;
    public string TransactionSignature { get; set; } = string.Empty;

    public RwaTokenOwnershipTransferStatus TransferStatus { get; set; }
}
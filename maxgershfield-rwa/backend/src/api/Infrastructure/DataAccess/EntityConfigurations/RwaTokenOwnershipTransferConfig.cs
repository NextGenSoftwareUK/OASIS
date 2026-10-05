namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class RwaTokenOwnershipTransferConfig : IEntityTypeConfiguration<RwaTokenOwnershipTransfer>
{
    public void Configure(EntityTypeBuilder<RwaTokenOwnershipTransfer> builder)
    {
        builder.HasOne(t => t.BuyerWallet)
            .WithMany(v => v.RwaTokenOwnershipTransferBuyers)
            .HasForeignKey(t => t.BuyerWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.SellerWallet)
            .WithMany(v => v.RwaTokenOwnershipTransferSellers)
            .HasForeignKey(t => t.SellerWalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
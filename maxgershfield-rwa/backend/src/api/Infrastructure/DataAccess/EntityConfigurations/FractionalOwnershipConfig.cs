namespace Infrastructure.DataAccess.EntityConfigurations;

using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class FractionalOwnershipConfig : IEntityTypeConfiguration<FractionalOwnership>
{
    public void Configure(EntityTypeBuilder<FractionalOwnership> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FractionAmount)
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(x => x.TokenCount)
            .IsRequired();

        builder.Property(x => x.MintAddress)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.MintTransactionHash)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.TransferTransactionHash)
            .HasMaxLength(255);

        builder.Property(x => x.MetadataUrl)
            .HasMaxLength(500)
            .IsRequired();

        // Relationships
        builder.HasOne(x => x.RwaToken)
            .WithMany()
            .HasForeignKey(x => x.RwaTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BuyerWalletLinkedAccount)
            .WithMany()
            .HasForeignKey(x => x.BuyerWalletLinkedAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.RwaTokenId);
        builder.HasIndex(x => x.MintAddress);
        builder.HasIndex(x => x.BuyerWalletLinkedAccountId);
    }
}




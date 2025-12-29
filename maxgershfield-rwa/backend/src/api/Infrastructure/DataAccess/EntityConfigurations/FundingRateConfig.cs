using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class FundingRateConfig : IEntityTypeConfiguration<FundingRate>
{
    public void Configure(EntityTypeBuilder<FundingRate> builder)
    {
        builder.ToTable("FundingRates");

        builder.HasKey(fr => fr.Id);

        builder.Property(fr => fr.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(fr => fr.Rate)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.HourlyRate)
            .IsRequired()
            .HasPrecision(18, 12);

        builder.Property(fr => fr.MarkPrice)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.SpotPrice)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.AdjustedSpotPrice)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.Premium)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.PremiumPercentage)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.BaseRate)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.CorporateActionAdjustment)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.LiquidityAdjustment)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.VolatilityAdjustment)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(fr => fr.CalculatedAt)
            .IsRequired();

        builder.Property(fr => fr.ValidUntil)
            .IsRequired();

        builder.Property(fr => fr.OnChainTransactionHash)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(fr => new { fr.Symbol, fr.CalculatedAt })
            .HasDatabaseName("IX_FundingRates_Symbol_CalculatedAt")
            .IsDescending(false, true); // CalculatedAt descending

        builder.HasIndex(fr => new { fr.Symbol, fr.ValidUntil })
            .HasDatabaseName("IX_FundingRates_Symbol_ValidUntil");
    }
}


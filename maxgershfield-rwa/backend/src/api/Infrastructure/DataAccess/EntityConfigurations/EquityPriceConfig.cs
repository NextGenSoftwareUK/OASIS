using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class EquityPriceConfig : IEntityTypeConfiguration<EquityPrice>
{
    public void Configure(EntityTypeBuilder<EquityPrice> builder)
    {
        builder.ToTable("EquityPrices");

        builder.HasKey(ep => ep.Id);

        builder.Property(ep => ep.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ep => ep.RawPrice)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(ep => ep.AdjustedPrice)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(ep => ep.Confidence)
            .IsRequired()
            .HasPrecision(5, 4);

        builder.Property(ep => ep.PriceDate)
            .IsRequired();

        builder.Property(ep => ep.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ep => ep.SourceBreakdownJson)
            .HasColumnType("nvarchar(max)");

        // Index
        builder.HasIndex(ep => new { ep.Symbol, ep.PriceDate })
            .HasDatabaseName("IX_EquityPrices_Symbol_PriceDate")
            .IsDescending(false, true); // PriceDate descending
    }
}


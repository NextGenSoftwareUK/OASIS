using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class CorporateActionConfig : IEntityTypeConfiguration<CorporateAction>
{
    public void Configure(EntityTypeBuilder<CorporateAction> builder)
    {
        builder.ToTable("CorporateActions");

        builder.HasKey(ca => ca.Id);

        builder.Property(ca => ca.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(ca => ca.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(ca => ca.ExDate)
            .IsRequired();

        builder.Property(ca => ca.RecordDate)
            .IsRequired();

        builder.Property(ca => ca.EffectiveDate)
            .IsRequired();

        builder.Property(ca => ca.SplitRatio)
            .HasPrecision(18, 8);

        builder.Property(ca => ca.DividendAmount)
            .HasPrecision(18, 8);

        builder.Property(ca => ca.DividendCurrency)
            .HasMaxLength(10);

        builder.Property(ca => ca.AcquiringSymbol)
            .HasMaxLength(20);

        builder.Property(ca => ca.ExchangeRatio)
            .HasPrecision(18, 8);

        builder.Property(ca => ca.DataSource)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(ca => ca.ExternalId)
            .HasMaxLength(200);

        builder.Property(ca => ca.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(ca => new { ca.Symbol, ca.EffectiveDate })
            .HasDatabaseName("IX_CorporateActions_Symbol_EffectiveDate");

        builder.HasIndex(ca => new { ca.Symbol, ca.ExDate })
            .HasDatabaseName("IX_CorporateActions_Symbol_ExDate");
    }
}


using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class RiskRecommendationConfig : IEntityTypeConfiguration<RiskRecommendation>
{
    public void Configure(EntityTypeBuilder<RiskRecommendation> builder)
    {
        builder.ToTable("RiskRecommendations");

        builder.HasKey(rr => rr.Id);

        builder.Property(rr => rr.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(rr => rr.PositionId)
            .HasMaxLength(200);

        builder.Property(rr => rr.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(rr => rr.CurrentLeverage)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(rr => rr.TargetLeverage)
            .IsRequired()
            .HasPrecision(18, 8);

        builder.Property(rr => rr.ReductionPercentage)
            .HasPrecision(5, 2);

        builder.Property(rr => rr.IncreasePercentage)
            .HasPrecision(5, 2);

        builder.Property(rr => rr.Reason)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(rr => rr.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(rr => rr.RecommendedBy)
            .IsRequired();

        builder.Property(rr => rr.Acknowledged)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(rr => rr.AcknowledgedBy)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(rr => new { rr.Symbol, rr.RecommendedBy })
            .HasDatabaseName("IX_RiskRecommendations_Symbol_RecommendedBy")
            .IsDescending(false, true); // RecommendedBy descending

        builder.HasIndex(rr => new { rr.Symbol, rr.Acknowledged })
            .HasDatabaseName("IX_RiskRecommendations_Symbol_Acknowledged");

        builder.HasIndex(rr => rr.PositionId)
            .HasDatabaseName("IX_RiskRecommendations_PositionId")
            .HasFilter("[PositionId] IS NOT NULL");

        builder.HasIndex(rr => rr.ValidUntil)
            .HasDatabaseName("IX_RiskRecommendations_ValidUntil")
            .HasFilter("[ValidUntil] IS NOT NULL");
    }
}


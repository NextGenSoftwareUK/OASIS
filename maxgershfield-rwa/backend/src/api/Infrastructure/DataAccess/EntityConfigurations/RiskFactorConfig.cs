using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class RiskFactorConfig : IEntityTypeConfiguration<RiskFactor>
{
    public void Configure(EntityTypeBuilder<RiskFactor> builder)
    {
        builder.ToTable("RiskFactors");

        builder.HasKey(rf => rf.Id);

        builder.Property(rf => rf.RiskWindowId)
            .IsRequired();

        builder.Property(rf => rf.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(rf => rf.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rf => rf.Impact)
            .IsRequired()
            .HasPrecision(5, 4);

        builder.Property(rf => rf.EffectiveDate)
            .IsRequired();

        builder.Property(rf => rf.Details)
            .HasColumnType("nvarchar(max)");

        // Relationship with RiskWindow
        builder.HasOne(rf => rf.RiskWindow)
            .WithMany(rw => rw.Factors)
            .HasForeignKey(rf => rf.RiskWindowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rf => rf.RiskWindowId)
            .HasDatabaseName("IX_RiskFactors_RiskWindowId");

        builder.HasIndex(rf => new { rf.Type, rf.EffectiveDate })
            .HasDatabaseName("IX_RiskFactors_Type_EffectiveDate");
    }
}


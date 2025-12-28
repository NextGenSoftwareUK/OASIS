using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class RiskWindowConfig : IEntityTypeConfiguration<RiskWindow>
{
    public void Configure(EntityTypeBuilder<RiskWindow> builder)
    {
        builder.ToTable("RiskWindows");

        builder.HasKey(rw => rw.Id);

        builder.Property(rw => rw.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(rw => rw.Level)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(rw => rw.StartDate)
            .IsRequired();

        builder.Property(rw => rw.EndDate)
            .IsRequired();

        // Relationship with RiskFactor
        builder.HasMany(rw => rw.Factors)
            .WithOne(rf => rf.RiskWindow)
            .HasForeignKey(rf => rf.RiskWindowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(rw => new { rw.Symbol, rw.StartDate })
            .HasDatabaseName("IX_RiskWindows_Symbol_StartDate");

        builder.HasIndex(rw => new { rw.Symbol, rw.EndDate })
            .HasDatabaseName("IX_RiskWindows_Symbol_EndDate");

        builder.HasIndex(rw => new { rw.StartDate, rw.EndDate })
            .HasDatabaseName("IX_RiskWindows_StartDate_EndDate");
    }
}


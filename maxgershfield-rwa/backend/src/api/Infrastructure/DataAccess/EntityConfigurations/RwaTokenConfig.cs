namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class RwaTokenConfig : IEntityTypeConfiguration<RwaToken>
{
    public void Configure(EntityTypeBuilder<RwaToken> builder)
    {
        builder.OwnsOne(x => x.Geolocation, geo =>
        {
            geo.Property(g => g.Latitude).HasColumnName("Latitude");
            geo.Property(g => g.Longitude).HasColumnName("Longitude");
        });
    }
}
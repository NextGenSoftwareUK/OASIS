using Role = Domain.Entities.Role;

namespace Infrastructure.DataAccess.EntityConfigurations;

public sealed class RoleConfig : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.DataAccess;

/// <summary>
/// Design-time factory for Entity Framework migrations
/// </summary>
public sealed class DataContextFactory : IDesignTimeDbContextFactory<DataContext>
{
    public DataContext CreateDbContext(string[] args)
    {
        // Use default connection string for design-time (migrations)
        // This will be overridden at runtime with actual configuration
        string connectionString = "server=localhost;port=5432;database=oasis_bridge_db;user id=postgres;password=123456";

        // Create options builder
        var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new DataContext(optionsBuilder.Options);
    }
}

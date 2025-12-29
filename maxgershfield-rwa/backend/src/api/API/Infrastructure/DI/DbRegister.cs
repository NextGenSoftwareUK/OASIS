namespace API.Infrastructure.DI;

/// <summary>
/// This class provides an extension method to register the DbContext service into the application's dependency injection (DI) container.
/// It is specifically configured to use PostgreSQL as the database provider.
/// </summary>
public static class DbRegister
{
    /// <summary>
    /// Registers the DbContext service, allowing the application to interact with the PostgreSQL database.
    /// It reads the connection string from the configuration file and configures the DbContext to use Npgsql (PostgreSQL provider).
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to register services.</param>
    /// <returns>The WebApplicationBuilder instance with the DbContext service registered, enabling method chaining.</returns>
    public static WebApplicationBuilder AddDbService(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<DataContext>(configure =>
        {
            configure.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
        });

        return builder;
    }
}
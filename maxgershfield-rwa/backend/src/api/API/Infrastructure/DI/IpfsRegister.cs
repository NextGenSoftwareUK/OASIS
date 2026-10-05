namespace API.Infrastructure.DI;

/// <summary>
/// This class registers IPFS-related service dependencies into the application's DI container.
/// It configures services required for interacting with the IPFS (InterPlanetary File System),
/// such as IPFS clients and settings.
/// </summary>
public static class IpfsRegister
{
    /// <summary>
    /// Registers the IPFS service dependencies in the DI container.
    /// It loads IPFS-related configuration from the application settings and adds services
    /// necessary for working with IPFS nodes (e.g., uploading, retrieving, and pinning files).
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to register services with.</param>
    /// <returns>The WebApplicationBuilder instance with the IPFS service dependencies registered.</returns>
    public static WebApplicationBuilder AddIpfsService(this WebApplicationBuilder builder)
    {
        builder.Services.AddIpfs(builder.Configuration);

        builder.Services.AddScoped<IIpfsService, IpfsService>();

        return builder;
    }
}
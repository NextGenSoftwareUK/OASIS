using BuildingBlocks.Extensions;
using Ipfs.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FileStorage;

/// <summary>
/// Provides extension methods to configure and register decentralized file storages such as IPFS.
/// </summary>
public static class DecentralizedFileStorages
{
    private const string Pin = "ipfsOptions:pin";
    private const string ChunkSize = "ipfsOptions:chunkSize";
    private const string NodeUrl = "ipfsOptions:url";
    private const string GatewayUrl = "ipfsOptions:gatewayUrl";

    /// <summary>
    /// Registers IPFS file storage implementation and its dependencies.
    /// Binds configuration from the provided <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The service collection to which the IPFS services will be added.</param>
    /// <param name="configuration">The application's configuration object used to retrieve IPFS settings.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if required configuration values (pin, chunkSize, url) are missing or invalid.
    /// </exception>
    public static IServiceCollection AddIpfs(this IServiceCollection services, IConfiguration configuration)
    {
        bool pin = configuration.GetRequiredBool(Pin);
        int chunkSize = configuration.GetRequiredInt(ChunkSize);
        string nodeUrl = configuration.GetRequiredString(NodeUrl);
        string gatewayUrl = configuration.GetRequiredString(GatewayUrl);

        ConfigureIpfsFileStorage(services, pin, chunkSize, nodeUrl, gatewayUrl);
        return services;
    }

    /// <summary>
    /// Configures and registers all necessary services to enable IPFS file storage.
    /// </summary>
    /// <param name="services">The service collection for dependency injection.</param>
    /// <param name="pin">Whether to pin files when adding to IPFS.</param>
    /// <param name="chunkSize">The buffer size used when reading files from IPFS.</param>
    /// <param name="url">The URL of the IPFS node.</param>
    /// <param name="gatewayUrl"></param>
    private static void ConfigureIpfsFileStorage(IServiceCollection services, bool pin, int chunkSize, string url, string gatewayUrl)
    {
        services.Configure<IpfsOptions>(options =>
        {
            options.Pin = pin;
            options.ChunkSize = chunkSize;
            options.GatewayUrl = gatewayUrl;
        });

        services.AddScoped<IpfsClient>(_ =>
            new()
            {
                ApiUri = new Uri(url, UriKind.Absolute),
            }
        );

        services.AddScoped<IDecentralizedFileStorage, IpfsFileStorage>();
    }
}

namespace API.Infrastructure.DI;

/// <summary>
/// This class provides an extension method for configuring response compression services
/// in a WebApplicationBuilder. It enhances the performance of web applications by reducing
/// the size of HTTP responses sent from the server to the client, thus optimizing bandwidth
/// usage and improving loading times for end-users.
/// </summary>
public static class ResponseCompressionRegister
{
    /// <summary>
    /// Adds response compression services to the application. This method configures
    /// two compression algorithms—Brotli and Gzip—for compressing HTTP responses.
    /// It also enables compression for HTTPS traffic to ensure that secure connections
    /// benefit from reduced response sizes as well.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance used to register services.</param>
    /// <returns>The WebApplicationBuilder instance to allow method chaining.</returns>
    public static WebApplicationBuilder AddResponseCompressionService(this WebApplicationBuilder builder)
    {
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        builder.Services.Configure<BrotliCompressionProviderOptions>(o =>
        {
            o.Level = CompressionLevel.Fastest;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(o =>
        {
            o.Level = CompressionLevel.Fastest;
        });
        return builder;
    }
}
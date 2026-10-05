namespace API.Infrastructure.DI;

/// <summary>
/// This class registers various services necessary for the application to function properly.
/// It integrates a wide range of services into the application's dependency injection container
/// to enable essential functionality such as authentication, database access, email communication, 
/// rate limiting, and more.
/// </summary>
public static class RegisterServices
{
    /// <summary>
    /// Registers various services needed for the application.
    /// This method configures and registers services related to authentication, 
    /// HTTP handling, database connectivity, email services, and other essential functionalities.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to register services with.</param>
    /// <returns>The WebApplicationBuilder instance with services registered.</returns>
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        builder.AddNftService();
        builder.AddDbService();
        builder.AddJwtService();
        builder.AddHttpService();
        builder.AddCorsService();
        builder.AddIpfsService();
        builder.AddCacheService();
        builder.AddEmailService();
        builder.AddBridgeService();
        builder.AddWorkerService();
        builder.AddCustomServices();
        builder.AddSwaggerService();
        builder.AddRateLimiterService();
        builder.Services.AddSwaggerGen();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.AddResponseCompressionService();
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)
                );
            });

        builder.Services.Configure<FormOptions>(options => { options.MultipartBodyLengthLimit = 2073741824; });

        return builder;
    }
}
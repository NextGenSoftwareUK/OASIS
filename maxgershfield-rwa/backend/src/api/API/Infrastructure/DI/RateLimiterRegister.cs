namespace API.Infrastructure.DI;

/// <summary>
/// This class registers rate-limiting services to the application's dependency injection container.
/// It ensures that requests to the API are limited based on the client's IP address and the specific route being accessed.
/// </summary>
public static class RateLimiterRegister
{
    private static readonly TimeSpan
        WindowSize = TimeSpan.FromMinutes(1);

    private const int RequestLimit = 20;

    /// <summary>
    /// Registers the rate-limiting service for the application to limit the number of requests that clients can make.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to register services with.</param>
    /// <returns>The WebApplicationBuilder instance with the rate-limiting services registered.</returns>
    public static WebApplicationBuilder AddRateLimiterService(this WebApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                string ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "UnknownIP";
                string routePath = httpContext.Request.Path.Value ?? "/";

                string partitionKey = $"{ipAddress}:{routePath}";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RequestLimit,
                        Window = WindowSize,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return builder;
    }
}
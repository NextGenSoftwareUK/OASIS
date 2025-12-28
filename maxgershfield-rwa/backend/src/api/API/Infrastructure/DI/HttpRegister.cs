namespace API.Infrastructure.DI;

/// <summary>
/// This class registers HTTP-related services within the DI container for the application.
/// It configures essential services for HTTP requests, including HttpClient, HttpContextAccessor, and HTTP request logging.
/// </summary>
public static class HttpRegister
{
    /// <summary>
    /// Registers HTTP-related services in the DI container.
    /// This includes setting up the HttpClient for making HTTP requests, HttpContextAccessor for accessing HTTP context,
    /// and enabling HTTP logging for monitoring and debugging HTTP requests.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance to register services with.</param>
    /// <returns>The WebApplicationBuilder instance with HTTP-related services registered.</returns>
    public static WebApplicationBuilder AddHttpService(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient(HttpClientNames.SolShiftClient, client =>
        {
            client.BaseAddress = new Uri(builder.Configuration.GetRequiredString(HttpClientNames.SolShiftClient));
            client.Timeout = TimeSpan.FromSeconds(20);
        });

        builder.Services.AddHttpClient(HttpClientNames.OASISClient, client =>
        {
            string oasisApiUrl = builder.Configuration["OASIS:ApiUrl"] ?? "https://api.oasisplatform.world";
            client.BaseAddress = new Uri(oasisApiUrl);
            client.Timeout = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("OASIS:Timeout", 30));
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            
            string? apiKey = builder.Configuration["OASIS:ApiKey"];
            if (!string.IsNullOrEmpty(apiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
            }
        });

        builder.Services.AddHttpContextAccessor();

        builder.Services.AddHttpLogging(options => { options.LoggingFields = HttpLoggingFields.All; });

        return builder;
    }
}
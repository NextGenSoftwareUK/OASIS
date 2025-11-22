namespace ScGen.Lib.Shared.Extensions.DI;

public static class RegisterServices
{
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerGen();
        builder.Services.AddHttpClient();
        builder.Services.AddScGenServices();
        builder.Services.AddProblemDetails();
        builder.Services.AddFluentValidation();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(
                new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false)
            );
        });

        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        builder.Services.Configure<BrotliCompressionProviderOptions>(o => { o.Level = CompressionLevel.Fastest; });

        builder.Services.Configure<GzipCompressionProviderOptions>(o => { o.Level = CompressionLevel.Fastest; });

        builder.Services.Configure<EthereumOptions>(
            builder.Configuration.GetSection(KeyNames.Ethereum));

        builder.Services.Configure<SolanaOptions>(
            builder.Configuration.GetSection(KeyNames.Solana));

        builder.Services.Configure<RadixOptions>(
            builder.Configuration.GetSection(KeyNames.Radix));

        builder.Services.Configure<X402Options>(
            builder.Configuration.GetSection("X402"));

        // Add CORS for frontend
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("http://localhost:3000", "http://localhost:3001", "http://localhost:3002", "https://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return builder;
    }
}
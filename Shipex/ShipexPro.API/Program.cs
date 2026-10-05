using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Repositories;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.IShip;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks;
using NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Connectors.QuickBooks.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Configure CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:8000", "http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register Shipex Pro Services
builder.Services.AddSingleton<IShipexProRepository>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration["ShipexPro:ConnectionString"] 
        ?? configuration["ConnectionStrings:ShipexProMongoDB"] 
        ?? "mongodb://localhost:27017";
    var dbName = configuration["ShipexPro:DatabaseName"] 
        ?? configuration["ConnectionStrings:ShipexProDatabase"] 
        ?? "ShipexPro";
    
    var context = new ShipexProMongoDbContext(connectionString, dbName);
    return new ShipexProMongoRepository(context);
});

// Register Encryption and Secret Vault
builder.Services.AddSingleton<NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.IEncryptionService, NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.EncryptionService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.EncryptionService(configuration);
});

builder.Services.AddScoped<NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.ISecretVaultService, NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.SecretVaultService>(sp =>
{
    var encryption = sp.GetRequiredService<NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.IEncryptionService>();
    var repository = sp.GetRequiredService<IShipexProRepository>();
    return new NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.SecretVaultService(encryption, repository);
});

// Register iShip Connector
builder.Services.AddScoped<IShipConnectorService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var secretVault = sp.GetRequiredService<NextGenSoftware.OASIS.API.Providers.ShipexProOASIS.Services.ISecretVaultService>();
    var logger = sp.GetService<ILogger<IShipConnectorService>>();
    var baseUrl = configuration["iShip:ApiUrl"] ?? "https://api.iship.com";
    return new IShipConnectorService(baseUrl, secretVault, null, logger);
});

// Register QuickBooks Service
builder.Services.AddScoped<IQuickBooksService, QuickBooksBillingWorker>(sp =>
{
    var repository = sp.GetRequiredService<IShipexProRepository>();
    var logger = sp.GetService<ILogger<QuickBooksBillingWorker>>();
    return new QuickBooksBillingWorker(repository, logger);
});

// Register Rate Service
builder.Services.AddScoped<IRateService, RateService>(sp =>
{
    var repository = sp.GetRequiredService<IShipexProRepository>();
    var iShipConnector = sp.GetRequiredService<IShipConnectorService>();
    var markupEngine = new RateMarkupEngine();
    var logger = sp.GetService<ILogger<RateService>>();
    return new RateService(iShipConnector, markupEngine, repository, logger);
});

// Register Shipment Service
builder.Services.AddScoped<IShipmentService, ShipmentService>(sp =>
{
    var repository = sp.GetRequiredService<IShipexProRepository>();
    var iShipConnector = sp.GetRequiredService<IShipConnectorService>();
    var quickBooksService = sp.GetRequiredService<IQuickBooksService>();
    var logger = sp.GetService<ILogger<ShipmentService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var webhookBaseUrl = configuration["ShipexPro:WebhookBaseUrl"] ?? "https://api.shipexpro.com/api/shipexpro/webhooks/iship";
    return new ShipmentService(iShipConnector, repository, quickBooksService, logger, webhookBaseUrl);
});

// Register Markup Configuration Service
builder.Services.AddScoped<MarkupConfigurationService>(sp =>
{
    var repository = sp.GetRequiredService<IShipexProRepository>();
    return new MarkupConfigurationService(repository);
});

// Register QuickBooks OAuth Service
builder.Services.AddScoped<QuickBooksOAuthService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetService<ILogger<QuickBooksOAuthService>>();
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    
    var config = new QuickBooksOAuthConfig
    {
        ClientId = configuration["QuickBooks:ClientId"] ?? "",
        ClientSecret = configuration["QuickBooks:ClientSecret"] ?? "",
        RedirectUri = configuration["QuickBooks:RedirectUri"] ?? "",
        UseSandbox = configuration.GetValue<bool>("QuickBooks:SandboxMode", true)
    };
    
    var httpClient = httpClientFactory.CreateClient();
    return new QuickBooksOAuthService(config, logger, httpClient, null);
});

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Configure URLs
app.Urls.Clear();
app.Urls.Add("https://localhost:5005");
app.Urls.Add("http://localhost:5006");

app.Run();

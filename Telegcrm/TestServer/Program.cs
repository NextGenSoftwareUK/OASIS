using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Telegcrm.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get MongoDB connection - try multiple sources
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? builder.Configuration["MongoDB:ConnectionString"]
    ?? builder.Configuration["MongoDBAtlas:ConnectionString"]
    ?? "mongodb+srv://OASISWEB4:Uppermall1%21@oasisweb4.ifxnugb.mongodb.net/?retryWrites=true&w=majority&appName=OASISWeb4";

Console.WriteLine($"🔗 MongoDB Connection: {mongoConnectionString.Substring(0, Math.Min(50, mongoConnectionString.Length))}...");

// Register CRM service
try
{
    builder.Services.AddSingleton(new TelegramCrmService(mongoConnectionString));
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to initialize CRM service: {ex.Message}");
    Console.WriteLine("");
    Console.WriteLine("💡 To fix this:");
    Console.WriteLine("   1. Set MONGODB_CONNECTION_STRING environment variable, OR");
    Console.WriteLine("   2. Use MongoDB Atlas (see MONGODB_ATLAS_SETUP.md), OR");
    Console.WriteLine("   3. Start local MongoDB: brew services start mongodb-community");
    Console.WriteLine("");
    Console.WriteLine("   Server will start but CRM features will not work.");
    throw;
}

// CORS for testing
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();
app.UseRouting();
app.UseStaticFiles(); // Serve static files from wwwroot

// Map default route to index.html
app.MapFallbackToFile("index.html");

app.MapControllers();

Console.WriteLine("🚀 Telegram CRM Test Server Starting...");
Console.WriteLine($"📡 API will be available at: http://localhost:5001");
Console.WriteLine($"📚 Swagger UI: http://localhost:5001/swagger");
Console.WriteLine("");

app.Run("http://localhost:5001");

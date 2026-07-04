using System.Reflection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB9 OASIS Singularity Layer API" },
        Description = "WEB9 - the Singularity Layer made literal: a live unified status aggregator that probes WEB4-WEB8 and reports back as one - the network observing itself.",
        Title = "WEB9 OASIS Singularity Layer API",
        Version = "v1"
    });

    var path = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(path))
        c.IncludeXmlComments(path, includeControllerXmlComments: true);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.WithOrigins(
            "https://oasisomniverse.one",
            "https://app.oasisomniverse.one",
            "https://oasisweb4.one",
            "https://oasisweb5.one",
            "http://localhost:3000",
            "http://localhost:5173")
        .AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WEB9 OASIS Singularity Layer API v1"));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();
app.UseMiddleware<NextGenSoftware.OASIS.Web9.WebAPI.Middleware.JwtMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

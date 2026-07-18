using System.Reflection;
using Microsoft.OpenApi.Models;
using NextGenSoftware.OASIS.OASISBootLoader;

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
        Description = $"WEB9 v{OASISBootLoader.WEB9APIVersion} - the Singularity Layer made literal: a live unified status aggregator that probes WEB4-WEB8 and reports back as one - the network observing itself." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB9/NextGenSoftware.OASIS.Web9.WebAPI/WEB9%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB9 OASIS Singularity Layer API v", OASISBootLoader.WEB9APIVersion),
        Version = "v1"
    });

    var path = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(path))
        c.IncludeXmlComments(path, includeControllerXmlComments: true);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", string.Concat("WEB9 OASIS Singularity Layer API v", OASISBootLoader.WEB9APIVersion)));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();
app.UseMiddleware<NextGenSoftware.OASIS.Web9.WebAPI.Middleware.JwtMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

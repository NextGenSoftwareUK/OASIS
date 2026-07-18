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
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB10 OASIS Source Layer API" },
        Description = $"WEB10 v{OASISBootLoader.WEB10APIVersion} - The Source / WEB0 made literal: the root source-of-truth endpoint, combining the foundational OASIS runtime/version identity with WEB9's live unified status across WEB4-WEB8." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB10/NextGenSoftware.OASIS.Web10.WebAPI/WEB10%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB10 OASIS Source Layer API v", OASISBootLoader.WEB10APIVersion),
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
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", string.Concat("WEB10 OASIS Source Layer API v", OASISBootLoader.WEB10APIVersion)));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();
app.UseMiddleware<NextGenSoftware.OASIS.Web10.WebAPI.Middleware.JwtMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

using System.Reflection;
using Path = System.IO.Path;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web8.WebAPI.GraphQL;
using NextGenSoftware.OASIS.Web8.WebAPI.GraphQL.Types;
using NextGenSoftware.OASIS.Web8.WebAPI.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var enableGenericExceptionHandling = builder.Configuration.GetValue<bool>("EnableGenericExceptionHandling",
    bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Web8ExceptionFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 1024;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddGrpc();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<MeshNodeType>()
    .AddType<MeshLinkType>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB8 OASIS Inter-Galactic Layer API" },
        Description = $"WEB8 v{OASISBootLoader.WEB8APIVersion} - the fractal holonic mesh-networking layer. Real node registry, Dijkstra shortest-path routing with self-healing failover, and a universal protocol bridge." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB8/NextGenSoftware.OASIS.Web8.WebAPI/WEB8%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB8 OASIS Inter-Galactic Layer API v", OASISBootLoader.WEB8APIVersion),
        Version = "v1"
    });

    var xmlFiles = new[] { $"{Assembly.GetExecutingAssembly().GetName().Name}.xml", "NextGenSoftware.OASIS.Web8.Core.xml" };

    foreach (var xml in xmlFiles)
    {
        var path = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path))
            c.IncludeXmlComments(path, includeControllerXmlComments: true);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", string.Concat("WEB8 OASIS Inter-Galactic Layer API v", OASISBootLoader.WEB8APIVersion)));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError($"Unexpected error in WEB8 middleware: {ex.Message}", ex, includeStackTrace: true);

        bool isValidationError = ex is OASISException || ex is System.Text.Json.JsonException || ex is ArgumentException;
        var errorResult = new OASISResult<object> { IsError = true, Exception = ex };

        errorResult.Message = enableGenericExceptionHandling ? $"Invalid args were passed. {ex.Message}" : ex.Message;

        if (!enableGenericExceptionHandling)
            errorResult.DetailedMessage = ex.ToString();

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = isValidationError ? 400 : 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errorResult));
        }
    }
});

app.UseAuthorization();
app.UseMiddleware<NextGenSoftware.OASIS.Web8.WebAPI.Middleware.JwtMiddleware>();

app.MapControllers();
app.MapGraphQL();
app.MapGrpcService<MeshGrpcService>();
app.MapGrpcService<ProtocolBridgeGrpcService>();
app.MapGet("/", () => Results.Redirect("/swagger"));

var dnaPath = OASISBootLoader.OASISDNAPath ?? Path.Combine(AppContext.BaseDirectory, "OASIS_DNA.json");
var bootResult = await OASISBootLoader.BootOASISAsync(dnaPath);
if (bootResult.IsError)
    OASISErrorHandling.HandleError($"Warning: WEB8 OASIS boot failed: {bootResult.Message}");

app.Run();

public class Web8ExceptionFilter : IExceptionFilter
{
    private readonly bool _enableGenericExceptionHandling;

    public Web8ExceptionFilter(IConfiguration configuration)
    {
        _enableGenericExceptionHandling = configuration.GetValue<bool>("EnableGenericExceptionHandling",
            bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));
    }

    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;
        OASISErrorHandling.HandleError($"Error in {context.ActionDescriptor.DisplayName}: {ex.Message}", ex, includeStackTrace: true);

        bool isValidationError = ex is OASISException || ex is ArgumentException || ex is ArgumentNullException;
        var errorResult = new OASISResult<object> { IsError = true, Exception = ex };

        errorResult.Message = _enableGenericExceptionHandling
            ? $"Invalid args were passed to {context.ActionDescriptor.DisplayName}. {ex.Message}"
            : ex.Message;

        if (!_enableGenericExceptionHandling)
            errorResult.DetailedMessage = ex.ToString();

        context.Result = isValidationError
            ? new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(errorResult)
            : new Microsoft.AspNetCore.Mvc.ObjectResult(errorResult) { StatusCode = 500 };

        context.ExceptionHandled = true;
    }
}

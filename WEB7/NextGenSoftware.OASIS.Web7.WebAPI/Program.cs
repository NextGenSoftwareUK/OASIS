using System.Reflection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web7.WebAPI.GraphQL;
using NextGenSoftware.OASIS.Web7.WebAPI.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    var port = int.Parse(Environment.GetEnvironmentVariable("PORT") ?? "8080");
    options.ListenAnyIP(port, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2);
});

Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var enableGenericExceptionHandling = builder.Configuration.GetValue<bool>("EnableGenericExceptionHandling",
    bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Web7ExceptionFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 1024;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddGrpc();
builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB7 OASIS Symbiotic Layer API" },
        Description = $"WEB7 v{OASISBootLoader.WEB7APIVersion} - the non-invasive human/AI symbiosis layer. Real bio-signal DSP (EEG/HRV/GSR), consent-gated sessions enforcing the Borg-Free pledge, and collective consciousness spaces." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB7/NextGenSoftware.OASIS.Web7.WebAPI/WEB7%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB7 OASIS Symbiotic Layer API v", OASISBootLoader.WEB7APIVersion),
        Version = "v1"
    });

    var xmlFiles = new[] { $"{Assembly.GetExecutingAssembly().GetName().Name}.xml", "NextGenSoftware.OASIS.Web7.Core.xml" };

    foreach (var xml in xmlFiles)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path))
            c.IncludeXmlComments(path, includeControllerXmlComments: true);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var cfg = OASISBootLoader.OASISDNA?.OASIS?.Security?.RateLimiting;
        if (cfg == null || !cfg.Enabled)
            return RateLimitPartition.GetNoLimiter("no-limit");

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(clientIp,
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit          = cfg.RequestsPerWindow,
                Window               = TimeSpan.FromSeconds(cfg.WindowSeconds),
                SegmentsPerWindow    = cfg.WindowSegments,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit           = cfg.QueueLimit
            });
    });

    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(
            "{\"IsError\":true,\"Message\":\"Too many requests. Please slow down and try again later.\"}", token);
    };
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", string.Concat("WEB7 OASIS Symbiotic Layer API v", OASISBootLoader.WEB7APIVersion)));

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseRateLimiter();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError($"Unexpected error in WEB7 middleware: {ex.Message}", ex, includeStackTrace: true);

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
app.UseMiddleware<NextGenSoftware.OASIS.Web7.WebAPI.Middleware.JwtMiddleware>();
app.UseMiddleware<NextGenSoftware.OASIS.Web7.WebAPI.Middleware.ApiKeyMiddleware>();

app.MapControllers();
app.MapGraphQL();
app.MapGrpcService<CollectiveConsciousnessGrpcService>();
app.MapGrpcService<SymbiosisGrpcService>();
app.MapGet("/", () => Results.Redirect("/swagger"));

var dnaPath = OASISBootLoader.OASISDNAPath ?? System.IO.Path.Combine(AppContext.BaseDirectory, "OASIS_DNA.json");
var bootResult = await OASISBootLoader.BootOASISAsync(dnaPath);
if (bootResult.IsError)
    OASISErrorHandling.HandleError($"Warning: WEB7 OASIS boot failed: {bootResult.Message}");

app.Run();

public class Web7ExceptionFilter : IExceptionFilter
{
    private readonly bool _enableGenericExceptionHandling;

    public Web7ExceptionFilter(IConfiguration configuration)
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

using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;

var builder = WebApplication.CreateBuilder(args);

// Ensure OASIS_DNA.json is resolved from the app output directory in local runs.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);

var enableGenericExceptionHandling = builder.Configuration.GetValue<bool>("EnableGenericExceptionHandling",
    bool.Parse(Environment.GetEnvironmentVariable("ENABLE_GENERIC_EXCEPTION_HANDLING") ?? "true"));

builder.Services.AddControllers(options =>
{
    options.Filters.Add<Web6ExceptionFilter>();
})
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.MaxDepth = 1024;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB6 OASIS AI API" },
        Description = "WEB6 - the unified AI abstraction and aggregation layer, built on top of WEB4 (OASIS API/ONODE - identity, karma, COSMIC ORM) and WEB5 (STAR ODK/STARNET - holon graph, OAPP ecosystem). One API, every AI provider, with the FAHRN (Fractal Adaptive Holonic Reasoning Network) controller agent and the Holonic BRAID shared reasoning-graph memory.",
        Title = "WEB6 OASIS AI API",
        Version = "v1"
    });

    var xmlFiles = new[]
    {
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
        "NextGenSoftware.OASIS.Web6.Core.xml"
    };

    foreach (var xml in xmlFiles)
    {
        var path = Path.Combine(AppContext.BaseDirectory, xml);
        if (File.Exists(path))
            c.IncludeXmlComments(path, includeControllerXmlComments: true);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "WEB6 OASIS AI API v1");
});

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");

// Global exception handler - logs and returns a real OASISResult-shaped error body.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError($"Unexpected error in WEB6 middleware: {ex.Message}", ex, includeStackTrace: true);

        bool isValidationError = ex is OASISException || ex is System.Text.Json.JsonException || ex is ArgumentException;
        var errorResult = new OASISResult<object> { IsError = true, Exception = ex };

        errorResult.Message = enableGenericExceptionHandling
            ? $"Invalid args were passed. {ex.Message}"
            : ex.Message;

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

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger"));

// Boot the OASIS engine (loads OASIS_DNA.json and activates the default storage provider) before serving requests.
var dnaPath = OASISBootLoader.OASISDNAPath ?? Path.Combine(AppContext.BaseDirectory, "OASIS_DNA.json");
var bootResult = await OASISBootLoader.BootOASISAsync(dnaPath);
if (bootResult.IsError)
    OASISErrorHandling.HandleError($"Warning: WEB6 OASIS boot failed: {bootResult.Message}");

// Auto-seed FAHRN with one reasoning agent per OpenServ catalog model so the reasoning network and Holonic
// BRAID have agents to score/dispatch/braid against out of the box. Controlled by OASIS_DNA.json ->
// OASIS.Web6.FAHRN.AutoSeedOpenServAgentsOnStartup (default true); only runs when SERV_API_KEY is set.
OASISDNA dna = OASISBootLoader.OASISDNA;
if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SERV_API_KEY"))
    && (dna?.OASIS?.Web6?.FAHRN?.AutoSeedOpenServAgentsOnStartup ?? true))
{
    try
    {
        FAHRNManager fahrnManager = new FAHRNManager(Guid.Empty, dna);
        var seedResult = await fahrnManager.SeedDefaultOpenServAgentsAsync();
        if (seedResult.IsError)
            OASISErrorHandling.HandleError($"Warning: WEB6 FAHRN OpenServ agent auto-seed failed: {seedResult.Message}");
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError($"Warning: WEB6 FAHRN OpenServ agent auto-seed threw an exception: {ex.Message}", ex);
    }
}

app.Run();

public class Web6ExceptionFilter : IExceptionFilter
{
    private readonly bool _enableGenericExceptionHandling;

    public Web6ExceptionFilter(IConfiguration configuration)
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

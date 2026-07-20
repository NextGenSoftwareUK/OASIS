using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc.Filters;
using ModelContextProtocol.Server;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices;

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

builder.Services.AddGrpc();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();

// Priority 1: HTTP MCP transport — makes the entire OASIS tool surface reachable by any MCP client (Claude.ai, OpenAI, etc.)
try
{
    builder.Services
        .AddMcpServer(options =>
        {
            options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
            {
                Name = "oasis-web4-to-web10-mcp",
                Version = "2.0.0"
            };
        })
        .WithHttpTransport()
        .WithToolsFromAssembly(System.Reflection.Assembly.GetExecutingAssembly());
}
catch { /* MCP tools assembly may not be referenced directly — skip if unavailable */ }
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Contact = new OpenApiContact { Email = "ourworld@nextgensoftware.co.uk", Name = "WEB6 OASIS AI API" },
        Description = $"WEB6 v{OASISBootLoader.WEB6APIVersion} — the unified AI abstraction and aggregation layer, built on top of WEB4 (OASIS API/ONODE - identity, karma, COSMIC ORM) and WEB5 (STAR ODK/STARNET - holon graph, OAPP ecosystem). One API, every AI provider, with the FAHRN (Fractal Adaptive Holonic Reasoning Network) controller agent, Holonic BRAID shared reasoning-graph memory, SkillOpt self-evolving agent skills, ML.NET in-process AutoML, DID/Verifiable Credentials, ACP/ANP/gRPC/GraphQL multi-protocol orchestration, karma-gated AI access, and full OpenTelemetry observability." +
            "<br><a href='https://github.com/dellamsOmega/OASIS/blob/master/WEB6/NextGenSoftware.OASIS.Web6.WebAPI/WEB6%20API%20RELEASE%20HISTORY.md'>Release History</a>",
        Title = string.Concat("WEB6 OASIS AI API v", OASISBootLoader.WEB6APIVersion),
        Version = "v2"
    });

    c.CustomSchemaIds(t => t.FullName);
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    // JsonObject / JsonNode have no built-in Swashbuckle mapping — tell it to treat them as free-form objects
    c.MapType<System.Text.Json.Nodes.JsonObject>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true });
    c.MapType<System.Text.Json.Nodes.JsonNode>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "object", AdditionalPropertiesAllowed = true });
    c.MapType<System.Text.Json.Nodes.JsonArray>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "array" });

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
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v2/swagger.json", string.Concat("WEB6 OASIS AI API v", OASISBootLoader.WEB6APIVersion));
});

if (!string.Equals(app.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase))
    app.UseHttpsRedirection();

app.UseCors("AllowAll");

// JwtMiddleware MUST be registered here. It was missing in the original Web6 Program.cs
// (unlike Web4/Web5 which had it), causing every JWT to be silently ignored and all
// authenticated requests to fail with 401. It must come AFTER UseCors and BEFORE
// UseAuthorization/MapControllers so the avatar context is populated before AuthorizeAttribute runs.
app.UseMiddleware<NextGenSoftware.OASIS.Web6.WebAPI.Middleware.JwtMiddleware>();

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

// Priority 18: WebSocket support for bidirectional agent sessions
app.UseWebSockets();

app.MapControllers();
app.MapGrpcService<AiGrpcService>();
app.MapGrpcService<AgentsGrpcService>();
app.MapGrpcService<MemoryGrpcService>();
app.MapGrpcService<NetworkGrpcService>();
app.MapGrpcService<IdentityGrpcService>();
app.MapGrpcService<TelemetryGrpcService>();
app.MapGet("/", () => Results.Redirect("/swagger"));

// Priority 1: mount /mcp HTTP endpoint
try { app.MapMcp("/mcp"); } catch { }

// Priority 1: MCP discovery document
app.MapGet("/.well-known/mcp.json", () => Results.Json(new
{
    schema_version = "v1",
    name_for_human = "OASIS WEB4–WEB10",
    name_for_model = "oasis",
    description_for_human = "Universal AI abstraction layer — Web4 identity/data, Web5 apps/quests, Web6 AI/FAHRN, Web7 symbiosis, Web8 mesh, Web9 singularity, Web10 source.",
    description_for_model = "Access all OASIS capabilities: avatar/karma (web4), quests/missions/OAPPs (web5), AI completion/FAHRN dispatch/holonic-braid (web6), bio-signal symbiosis (web7), galactic mesh routing (web8), unified status (web9), root identity (web10).",
    auth = new { type = "bearer" },
    api = new { type = "mcp", url = "https://api.web6.oasisomniverse.one/mcp" }
}));

// Priority 4: A2A Agent Card
app.MapGet("/.well-known/agent.json", () => Results.Json(new
{
    name = "OASIS WEB6 FAHRN",
    description = "Fractal Adaptive Holonic Reasoning Network — universal AI abstraction and aggregation layer (Web4–Web10)",
    url = "https://api.web6.oasisomniverse.one",
    version = "2.0.0",
    documentationUrl = "https://web6.oasisomniverse.one",
    capabilities = new { streaming = true, pushNotifications = false, stateTransitionHistory = false },
    defaultInputModes = new[] { "text/plain", "application/json" },
    defaultOutputModes = new[] { "text/plain", "application/json" },
    skills = new object[]
    {
        new { id = "fahrn-solve", name = "FAHRN Solve", description = "Full pipeline: classify → avatar context → dispatch → BRAID → answer + reasoning trace", inputModes = new[] { "text/plain" }, outputModes = new[] { "application/json" } },
        new { id = "ai-complete", name = "AI Completion", description = "Route chat completions across 15+ AI providers with automatic failover", inputModes = new[] { "application/json" }, outputModes = new[] { "application/json" } },
        new { id = "holonic-braid", name = "Holonic BRAID", description = "Shared reasoning graph memory — lookup or create Mermaid execution graphs per task type", inputModes = new[] { "application/json" }, outputModes = new[] { "application/json" } },
        new { id = "oasis-data", name = "OASIS Data (Web4)", description = "Avatar, karma, wallet, NFT, holon CRUD via COSMIC ORM across 40+ storage providers" }
    }
}));

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

// Priority 22: self-register this Web6 instance as an MCP orchestrator adapter so FAHRN agents can call back.
if (dna?.OASIS?.Web6?.SelfRegisterAsOrchestrator ?? true)
{
    try
    {
        string selfUrl = Environment.GetEnvironmentVariable("WEB6_PUBLIC_URL")
            ?? "https://api.web6.oasisomniverse.one/mcp";

        OrchestratorManager orchManager = new OrchestratorManager(Guid.Empty, dna);
        OASISResult<List<NextGenSoftware.OASIS.Web6.Core.Models.OrchestratorAdapterConfig>> existing = await orchManager.GetAdaptersAsync();

        bool alreadyRegistered = existing.Result?.Any(a =>
            string.Equals(a.EndpointUrl, selfUrl, StringComparison.OrdinalIgnoreCase)) ?? false;

        if (!alreadyRegistered)
        {
            await orchManager.RegisterAdapterAsync(new NextGenSoftware.OASIS.Web6.Core.Models.OrchestratorAdapterConfig
            {
                Name = "OASIS-WEB6-SELF",
                Protocol = NextGenSoftware.OASIS.Web6.Core.Enums.OrchestratorProtocolType.MCP,
                EndpointUrl = selfUrl,
                ExtraConfig = new Dictionary<string, string> { ["tool"] = "web6_fahrn_solve" }
            });
        }
    }
    catch (Exception ex)
    {
        OASISErrorHandling.HandleError($"Warning: WEB6 self-registration as orchestrator failed: {ex.Message}", ex);
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

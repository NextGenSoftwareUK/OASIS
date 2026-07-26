using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.OASISBootLoader;

// MCP communicates over stdio - stdout must carry ONLY protocol JSON-RPC, so all OASIS boot/log
// output (which writes to stdout via Console.WriteLine in LoggingManager) must be redirected to stderr
// before anything else runs.
var originalOut = Console.Out;
Console.SetOut(Console.Error);

var dnaPath = OASISBootLoader.OASISDNAPath ?? Path.Combine(AppContext.BaseDirectory, "OASIS_DNA.json");
var bootResult = await OASISBootLoader.BootOASISAsync(dnaPath);

if (bootResult.IsError)
    OASISErrorHandling.HandleError($"Warning: OASIS MCP Server boot failed: {bootResult.Message}");

Console.SetOut(originalOut);

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new ModelContextProtocol.Protocol.Implementation
        {
            Name = "oasis-web4-to-web10-mcp",
            Version = "1.0.0"
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

var app = builder.Build();
await app.RunAsync();

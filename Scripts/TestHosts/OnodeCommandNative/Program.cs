using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;
using NextGenSoftware.OASIS.ONODE.Service.Services;

static void Require(bool value, string invariant)
{
    if (!value) throw new InvalidOperationException(invariant);
}
var owned = Path.Combine(Path.GetTempPath(), "oasis-onode-command-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(owned);
var dna = Path.Combine(owned, "OASISDNA.json");
var priorPath = Environment.GetEnvironmentVariable("OASISDNA_PATH");
const string canary = "owned-public-view-exclusion-canary";
try
{
    Environment.SetEnvironmentVariable("OASISDNA_PATH", dna);
    await File.WriteAllTextAsync(dna, JsonSerializer.Serialize(new
    {
        OASIS = new
        {
            HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
            StorageProviders = new
            {
                AutoFailOverEnabled = true, AutoReplicationEnabled = false,
                AutoLoadBalanceEnabled = true, AutoFailOverLocalProvidersEnabled = false,
                ProviderMethodCallTimeOutSeconds = 7,
                MongoDBOASIS = new { ConnectionString = canary }
            },
            Secret = canary
        }
    }));
    var config = new SupervisorConfig
    {
        NodeId = "owned-command-node", AvatarId = "owned-command-avatar",
        OASISDNAPath = dna, Web4AccessToken = canary,
        Services = new Dictionary<string, ServiceConfig>()
    };
    var options = Options.Create(config);
    var logs = new LogAggregator(10);
    var supervisor = new ProcessSupervisor(options, logs, NullLogger<ProcessSupervisor>.Instance);
    using var metrics = new MetricsCollector(supervisor, options, NullLogger<MetricsCollector>.Instance);
    var providers = new ProviderService(options, NullLogger<ProviderService>.Instance);
    var executor = new CommandExecutor(supervisor, logs, providers, metrics, options,
        NullLogger<CommandExecutor>.Instance);
    var command = new CommandHolon { TargetNodeId = config.NodeId, Command = CommandType.GetConfig };
    var view = await executor.ExecuteAsync(command, CancellationToken.None);
    Require(view != null && !view.Contains(canary, StringComparison.Ordinal), "Public command leaked excluded fields.");
    using (var document = JsonDocument.Parse(view!))
    {
        var root = document.RootElement;
        Require(root.GetProperty("schemaVersion").GetString() == "onode-public-config-v1", "Public schema differs.");
        Require(root.GetProperty("nodeId").GetString() == config.NodeId, "Public node identity differs.");
        Require(root.GetProperty("oasisDna").GetProperty("hyperDriveMode").GetString() == NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2, "Public mode differs from shipping DNA contract.");
        Require(root.GetProperty("oasisDna").GetProperty("storage").GetProperty("autoFailOverEnabled").GetBoolean(),
            "Saved routing setting was not projected.");
        Require(root.GetProperty("oasisDna").GetProperty("timeouts").GetProperty("providerMethodCallTimeOutSeconds").GetInt32() == 7,
            "Saved timeout was not projected.");
    }
    Require(!JsonSerializer.Serialize(config).Contains(canary, StringComparison.Ordinal), "Runtime token was serialized.");
    command.Command = CommandType.GetMetrics;
    using (var document = JsonDocument.Parse((await executor.ExecuteAsync(command, CancellationToken.None))!))
        Require(document.RootElement.GetProperty("totalPeers").GetInt32() == metrics.Aggregate.TotalPeers,
            "Metrics command did not serialize the actual collector.");
    command.TargetNodeId = "other-node";
    try
    {
        await executor.ExecuteAsync(command, CancellationToken.None);
        throw new InvalidOperationException("Wrong-node command was accepted.");
    }
    catch (UnauthorizedAccessException) { }
    Console.WriteLine("ONODE_COMMAND_NATIVE: public configuration exclusion, identity, saved routing/timeout, runtime-token exclusion, actual collector and wrong-node rejection passed; no HTTP/browser/provider acceptance inferred.");
}
finally
{
    Environment.SetEnvironmentVariable("OASISDNA_PATH", priorPath);
    if (File.Exists(dna)) File.Delete(dna);
    Directory.Delete(owned);
    Console.WriteLine("ONODE_COMMAND_CLEANUP: owned DNA and directory absent.");
}

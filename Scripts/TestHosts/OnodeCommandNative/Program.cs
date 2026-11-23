using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using NextGenSoftware.OASIS.ONODE.Service.Api;
using NextGenSoftware.OASIS.ONODE.Client;
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
var tokenPath = Path.Combine(owned, "supervisor-token");
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
            OASISHyperDriveConfig = new NextGenSoftware.OASIS.API.Core.Configuration.OASISHyperDriveConfig
            {
                AutoFailoverEnabled = false, AutoReplicationEnabled = true,
                AutoLoadBalancingEnabled = false,
                AutoFailoverProviders = new List<string> { "MongoDBOASIS", "SQLLiteDBOASIS" }
            },
            StorageProviders = new
            {
                AutoFailOverEnabled = true, AutoFailOverProviders = "MongoDBOASIS,SQLLiteDBOASIS", AutoReplicationEnabled = false,
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
        var policy = root.GetProperty("oasisDna").GetProperty("routingPolicy");
        Require(policy.GetProperty("source").GetString() == "OASIS.OASISHyperDriveConfig", "Modern routing owner differs.");
        Require(!policy.GetProperty("settings").GetProperty("autoFailoverEnabled").GetBoolean(),
            "Modern view incorrectly used the legacy switch.");
        Require(policy.GetProperty("settings").GetProperty("autoFailoverProviders")[1].GetString() == "SQLLiteDBOASIS",
            "Modern routing provider order differs.");
        Require(root.GetProperty("oasisDna").GetProperty("storage").GetProperty("autoFailOverEnabled").GetBoolean(),
            "Saved routing setting was not projected.");
        Require(root.GetProperty("oasisDna").GetProperty("timeouts").GetProperty("providerMethodCallTimeOutSeconds").GetInt32() == 7,
            "Saved timeout was not projected.");
    }
    var savedUtf8 = await File.ReadAllBytesAsync(dna);
    await File.WriteAllBytesAsync(dna, new byte[] { 0xef, 0xbb, 0xbf }.Concat(savedUtf8).ToArray());
    var modernProviders = providers.GetProviders("failover");
    Require(modernProviders.Count == 2 && modernProviders.All(item => item.Role == "failover"),
        "Canonical modern provider view differs.");
    var beforeRoleRevision = modernProviders[0].Revision;
    Require(await providers.SetPriorityAsync("SQLLiteDBOASIS", 1, CancellationToken.None, "failover", beforeRoleRevision),
        "Modern role order was not saved.");
    Require(providers.GetProviders("failover").Single(item => item.ProviderType == "SQLLiteDBOASIS").Priority == 1,
        "Modern role order did not persist.");
    var modernAfterOrder = await File.ReadAllTextAsync(dna);
    try
    {
        await providers.SetEnabledAsync("MongoDBOASIS", false, CancellationToken.None, "failover", beforeRoleRevision);
        throw new InvalidOperationException("Stale routing edit was accepted.");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("reload before editing", StringComparison.Ordinal)) { }
    Require(await File.ReadAllTextAsync(dna) == modernAfterOrder, "Stale edit changed saved configuration.");
    Require(modernAfterOrder.Contains(canary, StringComparison.Ordinal), "Role edit lost unrelated credential configuration.");
    var legacy = System.Text.Json.Nodes.JsonNode.Parse(await File.ReadAllTextAsync(dna))!;
    legacy["OASIS"]!["HyperDriveMode"] = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.Legacy;
    legacy["OASIS"]!.AsObject().Remove("OASISHyperDriveConfig");
    await File.WriteAllTextAsync(dna, legacy.ToJsonString());
    using (var document = JsonDocument.Parse((await executor.ExecuteAsync(command, CancellationToken.None))!))
    {
        var policy = document.RootElement.GetProperty("oasisDna").GetProperty("routingPolicy");
        Require(policy.GetProperty("source").GetString() == "OASIS.StorageProviders", "Legacy routing owner differs.");
        Require(policy.GetProperty("settings").GetProperty("autoFailOverEnabled").GetBoolean(), "Legacy switch differs.");
        Require(policy.GetProperty("settings").GetProperty("autoFailOverProviders")[1].GetString() == "SQLLiteDBOASIS",
            "Legacy provider order differs.");
    }
    Require(await providers.SetPriorityAsync("SQLLiteDBOASIS", 1, CancellationToken.None, "failover"),
        "Legacy order was not saved.");
    Require(await providers.SetEnabledAsync("MongoDBOASIS", false, CancellationToken.None, "failover"),
        "Legacy role membership was not saved.");
    var legacyBeforeRejectedEdit = await File.ReadAllTextAsync(dna);
    try
    {
        await providers.SetEnabledAsync("SQLLiteDBOASIS", false, CancellationToken.None, "failover");
        throw new InvalidOperationException("An enabled feature lost its final provider.");
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("at least one provider", StringComparison.Ordinal)) { }
    Require(await File.ReadAllTextAsync(dna) == legacyBeforeRejectedEdit, "Rejected empty policy changed configuration.");
    Require(!Directory.EnumerateFiles(owned, "*.routing-*.tmp").Any(), "Owned routing temporary output remains.");
    Console.WriteLine("ONODE_ROUTING_FILE: canonical modern/legacy membership/order persisted, stale revision and enabled-empty policy rejected, unrelated credential fields preserved; saved configuration only, no provider activation or applied-runtime acceptance.");
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
    // Real MVC routes over a real loopback socket, with shipping authentication/client and no background workers.
    var tokenService = new TokenService();
    tokenService.Initialise(tokenPath);
    var oldToken = await File.ReadAllTextAsync(tokenPath);
    var builder = WebApplication.CreateBuilder();
    builder.Logging.ClearProviders();
    builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
    builder.Services.AddSingleton(options);
    builder.Services.AddSingleton(logs);
    builder.Services.AddSingleton(supervisor);
    builder.Services.AddSingleton(metrics);
    builder.Services.AddSingleton(providers);
    builder.Services.AddSingleton(tokenService);
    builder.Services.AddSingleton<MetricsHistoryService>();
    builder.Services.AddControllers().AddApplicationPart(typeof(SupervisorController).Assembly);
    await using (var app = builder.Build())
    {
        app.MapControllers();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await app.StartAsync(deadline.Token);
        try
        {
            var origin = app.Urls.Single();
            using var raw = new HttpClient { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(5) };
            using (var denied = await raw.GetAsync("/supervisor/public-config", deadline.Token))
                Require(denied.StatusCode == HttpStatusCode.Unauthorized, "Anonymous public configuration was accepted.");
            using var client = new SupervisorClient(new Uri(origin).Port, tokenPath);
            Require(client.IsAvailable, "Real supervisor client could not authenticate status.");
            var httpView = await client.GetPublicConfigurationAsync(deadline.Token);
            Require(httpView.GetProperty("nodeId").GetString() == config.NodeId, "HTTP projection node differs.");
            Require(!httpView.GetRawText().Contains(canary, StringComparison.Ordinal), "HTTP projection leaked excluded fields.");
            Require(await client.RotateTokenAsync(deadline.Token), "Acknowledged token rotation failed.");
            raw.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", oldToken.Trim());
            using (var rejected = await raw.GetAsync("/supervisor/public-config", deadline.Token))
                Require(rejected.StatusCode == HttpStatusCode.Unauthorized, "Old token survived acknowledged rotation.");
            await client.GetPublicConfigurationAsync(deadline.Token);
            var httpRouting = await client.GetProviderRoutingAsync("failover", deadline.Token);
            var httpRoutingRevision = httpRouting[0].Revision;
            var membershipAck = await client.SetProviderMembershipAsync("MongoDBOASIS", true, "failover", httpRoutingRevision, deadline.Token);
            Require(membershipAck.ReloadRequired, "Saved membership did not require runtime reload.");
            var savedRouting = await client.GetProviderRoutingAsync("failover", deadline.Token);
            Require(savedRouting.Single(item => item.ProviderType == "MongoDBOASIS").Priority == 2,
                "Real SDK membership edit did not persist.");
            try
            {
                await client.SetProviderRoutingPriorityAsync("MongoDBOASIS", 1, "failover", httpRoutingRevision, deadline.Token);
                throw new InvalidOperationException("HTTP stale revision was accepted.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict) { }
            await client.SetProviderRoutingPriorityAsync("MongoDBOASIS", 1, "failover", savedRouting[0].Revision, deadline.Token);
            var replicationBefore = await client.GetProviderRoutingAsync("replication", deadline.Token);
            await client.SetProviderMembershipAsync("MongoDBOASIS", true, "replication", replicationBefore[0].Revision, deadline.Token);
            var replicationAfter = await client.GetProviderRoutingAsync("replication", deadline.Token);
            Require(replicationAfter.Single(item => item.ProviderType == "MongoDBOASIS").IsEnabled,
                "Replication membership was not saved by the real SDK.");
            var balancingBefore = await client.GetProviderRoutingAsync("loadBalancing", deadline.Token);
            await client.SetProviderMembershipAsync("SQLLiteDBOASIS", true, "loadBalancing", balancingBefore[0].Revision, deadline.Token);
            Require((await client.GetProviderRoutingAsync("loadBalancing", deadline.Token))
                .Single(item => item.ProviderType == "SQLLiteDBOASIS").IsEnabled, "Load-balancing role used the wrong configuration key.");
            var commandRole = new CommandHolon { TargetNodeId = config.NodeId, Command = CommandType.GetProviders,
                Payload = JsonSerializer.Serialize(new { role = "replication" }) };
            using (var commandRoleView = JsonDocument.Parse((await executor.ExecuteAsync(commandRole, deadline.Token))!))
                Require(commandRoleView.RootElement.EnumerateArray().All(item => item.GetProperty("role").GetString() == "replication"),
                    "Command transport lost the selected routing role.");
            var replicationLatest = await client.GetProviderRoutingAsync("replication", deadline.Token);
            commandRole.Command = CommandType.DisableProvider;
            commandRole.Payload = JsonSerializer.Serialize(new { providerType = "MongoDBOASIS", role = "replication",
                expectedRevision = replicationLatest[0].Revision });
            await executor.ExecuteAsync(commandRole, deadline.Token);
            Require(!(await client.GetProviderRoutingAsync("replication", deadline.Token))
                .Single(item => item.ProviderType == "MongoDBOASIS").IsEnabled, "Command edit did not persist in its selected role.");
            Require((await client.GetProviderRoutingAsync("failover", deadline.Token))
                .Single(item => item.ProviderType == "MongoDBOASIS").Priority == 1, "A different role edit changed failover order.");
            Console.WriteLine("ONODE_ROUTING_HTTP: shipping SDK and actual MVC saved all three roles, ordered failover, rejected stale revision with409, preserved independent roles and returned reload-required; actual command role read/edit passed. No applied-runtime or WEB4 bridge/browser acceptance.");
            await File.WriteAllTextAsync(dna, "{", deadline.Token);
            try
            {
                await client.GetPublicConfigurationAsync(deadline.Token);
                throw new InvalidOperationException("Malformed saved configuration returned success.");
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.ServiceUnavailable) { }
            Console.WriteLine("ONODE_PUBLIC_HTTP: real MVC/loopback/client authenticated projection, anonymous denial, credential exclusion, acknowledged rotation, old-token denial and malformed-config error passed; no WEB4 bridge/browser/provider acceptance inferred.");
        }
        finally { await app.StopAsync(CancellationToken.None); }
    }
    Console.WriteLine("ONODE_COMMAND_NATIVE: public configuration exclusion, identity, saved routing/timeout, runtime-token exclusion, actual collector and wrong-node rejection passed; no HTTP/browser/provider acceptance inferred.");
}
finally
{
    Environment.SetEnvironmentVariable("OASISDNA_PATH", priorPath);
    if (File.Exists(dna)) File.Delete(dna);
    if (File.Exists(tokenPath)) File.Delete(tokenPath);
    Directory.Delete(owned);
    Console.WriteLine("ONODE_COMMAND_CLEANUP: owned DNA, token and directory absent.");
}

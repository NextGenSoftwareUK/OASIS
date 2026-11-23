using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.ONODE.Service.Services;
using NextGenSoftware.Utilities;
using SupervisorConfig = NextGenSoftware.OASIS.ONODE.Service.Models.SupervisorConfig;
using Web4Startup = NextGenSoftware.OASIS.API.ONODE.WebAPI.Startup;

const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
    Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
    throw new InvalidOperationException("Only the owned cloud loopback fixture may be used.");
var stage = "preparation";
string invariant = "";
void Check(bool value, string message)
{
    if (!value) { invariant = message; throw new InvalidOperationException("Owned ONODE invariant failed."); }
}
var candidateRoot = Environment.GetEnvironmentVariable("OASIS_ONODE_CANDIDATE_ROOT")
    ?? throw new InvalidOperationException("Exact candidate source path required.");
var marker = "onode_bridge_" + Guid.NewGuid().ToString("N");
var databases = new[] { marker, marker + "_accounting", marker + "_outbox" };
var mongo = new MongoClient(uri);
var owned = Path.Combine(Path.GetTempPath(), marker);
Check(!Directory.Exists(owned), "Owned directory already exists.");
Directory.CreateDirectory(owned);
var dnaPath = Path.Combine(owned, "OASISDNA.json");
var priorEnvironment = new Dictionary<string, string?>();
void SetEnvironment(string key, string value)
{
    if (!priorEnvironment.ContainsKey(key)) priorEnvironment.Add(key, Environment.GetEnvironmentVariable(key));
    Environment.SetEnvironmentVariable(key, value);
}
IHost? api = null;
WebApplication? supervisorApp = null;
HolonBridge? bridge = null;
MongoDBOASIS? provider = null;
var tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "supervisor-token");
bool ownsToken = false, databaseNamesValidated = false;
using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
try
{
    stage = "owned database refusal";
    var existing = await (await mongo.ListDatabaseNamesAsync(cancellationToken: deadline.Token)).ToListAsync(deadline.Token);
    Check(databases.All(name => !existing.Contains(name)), "Owned database name already exists.");
    databaseNamesValidated = true;
    Check(!File.Exists(tokenPath), "An existing supervisor token must not be read or overwritten.");

    stage = "isolated DNA";
    var json = JObject.Parse(await File.ReadAllTextAsync(Path.Combine(candidateRoot,
        "OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json"), deadline.Token));
    var oasis = (JObject)json["OASIS"]!;
    var storage = (JObject)oasis["StorageProviders"]!;
    foreach (var property in storage.Properties().ToList())
    {
        if (property.Name.StartsWith("Auto", StringComparison.Ordinal) && property.Value.Type == JTokenType.Boolean) property.Value = false;
        if (property.Name.StartsWith("Auto", StringComparison.Ordinal) && property.Value.Type == JTokenType.String) property.Value = "MongoDBOASIS";
    }
    storage["OASISProviderBootType"] = "Cold";
    storage["MongoDBOASIS"] = JObject.FromObject(new { ConnectionString = uri, DBName = marker, NativeImportDBName = marker });
    oasis["HyperDriveMode"] = HyperDriveModes.V2;
    var hyperdrive = (JObject)oasis["OASISHyperDriveConfig"]!;
    foreach (var key in new[] { "OfflineSyncEnabled", "EnableHostedSync", "EnableSyncHistoryCompaction",
        "AutoFailoverEnabled", "AutoReplicationEnabled", "AutoLoadBalancingEnabled" }) hyperdrive[key] = false;
    foreach (var key in new[] { "EnabledProviders", "AutoFailoverProviders", "AutoReplicationProviders", "LoadBalancingProviders" })
        hyperdrive[key] = new JArray("MongoDBOASIS");
    hyperdrive["ProviderConfigs"] = new JObject();
    var onet = (JObject)oasis["ONET"]!;
    onet["EnableHyperDriveSyncHost"] = false; onet["EnableMDNS"] = false;
    onet["AutoRegisterOnBootstrap"] = false; onet["BootstrapServers"] = new JArray();
    oasis["Security"]!["SecretKey"] = Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
    oasis["Security"]!["RequireApiKey"] = false;
    oasis["OfflineSessionGrants"]!["Enabled"] = false;
    oasis["Sandbox"]!["Enabled"] = true;
    oasis["Logging"]!["LogToFile"] = false;
    oasis["Email"]!["DisableAllEmails"] = true;
    var dnaJson = json.ToString(Formatting.None);
    var dna = JsonConvert.DeserializeObject<OASISDNA>(dnaJson)!;
    await File.WriteAllTextAsync(dnaPath, dnaJson, deadline.Token);
    SetEnvironment("OASIS_DNA_JSON", dnaJson);
    SetEnvironment("OASIS_SANDBOX_OPENVINO", "false");
    SetEnvironment("OASIS_PAUSE_HYPERDRIVE_WORKERS", "true");
    SetEnvironment("OASIS_PAUSE_USAGE_RECONCILIATION", "true");
    SetEnvironment("OASIS_PAUSE_APPLICATION_MAINTENANCE", "true");
    SetEnvironment("SUBSCRIPTION_USAGE_ENABLED", "true");
    SetEnvironment("SUBSCRIPTION_MONGODB_CONNECTION_STRING", uri);
    SetEnvironment("SUBSCRIPTION_MONGODB_DATABASE", databases[1]);
    SetEnvironment("SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING", uri);
    SetEnvironment("SUBSCRIPTION_OUTBOX_DATABASE", databases[2]);
    SetEnvironment("SUBSCRIPTION_LEDGER_INITIAL_STATE", "empty-new-installation");
    SetEnvironment("ASPNETCORE_ENVIRONMENT", "Production");
    var policy = new { requiresProviderMeasurement = true, meterCategory = "onode.owned-fixture",
        reservedUnits = 1, fixedCostUsd = 0, estimatedCostUsd = 0, maximumNativeFeeCostUsd = 0,
        maximumProviderAttempts = 1, pricingCatalogueVersion = "onode-owned-v1" };
    SetEnvironment("PREPARED_STORAGE_ADMISSION_JSON", System.Text.Json.JsonSerializer.Serialize(new
    {
        nativeProviders = Array.Empty<string>(), ordinaryProviders = new[] { "MongoDBOASIS" },
        queryOnlyProviders = Array.Empty<string>(), authoritativeProvider = "MongoDBOASIS",
        storageTariffs = Array.Empty<object>(),
        queryTariffs = new[] { new { provider = "MongoDBOASIS", costUsd = 0, catalogueVersion = "onode-owned-v1" } },
        routes = new[] { new { method = "POST", path = "/api/data/query-cql", policy } }
    }));

    stage = "real native avatar seed";
    provider = new MongoDBOASIS(uri, marker, dna);
    var activated = await provider.ActivateProviderAsync();
    Check(!activated.IsError, "Owned Mongo activation failed.");
    var wizard = Guid.NewGuid(); var ordinary = Guid.NewGuid(); var system = Guid.NewGuid();
    var password = "Owned!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
    foreach (var actor in new[] { (id: wizard, kind: AvatarType.Wizard), (id: ordinary, kind: AvatarType.User), (id: system, kind: AvatarType.System) })
    {
        var username = actor.kind == AvatarType.System ? "root" : "owned-" + actor.id.ToString("N");
        var email = actor.kind == AvatarType.System ? "anorak@oasisomniverse.one" : username + "@example.test";
        var saved = await provider.SaveAvatarAsync(new Avatar { Id = actor.id, Username = username,
            Email = email, FirstName = actor.kind == AvatarType.System ? "OASIS" : "Owned",
            LastName = actor.kind == AvatarType.System ? "SYSTEM" : "Bridge", Password = BCrypt.Net.BCrypt.HashPassword(password),
            Verified = DateTime.UtcNow, AvatarType = new EnumValue<AvatarType>(actor.kind) });
        Check(!saved.IsError, "Native actor seed failed.");
        var detail = await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = actor.id, Username = username,
            Email = email, Version = 1, VersionId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow });
        Check(!detail.IsError, "Native detail seed failed.");
    }
    NextGenSoftware.OASIS.OASISBootLoader.OASISBootLoader.OASISDNAPath = dnaPath;
    ProviderManager.Instance.OASISDNA = dna;
    Check(ProviderManager.Instance.RegisterProvider(provider), "Owned provider registration failed.");
    var selected = await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(ProviderType.MongoDBOASIS);
    Check(!selected.IsError, "Owned provider selection failed.");
    var boot = await new OASISAPI().BootOASISAsync(dna, startApolloServer: false);
    Check(!boot.IsError && boot.Result, "Owned runtime boot failed.");

    stage = "actual supervisor composition";
    var nodeConfig = new SupervisorConfig { NodeId = marker, AvatarId = wizard.ToString("D"), OASISDNAPath = dnaPath,
        TokenPath = tokenPath, HolonPushIntervalSeconds = 1, CommandPollIntervalSeconds = 1, Services = new() };
    var options = Options.Create(nodeConfig);
    var logs = new LogAggregator(10);
    var processSupervisor = new ProcessSupervisor(options, logs, NullLogger<ProcessSupervisor>.Instance);
    var metrics = new MetricsCollector(processSupervisor, options, NullLogger<MetricsCollector>.Instance);
    var providers = new ProviderService(options, NullLogger<ProviderService>.Instance);
    var executor = new CommandExecutor(processSupervisor, logs, providers, metrics, options, NullLogger<CommandExecutor>.Instance);
    Check(!File.Exists(tokenPath), "Supervisor token appeared before owned initialization.");
    var token = new TokenService(); ownsToken = true; token.Initialise(tokenPath);
    var supervisorBuilder = WebApplication.CreateBuilder();
    supervisorBuilder.Logging.ClearProviders();
    supervisorBuilder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
    supervisorBuilder.Services.AddSingleton(options); supervisorBuilder.Services.AddSingleton(logs);
    supervisorBuilder.Services.AddSingleton(processSupervisor); supervisorBuilder.Services.AddSingleton(metrics);
    supervisorBuilder.Services.AddSingleton(providers); supervisorBuilder.Services.AddSingleton(token);
    supervisorBuilder.Services.AddSingleton<MetricsHistoryService>();
    supervisorBuilder.Services.AddControllers().AddApplicationPart(typeof(NextGenSoftware.OASIS.ONODE.Service.Api.SupervisorController).Assembly);
    supervisorApp = supervisorBuilder.Build(); supervisorApp.MapControllers();
    await supervisorApp.StartAsync(deadline.Token);
    var supervisorOrigin = supervisorApp.Urls.Single();

    stage = "actual WEB4 Startup";
    api = Host.CreateDefaultBuilder()
        .ConfigureLogging(logging => logging.ClearProviders())
        .ConfigureWebHostDefaults(web => web.UseSetting(WebHostDefaults.ApplicationKey, typeof(Web4Startup).Assembly.FullName)
            .UseStartup<Web4Startup>().ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0)))
        .ConfigureServices(services => services.PostConfigure<Microsoft.Extensions.Http.HttpClientFactoryOptions>("ONODESupervisor",
            settings => settings.HttpClientActions.Add(client => { client.BaseAddress = new Uri(supervisorOrigin); client.Timeout = TimeSpan.FromSeconds(5); })))
        .Build();
    await api.StartAsync(deadline.Token);
    var origin = api.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    Check(new Uri(origin).IsLoopback, "WEB4 server escaped loopback.");
    using var http = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(10) };
    async Task<string> SignIn(Guid id)
    {
        using var response = await http.PostAsJsonAsync("/api/avatar/authenticate",
            new { username = "owned-" + id.ToString("N"), password }, deadline.Token);
        Check(response.StatusCode == HttpStatusCode.OK, "Real password authentication HTTP failed: " + (int)response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
        var result = body.RootElement.GetProperty("result");
        Check(!result.GetProperty("isError").GetBoolean(), "Real password authentication failed.");
        var avatar = result.GetProperty("result");
        Check(avatar.GetProperty("id").GetGuid() == id, "Signed identity differs.");
        return avatar.GetProperty("jwtToken").GetString() ?? throw new InvalidOperationException("No issued token.");
    }
    stage = "issued sessions";
    var wizardJwt = await SignIn(wizard);
    var ordinaryJwt = await SignIn(ordinary);
    stage = "real free subscription admission";
    foreach (var issued in new[] { wizardJwt, ordinaryJwt })
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", issued);
        using var admitted = await http.PostAsJsonAsync("/api/subscription/checkout/session",
            new { planId = "free", successUrl = origin + "/subscription", cancelUrl = origin + "/" }, deadline.Token);
        Check(admitted.StatusCode == HttpStatusCode.OK, "Owned free subscription was not admitted.");
    }
    http.DefaultRequestHeaders.Authorization = null;
    stage = "actual ONODE authorization";
    using (var denied = await http.GetAsync("/api/v1/onode/active-nodes", deadline.Token))
        Check(denied.StatusCode == HttpStatusCode.Unauthorized, "Anonymous ONODE access was accepted.");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ordinaryJwt);
    using (var denied = await http.GetAsync("/api/v1/onode/active-nodes", deadline.Token))
        Check(denied.StatusCode == HttpStatusCode.Forbidden, "Ordinary ONODE access was accepted.");
    http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", wizardJwt);
    nodeConfig.Web4ApiBaseUrl = origin; nodeConfig.Web4AccessToken = wizardJwt;
    bridge = new HolonBridge(processSupervisor, metrics, providers, options, executor, NullLogger<HolonBridge>.Instance);
    await bridge.StartAsync(deadline.Token);
    async Task<JsonDocument> Read(string path)
    {
        using var response = await http.GetAsync(path, deadline.Token);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(deadline.Token));
    }
    stage = "real node state push";
    for (var attempt = 0; ; attempt++)
    {
        using var state = await http.GetAsync("/api/v1/onode/node-state/" + marker, deadline.Token);
        if (state.StatusCode == HttpStatusCode.OK)
        {
            using var body = JsonDocument.Parse(await state.Content.ReadAsStringAsync(deadline.Token));
            Check(body.RootElement.GetProperty("avatarId").GetString() == wizard.ToString("D"), "Pushed avatar identity differs.");
            Check(body.RootElement.GetProperty("capabilities").EnumerateArray().Any(item => item.GetString() == "provider-routing-v1"),
                "Live node capability was lost.");
            break;
        }
        Check(state.StatusCode == HttpStatusCode.NotFound && attempt < 8, "Real node state was not pushed.");
        await Task.Delay(250, deadline.Token);
    }
    stage = "immediate real command polling";
    var clock = Stopwatch.StartNew();
    using var commandResponse = await http.PostAsJsonAsync("/api/v1/onode/commands",
        new { targetNodeId = marker, command = "GetConfig" }, deadline.Token);
    commandResponse.EnsureSuccessStatusCode();
    using var commandAck = JsonDocument.Parse(await commandResponse.Content.ReadAsStringAsync(deadline.Token));
    var commandId = commandAck.RootElement.GetProperty("commandId").GetString();
    for (var attempt = 0; ; attempt++)
    {
        using var result = await Read("/api/v1/onode/commands/" + commandId);
        var status = result.RootElement.GetProperty("status").GetString();
        if (status == "Done")
        {
            using var view = JsonDocument.Parse(result.RootElement.GetProperty("result").GetString()!);
            Check(view.RootElement.GetProperty("schemaVersion").GetString() == "onode-public-config-v1", "Bridge projection differs.");
            Check(view.RootElement.GetProperty("nodeId").GetString() == marker, "Bridge selected node differs.");
            Check(clock.Elapsed < TimeSpan.FromSeconds(8), "First command retained an unnecessary warmup delay.");
            break;
        }
        Check(status is "Pending" or "Executing" && attempt < 30, "Real command did not complete.");
        await Task.Delay(250, deadline.Token);
    }
    stage = "real WEB4 local provider proxy";
    using (var routing = await Read("/api/v1/onode/providers?role=failover"))
    {
        var rows = routing.RootElement;
        Check(rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() > 0, "WEB4 proxy lost routing rows.");
        Check(rows.EnumerateArray().All(row => row.GetProperty("role").GetString() == "failover"), "WEB4 proxy lost selected role.");
    }
    stage = "real WEB4 revision-aware local write";
    using (var routing = await Read("/api/v1/onode/providers?role=replication"))
    {
        var revision = routing.RootElement[0].GetProperty("revision").GetString();
        using var saved = await http.PutAsync("/api/v1/onode/providers/MongoDBOASIS/disable?role=replication&expectedRevision=" +
            Uri.EscapeDataString(revision!), null, deadline.Token);
        Check(saved.StatusCode == HttpStatusCode.OK, "WEB4 proxy did not forward saved-role revision.");
        using var ack = JsonDocument.Parse(await saved.Content.ReadAsStringAsync(deadline.Token));
        Check(ack.RootElement.GetProperty("reloadRequired").GetBoolean(), "WEB4 edit lost restart-required acknowledgement.");
        using var readback = await Read("/api/v1/onode/providers?role=replication");
        Check(!readback.RootElement.EnumerateArray().Single(row => row.GetProperty("providerType").GetString() == "MongoDBOASIS")
            .GetProperty("isEnabled").GetBoolean(), "WEB4 saved membership did not persist.");
    }
    stage = "actual Startup WebSocket";
    using (var socket = new ClientWebSocket())
    {
        socket.Options.AddSubProtocol("oasis-onode-v1");
        socket.Options.AddSubProtocol("oasis-jwt." + wizardJwt);
        var websocketUri = new UriBuilder(origin) { Scheme = "ws", Path = "/ws/onode/" + marker }.Uri;
        await socket.ConnectAsync(websocketUri, deadline.Token);
        Check(socket.SubProtocol == "oasis-onode-v1", "Actual Startup selected the wrong live protocol.");
        using var frame = new MemoryStream();
        var buffer = new byte[4096];
        WebSocketReceiveResult message;
        do
        {
            message = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), deadline.Token);
            Check(message.MessageType == WebSocketMessageType.Text, "Live node state was not JSON text.");
            frame.Write(buffer, 0, message.Count);
            Check(frame.Length <= 1024 * 1024, "Live node state exceeded owned fixture bound.");
        } while (!message.EndOfMessage);
        using var state = JsonDocument.Parse(frame.ToArray());
        Check(state.RootElement.GetProperty("nodeId").GetString() == marker, "Live state selected the wrong node.");
        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Owned check complete", deadline.Token);
    }
    Console.WriteLine("ONODE_BRIDGE_NATIVE: real owned Mongo password-issued sessions and actual WEB4 Startup enforced anonymous/ordinary denial; actual supervisor pushed node state and polled/executed/acknowledged public-config command within configured cadence; real authenticated revision-aware local provider proxy and actual Startup WebSocket passed. No browser, applied-runtime or all-provider acceptance.");
}
catch (Exception error)
{
    Console.Error.WriteLine("ONODE_BRIDGE_FAILURE: stage=" + stage + "; invariant=" + invariant +
        "; type=" + error.GetType().Name + "; base=" + error.GetBaseException().GetType().Name);
    Environment.ExitCode = 1;
}
finally
{
    if (bridge != null) { await bridge.StopAsync(CancellationToken.None); bridge.Dispose(); }
    if (api != null) { await api.StopAsync(CancellationToken.None); api.Dispose(); }
    if (supervisorApp != null) { await supervisorApp.StopAsync(CancellationToken.None); await supervisorApp.DisposeAsync(); }
    if (provider != null) await provider.DeactivateProviderAsync();
    if (databaseNamesValidated)
    {
        foreach (var database in databases) await mongo.DropDatabaseAsync(database);
        var remaining = await (await mongo.ListDatabaseNamesAsync()).ToListAsync();
        if (databases.Any(remaining.Contains)) throw new InvalidOperationException("Owned database remains after cleanup.");
    }
    if (ownsToken && File.Exists(tokenPath)) File.Delete(tokenPath);
    if (File.Exists(dnaPath)) File.Delete(dnaPath);
    var resolvedOwned = Path.GetFullPath(owned);
    var temporaryRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
    if (!resolvedOwned.StartsWith(temporaryRoot, StringComparison.Ordinal) || Path.GetFileName(resolvedOwned) != marker)
        throw new InvalidOperationException("Owned cleanup escaped the temporary directory.");
    Directory.Delete(resolvedOwned, recursive: true);
    foreach (var pair in priorEnvironment) Environment.SetEnvironmentVariable(pair.Key, pair.Value);
    Console.WriteLine(databaseNamesValidated
        ? "ONODE_BRIDGE_CLEANUP: owned API/supervisor/bridge stopped; databases, DNA, generated token and owned directory removed."
        : "ONODE_BRIDGE_CLEANUP: no database ownership claim; existing data preserved, owned resources stopped/removed.");
}

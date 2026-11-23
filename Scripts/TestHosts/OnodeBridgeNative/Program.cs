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
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.Utilities;
using Web4Startup = NextGenSoftware.OASIS.API.ONODE.WebAPI.Startup;

const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
if (Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true" ||
    Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
    throw new InvalidOperationException("Only the owned cloud loopback fixture may be used.");
var browserEnabled = Environment.GetEnvironmentVariable("OASIS_ONODE_BROWSER") == "true";
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
Process? supervisorProcess = null;
Task<string>? supervisorOutput = null, supervisorError = null;
MongoDBOASIS? provider = null;
var tokenPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".oasis", "supervisor-token");
bool ownsToken = false, ownsMetrics = false, databaseNamesValidated = false;
var metricsPath = Path.Combine(Path.GetDirectoryName(tokenPath)!, "onode-metrics.db");
var metricsFiles = new[] { metricsPath, metricsPath + "-wal", metricsPath + "-shm" };
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
    storage["OASISProviderBootType"] = "Warm";
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
    // The private SDK instance seeds only owned data. Actual Startup owns passive boot and admitted registration.

    // Separate shipping processes retain their independent OpenAPI dependency graphs.
    var supervisorOrigin = "http://127.0.0.1:8765";
    Check(metricsFiles.All(path => !File.Exists(path)), "Existing supervisor metrics must be preserved.");

    stage = "actual WEB4 Startup";
    api = Host.CreateDefaultBuilder()
        .ConfigureLogging(logging => { logging.ClearProviders(); logging.AddProvider(new OwnedServerExceptionLogger()); })
        .ConfigureWebHostDefaults(web => web.UseSetting(WebHostDefaults.ApplicationKey, typeof(Web4Startup).Assembly.FullName)
            .UseStartup<Web4Startup>().ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0)))
        .ConfigureServices(services => {
            services.PostConfigure<Microsoft.Extensions.Http.HttpClientFactoryOptions>("ONODESupervisor",
                settings => settings.HttpClientActions.Add(client => { client.BaseAddress = new Uri(supervisorOrigin); client.Timeout = TimeSpan.FromSeconds(5); }));
            if (browserEnabled) services.AddTransient<IStartupFilter, OwnedOnodeBrowserAssets>();
        })
        .Build();
    await api.StartAsync(deadline.Token);
    var origin = api.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    Check(new Uri(origin).IsLoopback, "WEB4 server escaped loopback.");
    using var http = new HttpClient(new HttpClientHandler { UseCookies = false }) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromSeconds(10) };
    async Task<string> SignIn(Guid id)
    {
        using var response = await http.PostAsJsonAsync("/api/avatar/authenticate",
            new { username = "owned-" + id.ToString("N"), password }, deadline.Token);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var diagnosticBody = await response.Content.ReadAsStringAsync(deadline.Token);
            // Error responses may be plain text from hosting, before the API JSON middleware.
            var diagnostic = diagnosticBody.Split('\n', 2)[0].Trim();
            if (response.Content.Headers.ContentType?.MediaType == "application/json")
            {
                using var failure = JsonDocument.Parse(diagnosticBody);
                diagnostic = failure.RootElement.TryGetProperty("message", out var message)
                    ? message.GetString() ?? "No error message" : "No top-level error message";
            }
            if (diagnostic.Length == 0) diagnostic = "Empty error body";
            diagnostic = diagnostic.Replace(password, "[password]").Replace(uri, "[owned-connection]");
            diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"(?:https?|mongodb(?:\+srv)?)://\S+", "[address]");
            diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"[A-Za-z0-9+/=_-]{32,}", "[opaque-value]");
            diagnostic = System.Text.RegularExpressions.Regex.Replace(diagnostic, @"(?i)(password|secret|token|api[-_ ]?key)\s*[:=]\s*[^ ;]+", "$1=[redacted]");
            Console.WriteLine("ONODE_AUTH_FAILURE: http=" + (int)response.StatusCode + "; message=" + diagnostic[..Math.Min(diagnostic.Length, 600)]);
        }
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
    stage = "actual supervisor process";
    var serviceDll = Path.Combine(candidateRoot, "ONODE/ONODEManager/NextGenSoftware.OASIS.ONODE.Service/bin/Release/net10.0/NextGenSoftware.OASIS.ONODE.Service.dll");
    Check(File.Exists(serviceDll), "Separately built shipping supervisor missing.");
    var serviceStart = new ProcessStartInfo("dotnet") { WorkingDirectory = owned,
        RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
    serviceStart.ArgumentList.Add(serviceDll);
    serviceStart.Environment["Supervisor__Port"] = "8765";
    serviceStart.Environment["Supervisor__NodeId"] = marker;
    serviceStart.Environment["Supervisor__AvatarId"] = wizard.ToString("D");
    serviceStart.Environment["Supervisor__OASISDNAPath"] = dnaPath;
    serviceStart.Environment["Supervisor__TokenPath"] = tokenPath;
    serviceStart.Environment["Supervisor__Web4ApiBaseUrl"] = origin;
    serviceStart.Environment["Supervisor__Web4AccessToken"] = wizardJwt;
    if (!browserEnabled)
    {
        serviceStart.Environment["Supervisor__HolonPushIntervalSeconds"] = "1";
        serviceStart.Environment["Supervisor__CommandPollIntervalSeconds"] = "1";
    }
    foreach (var service in new[] { "web4", "web5", "web6", "web7", "web8", "web9", "web10" })
        serviceStart.Environment["Supervisor__Services__" + service + "__Enabled"] = "false";
    Check(!File.Exists(tokenPath) && metricsFiles.All(path => !File.Exists(path)), "Owned supervisor files appeared before start.");
    ownsToken = true; ownsMetrics = true;
    supervisorProcess = Process.Start(serviceStart) ?? throw new InvalidOperationException("Actual supervisor did not start.");
    supervisorOutput = supervisorProcess.StandardOutput.ReadToEndAsync();
    supervisorError = supervisorProcess.StandardError.ReadToEndAsync();
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
        Check(!supervisorProcess.HasExited && state.StatusCode == HttpStatusCode.NotFound && attempt < 40, "Real node state was not pushed.");
        await Task.Delay(250, deadline.Token);
    }
    if (!browserEnabled)
    {
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
    stage = "loaded WEB4 public configuration";
    using (var loaded = await Read("/api/v1/onode/public-config"))
    {
        Check(!loaded.RootElement.GetProperty("isError").GetBoolean(), "Loaded public view returned an error.");
        var view = loaded.RootElement.GetProperty("result");
        Check(view.GetProperty("schemaVersion").GetString() == "onode-public-config-v1", "Loaded public schema differs.");
        Check(view.GetProperty("oasisDna").GetProperty("routingPolicy").GetProperty("source").GetString() ==
            "OASIS.OASISHyperDriveConfig", "Loaded public view selected the wrong routing owner.");
        Check(!view.GetRawText().Contains(wizardJwt, StringComparison.Ordinal), "Loaded public view leaked a runtime token.");
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
    }
    if (browserEnabled)
    {
        stage = "actual ONODE browser";
        var script = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_SCRIPT")
            ?? throw new InvalidOperationException("Pinned browser script required.");
        Check(File.Exists(script), "Owned browser script missing.");
        var start = new ProcessStartInfo("node") { RedirectStandardInput = true, RedirectStandardOutput = true,
            RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add("-e");
        start.ArgumentList.Add("const fs=require('node:fs');require(process.argv[1]).verify(JSON.parse(fs.readFileSync(0,'utf8'))).then(x=>console.log(JSON.stringify(x))).catch(e=>{console.error(e.message);process.exitCode=1;});");
        start.ArgumentList.Add(script);
        using var browser = Process.Start(start) ?? throw new InvalidOperationException("Owned browser did not start.");
        var output = browser.StandardOutput.ReadToEndAsync();
        var error = browser.StandardError.ReadToEndAsync();
        try
        {
            await browser.StandardInput.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new {
                baseUrl = origin, token = wizardJwt, avatarId = wizard.ToString("D"), nodeId = marker }));
            browser.StandardInput.Close();
            await browser.WaitForExitAsync(deadline.Token);
            if (browser.ExitCode != 0)
            {
                var safeStage = (await error).Split('\n').Select(line => line.Trim()).FirstOrDefault(line =>
                    System.Text.RegularExpressions.Regex.IsMatch(line,
                        @"^Owned ONODE browser failed at [A-Za-z -]+; type=[A-Za-z]+; credential-bearing detail withheld\.$"));
                if (safeStage != null) Console.WriteLine("ONODE_BROWSER_FAILURE: " + safeStage);
            }
            Check(browser.ExitCode == 0, "Actual popup journey failed; credential-bearing browser details withheld.");
            using var result = JsonDocument.Parse(await output);
            Check(result.RootElement.GetProperty("actualOnodePopup").GetBoolean() &&
                result.RootElement.GetProperty("realLiveFrame").GetBoolean(), "Actual popup/live result missing.");
            Console.WriteLine("ONODE_BROWSER_REQUESTS: commandPosts=" + result.RootElement.GetProperty("commandPosts").GetInt32() +
                "; statusReads=" + result.RootElement.GetProperty("statusReads").GetInt32() + "; rateLimited=" + result.RootElement.GetProperty("rateLimited").GetBoolean());
            Console.WriteLine("ONODE_BROWSER_NATIVE: actual pinned popup and SDK; registered public views, saved role readback, actual live frame, loaded API view and close passed; no API mocks.");
        }
        finally
        {
            if (!browser.HasExited) { browser.Kill(entireProcessTree: true); await browser.WaitForExitAsync(); }
            await output; await error;
        }
    }
    if (!browserEnabled) Console.WriteLine("ONODE_BRIDGE_NATIVE: real owned Mongo password-issued sessions and actual WEB4 Startup enforced anonymous/ordinary denial; actual supervisor pushed node state and polled/executed/acknowledged public-config command within configured cadence; real authenticated revision-aware local provider proxy and actual Startup WebSocket passed. No browser, applied-runtime or all-provider acceptance.");
}
catch (Exception error)
{
    Console.Error.WriteLine("ONODE_BRIDGE_FAILURE: stage=" + stage + "; invariant=" + invariant +
        "; type=" + error.GetType().Name + "; base=" + error.GetBaseException().GetType().Name);
    Environment.ExitCode = 1;
}
finally
{
    if (supervisorProcess != null)
    {
        if (!supervisorProcess.HasExited) { supervisorProcess.Kill(entireProcessTree: true); await supervisorProcess.WaitForExitAsync(); }
        if (supervisorOutput != null) await supervisorOutput;
        if (supervisorError != null) await supervisorError;
        supervisorProcess.Dispose();
    }
    if (api != null) { await api.StopAsync(CancellationToken.None); api.Dispose(); }
    if (ownsMetrics) foreach (var path in metricsFiles) { if (File.Exists(path)) File.Delete(path); Check(!File.Exists(path), "Owned metrics remain."); }
    if (provider != null) await provider.DeActivateProviderAsync();
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

// Adds only owned static assets ahead of the actual shipping Startup pipeline.
sealed class OwnedOnodeBrowserAssets : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        var portal = Environment.GetEnvironmentVariable("OASIS_GEO_PORTAL_ROOT")
            ?? throw new InvalidOperationException("Pinned Portal root required.");
        var host = Environment.GetEnvironmentVariable("OASIS_GEO_BROWSER_HOST")
            ?? throw new InvalidOperationException("Owned browser host required.");
        if (!File.Exists(host) || !File.Exists(Path.Combine(portal, "assets/js/onode-modal.js")))
            throw new InvalidOperationException("Actual ONODE assets unavailable.");
        app.UseStaticFiles(new StaticFileOptions {
            FileProvider = new PhysicalFileProvider(Path.Combine(portal, "assets")), RequestPath = "/assets" });
        app.Use(async (context, following) => {
            if (context.Request.Path == "/__onode_browser") {
                context.Response.ContentType = "text/html";
                await context.Response.SendFileAsync(host, context.RequestAborted);
            } else await following();
        });
        next(app);
    };
}

// Emit exception identities and code locations only; never format log state or exception messages.
sealed class OwnedServerExceptionLogger : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ExceptionLogger();
    public void Dispose() { }
    sealed class ExceptionLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Error;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (level < LogLevel.Error || exception == null) return;
            var root = exception.GetBaseException();
            var methods = new StackTrace(root, false).GetFrames()?.Take(8).Select(frame => {
                var method = frame.GetMethod();
                return method?.DeclaringType?.FullName + "." + method?.Name;
            }) ?? Array.Empty<string>();
            Console.WriteLine("ONODE_SERVER_EXCEPTION: type=" + exception.GetType().FullName +
                "; base=" + root.GetType().FullName + "; methods=" + string.Join(" | ", methods));
        }
    }
}

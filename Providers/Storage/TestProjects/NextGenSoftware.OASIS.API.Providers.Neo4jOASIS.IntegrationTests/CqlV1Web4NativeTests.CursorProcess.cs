extern alias ShippingWeb4;

using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using ShippingCursors = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlFederationCursorHost;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed record CursorProcessIntent(Guid Actor, Guid[] Ids, Guid EncryptedId, string Tag,
        string Sqlite, string JwtSecret, string AuthorityUrl, string AuthorityKey, string Deployment);

    private static async Task RunCursorProcessHost(string phase)
    {
        Assert.IsTrue(phase is "first" or "second");
        Assert.IsTrue(OperatingSystem.IsWindows(), "This acceptance verifies Windows DPAPI-protected keys.");
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var root = Path.Combine(work, "cql-cursor-process");
        var intent = JsonSerializer.Deserialize<CursorProcessIntent>(await File.ReadAllTextAsync(Path.Combine(root, "intent.json")))!;
        Assert.AreEqual(Path.Combine(work, "cql-v1-web4.sqlite"), intent.Sqlite);
        Assert.IsTrue(new Uri(intent.AuthorityUrl).IsLoopback);
        const string mongo = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongo, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var providers = new Dictionary<ProviderType, IOASISStorageProvider>
        {
            [ProviderType.MongoDBOASIS] = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(mongo, "cosmic_cql_v1_web4_storage"),
            [ProviderType.SQLLiteDBOASIS] = new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS("Data Source=" + intent.Sqlite),
            [ProviderType.Neo4jOASIS] = new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only")
        };
        foreach (var provider in providers.Values)
        { var activated = await provider.ActivateProviderAsync(); Assert.IsFalse(activated.IsError, activated.Message); }
        var keyDirectory = Path.Combine(root, "keys");
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("OASIS-owned-process-cursor")
            .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory)).ProtectKeysWithDpapi();
        using var keys = services.BuildServiceProvider();
        var protection = keys.GetRequiredService<IDataProtectionProvider>();
        // Key creation/decryption is startup readiness, before accepting a metered query.
        protection.CreateProtector("OASIS.owned.cursor.readiness").Protect(new byte[] { 1 });
        var outbox = new MongoUsageOutboxStore(mongo, "cosmic_cql_v1_web4_usage");
        await outbox.InitializeAsync(default);
        using var authorityHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var authority = new Web4SubscriptionAuthorizationClient(authorityHttp, intent.AuthorityUrl, "WEB4", intent.AuthorityKey);
        await using var app = ShippingReadHost(new ShippingFederationSources(providers),
            new ShippingFederationPolicy(intent.Actor, intent.Ids, intent.EncryptedId), providers.Keys.ToArray(),
            outbox, authority, intent.JwtSecret, new ShippingCursors(protection, intent.Deployment));
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var readyPath = Path.Combine(root, phase + "-ready.json");
        // Publish readiness only after the complete file is closed. The parent
        // must never observe File.Exists while JSON bytes are still being written.
        await File.WriteAllTextAsync(readyPath + ".pending",
            JsonSerializer.Serialize(new { pid = Environment.ProcessId, address }));
        File.Move(readyPath + ".pending", readyPath);
        using var lifetime = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        while (!File.Exists(Path.Combine(root, "stop"))) await Task.Delay(50, lifetime.Token);
        await app.StopAsync();
        foreach (var provider in providers.Values)
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        await File.WriteAllTextAsync(Path.Combine(root, "stopped.json"),
            JsonSerializer.Serialize(new { pid = Environment.ProcessId, stopped = true }));
    }

    private static async Task<int> VerifyCursorProcessRestart(MongoUsageOutboxStore outbox, Authority authority,
        string secret, Guid actor, Guid[] ids, Guid encryptedId, string tag, string sqlite,
        Func<string, object?, Task> journal)
    {
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var root = Path.Combine(work, "cql-cursor-process");
        Assert.IsFalse(Directory.Exists(root), "Preserve unfinished cursor process/key evidence.");
        await journal("before-cursor-process", new { root, actor });
        Directory.CreateDirectory(root);
        var bridgeKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using var bridge = builder.Build();
        bridge.Use(async (http, next) =>
        {
            if (http.Request.Headers["X-OASIS-Service"] != "WEB4" || http.Request.Headers["X-OASIS-Service-Key"] != bridgeKey)
            { http.Response.StatusCode = 401; return; }
            await next();
        });
        bridge.MapPost("/api/subscription/usage/authorize", async (HttpContext http, UsageAuthorizationRequest body) =>
        {
            Assert.IsTrue(AuthenticationHeaderValue.TryParse(http.Request.Headers.Authorization, out var bearer));
            return Results.Json(await authority.AuthorizeUsageAsync(bearer!.Parameter!, body));
        });
        bridge.MapPost("/api/subscription/usage/start", async (UsageStartRequest body) => Results.Json(await authority.StartUsageAsync(body)));
        bridge.MapPost(UsageSettlementProtocol.OrdinaryPath, async (UsageSettlementRequest body) =>
        {
            Assert.AreEqual(UsageSettlementProtocol.OrdinaryPath, UsageSettlementProtocol.PathFor(body));
            return Results.Json(await authority.SettleUsageAsync(body));
        });
        await bridge.StartAsync();
        var authorityUrl = bridge.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        var deployment = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("owned-process-cursor:" + tag))).ToLowerInvariant();
        await File.WriteAllTextAsync(Path.Combine(root, "intent.json"), JsonSerializer.Serialize(new CursorProcessIntent(
            actor, ids, encryptedId, tag, sqlite, secret, authorityUrl, bridgeKey, deployment)));
        var bearerToken = authority.Tokens.Single(value => value.Value == actor).Key;
        var children = new List<(Process Runner, Task<string> Out, Task<string> Error, string Phase)>();
        var hostPids = new List<int>();
        var operationIds = new List<string>();
        var copiedRows = new List<Guid>();
        var complete = false;

        async Task<Uri> Start(string phase)
        {
            var start = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet", "dotnet.exe"))
            { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add("vstest"); start.ArgumentList.Add(typeof(CqlV1Web4NativeTests).Assembly.Location);
            start.ArgumentList.Add("/ResultsDirectory:" + work);
            start.ArgumentList.Add("/Logger:trx;LogFileName=cosmic-cursor-process-" + phase + ".trx");
            start.ArgumentList.Add("/TestCaseFilter:FullyQualifiedName=" + typeof(CqlV1Web4NativeTests).FullName
                + ".SignedHttpQueriesUseNativeVisibilityPagingAndDurableReceiptsOnAllThreeProviders");
            start.Environment.Clear();
            foreach (var name in new[] { "SystemRoot", "WINDIR", "PATH", "TEMP", "TMP", "USERPROFILE", "APPDATA", "LOCALAPPDATA",
                "ProgramFiles", "ProgramFiles(x86)", "ProgramData", "COMSPEC", "SYSTEMDRIVE", "HOMEDRIVE", "HOMEPATH",
                "DOTNET_ROOT", "DOTNET_CLI_HOME", "OASIS_OWNED_NEO4J_WORK", "SUBSCRIPTION_TEST_MONGODB_URI", "OASIS_OWNED_NEO4J_URI" })
                if (Environment.GetEnvironmentVariable(name) is { } value) start.Environment[name] = value;
            start.Environment["OASIS_COSMIC_CURSOR_PROCESS"] = phase;
            var process = Process.Start(start)!;
            children.Add((process, process.StandardOutput.ReadToEndAsync(), process.StandardError.ReadToEndAsync(), phase));
            var ready = Path.Combine(root, phase + "-ready.json");
            var deadline = Stopwatch.StartNew();
            while (!File.Exists(ready))
            {
                Assert.IsFalse(process.HasExited, "Cursor host exited before readiness; preserve its output.");
                Assert.IsTrue(deadline.Elapsed < TimeSpan.FromSeconds(60), "Owned cursor host readiness timed out.");
                await Task.Delay(50);
            }
            using var json = JsonDocument.Parse(await File.ReadAllTextAsync(ready));
            var pid = json.RootElement.GetProperty("pid").GetInt32();
            Assert.AreNotEqual(Environment.ProcessId, pid);
            Assert.IsFalse(hostPids.Contains(pid)); hostPids.Add(pid);
            var address = new Uri(json.RootElement.GetProperty("address").GetString()!);
            Assert.IsTrue(address.IsLoopback);
            return address;
        }
        object Intent(string? cursor = null) => new
        {
            EnableContinuation = true, Cursor = cursor, Scope = "Named",
            Providers = new[] { "MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS" },
            Source = new { Filters = new[] { new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag } }, OrderBy = "Name", Take = 100 },
            Take = 2, MaximumInputRows = 900, MaximumRowsPerProvider = 300, MaximumProviderAttempts = 9
        };
        async Task<JsonElement> Read(Uri address, string? token, string key, bool duplicate = false)
        {
            using var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql/federation")
            { Content = new StringContent(JsonSerializer.Serialize(Intent(token)), Encoding.UTF8, "application/json") };
            request.Headers.Add("Idempotency-Key", key);
            using var response = await client.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(duplicate ? HttpStatusCode.Conflict : HttpStatusCode.OK, response.StatusCode, raw);
            using var json = JsonDocument.Parse(raw);
            if (duplicate) { Assert.AreEqual("OPERATION_ALREADY_EXECUTED", json.RootElement.GetProperty("code").GetString()); return default; }
            var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
            var operation = await outbox.GetAsync(operationId, default);
            Assert.AreEqual("settled", operation.State);
            Assert.AreEqual(3, operation.ProviderAttempts.Count); Assert.AreEqual(3, operation.Receipts.Count);
            Assert.AreEqual(0.006m, authority.Settlements[operationId].ActualCostUsd);
            operationIds.Add(operationId);
            copiedRows.AddRange(json.RootElement.GetProperty("result").GetProperty("rows").EnumerateArray()
                .Select(row => row.GetProperty("selected").GetProperty("row").GetProperty("id").GetGuid()));
            return json.RootElement.Clone();
        }
        static (string Token, string Key) Next(JsonElement page)
        {
            var next = page.GetProperty("result").GetProperty("continuation");
            return (next.GetProperty("cursor").GetString()!, next.GetProperty("nextRequestKey").GetString()!);
        }
        try
        {
            var firstAddress = await Start("first");
            var next = Next(await Read(firstAddress, null, Guid.NewGuid().ToString("D")));
            var keys = Directory.GetFiles(Path.Combine(root, "keys"), "*.xml");
            Assert.AreEqual(1, keys.Length);
            Assert.IsTrue(XDocument.Load(keys[0]).Descendants().Any(element => element.Name.LocalName == "encryptedSecret"),
                "Persisted test key must be protected at rest.");
            var keyHash = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(keys[0])));
            children[0].Runner.Kill(entireProcessTree: true);
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                await children[0].Runner.WaitForExitAsync(timeout.Token);
            Assert.AreNotEqual(0, children[0].Runner.ExitCode);
            var secondAddress = await Start("second");
            var second = await Read(secondAddress, next.Token, next.Key);
            var started = authority.Started.Count;
            await Read(secondAddress, next.Token, next.Key, duplicate: true);
            Assert.AreEqual(started, authority.Started.Count);
            var last = Next(second);
            var third = await Read(secondAddress, last.Token, last.Key);
            Assert.IsFalse(third.GetProperty("result").GetProperty("hasMore").GetBoolean());
            CollectionAssert.AreEqual(new[] { ids[0], ids[2], ids[3], ids[3], ids[3], ids[4] }, copiedRows.ToArray());
            Assert.AreEqual(keyHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(keys[0]))));
            Assert.AreEqual(1, Directory.GetFiles(Path.Combine(root, "keys"), "*.xml").Length);
            await File.WriteAllTextAsync(Path.Combine(root, "stop"), "verified");
            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                await children[1].Runner.WaitForExitAsync(timeout.Token);
            Assert.AreEqual(0, children[1].Runner.ExitCode);
            using var stopped = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, "stopped.json")));
            Assert.AreEqual(hostPids[1], stopped.RootElement.GetProperty("pid").GetInt32());
            var evidence = new { hostPids, parentPid = Environment.ProcessId, operationIds, pages = 3,
                attempts = 9, duplicateRejections = 1, abruptTermination = true, persistedProtectedKey = true, keyHash,
                unchangedKey = true, providers = 3, keyBytes = new FileInfo(keys[0]).Length,
                temporaryBytes = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length) };
            await File.WriteAllTextAsync(Path.Combine(work, "cursor-process-evidence.json"), JsonSerializer.Serialize(evidence));
            await journal("cursor-process-restart-verified", evidence);
            complete = true;
            return 3;
        }
        finally
        {
            foreach (var child in children)
            {
                if (!child.Runner.HasExited) { child.Runner.Kill(entireProcessTree: true); await child.Runner.WaitForExitAsync(); }
                await File.WriteAllTextAsync(Path.Combine(work, "cosmic-cursor-process-" + child.Phase + ".log"), await child.Out + await child.Error);
                child.Runner.Dispose();
            }
            await bridge.StopAsync();
            if (complete)
            {
                // Only files in the exact owned fixed root, after both process trees stopped.
                Assert.AreEqual(Path.GetFullPath(work), Directory.GetParent(root)!.FullName);
                Assert.IsFalse((new DirectoryInfo(root).Attributes & FileAttributes.ReparsePoint) != 0);
                var entries = new DirectoryInfo(root).EnumerateFileSystemInfos("*", SearchOption.AllDirectories).ToArray();
                Assert.IsFalse(entries.Any(entry => (entry.Attributes & FileAttributes.ReparsePoint) != 0));
                foreach (var file in entries.OfType<FileInfo>())
                {
                    var relative = Path.GetRelativePath(root, file.FullName);
                    Assert.IsTrue(new[] { "intent.json", "first-ready.json", "second-ready.json", "stop", "stopped.json" }.Contains(relative)
                        || relative.StartsWith("keys" + Path.DirectorySeparatorChar + "key-", StringComparison.Ordinal)
                        && relative.EndsWith(".xml", StringComparison.Ordinal));
                    using (File.Open(file.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
                    File.Delete(file.FullName);
                }
                foreach (var directory in entries.OfType<DirectoryInfo>().OrderByDescending(value => value.FullName.Length)) directory.Delete();
                Directory.Delete(root);
            }
        }
    }
}

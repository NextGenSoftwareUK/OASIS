extern alias ShippingWeb4;

using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using ShippingCursors = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.CqlFederationCursorHost;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    // Real Data Protection key creation/rotation/revocation, with a bounded memory-only
    // repository. No production keys, filesystem key ring or new environment is used.
    private sealed class CursorKeys : IXmlRepository
    {
        private readonly List<XElement> elements = new();
        public IReadOnlyCollection<XElement> GetAllElements()
        { lock (elements) return elements.Select(value => new XElement(value)).ToArray(); }
        public void StoreElement(XElement element, string friendlyName)
        { lock (elements) { Assert.IsTrue(elements.Count < 16); elements.Add(new XElement(element)); } }
    }
    private sealed class CursorClock : TimeProvider
    {
        internal DateTimeOffset Now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static ServiceProvider CursorKeyOwner(CursorKeys repository)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().SetApplicationName("OASIS-owned-cursor-acceptance")
            .AddKeyManagementOptions(options => { options.XmlRepository = repository; options.AutoGenerateKeys = false; });
        return services.BuildServiceProvider();
    }

    private static async Task<(int Requests, int Settled, int Attempts, int Duplicates)> VerifyShippingFederationCursors(
        Dictionary<ProviderType, IOASISStorageProvider> providers, MongoUsageOutboxStore outbox,
        Authority authority, string secret, Guid actor, Guid[] ids, Guid encryptedId, string tag,
        Func<string, object?, Task> journal)
    {
        var sources = new ShippingFederationSources(providers);
        var policy = new ShippingFederationPolicy(actor, ids, encryptedId);
        var selected = providers.Keys.ToArray();
        var clock = new CursorClock();
        var repository = new CursorKeys();
        using var initialKeys = CursorKeyOwner(repository);
        var oldKey = initialKeys.GetRequiredService<IKeyManager>().CreateNewKey(
            DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(2));
        using var rotatedKeys = CursorKeyOwner(repository);
        using var revokedKeys = CursorKeyOwner(repository);
        var deployment = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("owned-cql-cursor:" + tag))).ToLowerInvariant();
        WebApplication? app = null;
        HttpClient? client = null;
        var requests = 0; var settled = 0; var attempts = 0; var duplicates = 0;
        using var journalGate = new SemaphoreSlim(1, 1);

        async Task Start(IDataProtectionProvider keys)
        {
            client?.Dispose();
            if (app != null) { await app.StopAsync(); await app.DisposeAsync(); }
            app = ShippingReadHost(sources, policy, selected, outbox, authority, secret,
                new ShippingCursors(keys, deployment, clock));
            await app.StartAsync();
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authority.Tokens.Single(value => value.Value == actor).Key);
        }
        object Intent(string? cursor = null, int take = 2, int maximumAttempts = 12, string scope = "Named") => new
        {
            EnableContinuation = true, Cursor = cursor, Scope = scope,
            Providers = scope == "Named" ? selected.Select(value => value.ToString()).ToArray() : Array.Empty<string>(),
            Source = new { Filters = new[] { new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag } }, OrderBy = "Name", Take = 100 },
            Take = take, MaximumInputRows = 900, MaximumRowsPerProvider = 300, MaximumProviderAttempts = maximumAttempts
        };
        async Task<JsonElement> Send(object payload, string key, int expectedAttempts,
            HttpStatusCode expected = HttpStatusCode.OK, string? error = null, bool racing = false)
        {
            await journalGate.WaitAsync();
            try { await journal("before-hosted-cursor", new { actor, key, expectedAttempts }); }
            finally { journalGate.Release(); }
            Interlocked.Increment(ref requests);
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql/federation")
            { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
            request.Headers.Add("Idempotency-Key", key);
            using var response = await client!.SendAsync(request);
            var raw = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(raw);
            if (response.StatusCode == HttpStatusCode.Conflict && (racing || expected == HttpStatusCode.Conflict))
            {
                Interlocked.Increment(ref duplicates);
                Assert.AreEqual("OPERATION_ALREADY_EXECUTED", json.RootElement.GetProperty("code").GetString());
                return json.RootElement.Clone();
            }
            Assert.AreEqual(expected, response.StatusCode, raw);
            var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
            var operation = await outbox.GetAsync(operationId, CancellationToken.None);
            Assert.AreEqual("settled", operation.State);
            Assert.AreEqual(expectedAttempts, operation.ProviderAttempts.Count);
            Assert.AreEqual(expectedAttempts, operation.Receipts.Count);
            Assert.AreEqual(expectedAttempts * 0.002m, authority.Settlements[operationId].ActualCostUsd);
            if (expectedAttempts == 0) Assert.IsFalse(string.IsNullOrWhiteSpace(operation.NoProviderExecutionReason));
            if (expectedAttempts == 3)
                CollectionAssert.AreEquivalent(selected.Select(value => value.ToString()).ToArray(), operation.Receipts.Select(value => value.Provider).ToArray());
            if (error != null) Assert.AreEqual(error, json.RootElement.GetProperty("errorCode").GetString());
            Assert.IsFalse(raw.Contains("providerUniqueStorageKey", StringComparison.OrdinalIgnoreCase));
            Interlocked.Increment(ref settled);
            Interlocked.Add(ref attempts, expectedAttempts);
            return json.RootElement.Clone();
        }
        static (string Cursor, string Key) Next(JsonElement response)
        {
            var next = response.GetProperty("result").GetProperty("continuation");
            return (next.GetProperty("cursor").GetString()!, next.GetProperty("nextRequestKey").GetString()!);
        }
        static Guid[] RowIds(JsonElement response) => response.GetProperty("result").GetProperty("rows")
            .EnumerateArray().Select(row => row.GetProperty("selected").GetProperty("row").GetProperty("id").GetGuid()).ToArray();

        try
        {
            await Start(initialKeys.GetRequiredService<IDataProtectionProvider>());
            var first = await Send(Intent(), Guid.NewGuid().ToString("D"), 3);
            var oldCursor = Next(first);
            var observed = RowIds(first).ToList();
            await Send(Intent(oldCursor.Cursor), Guid.NewGuid().ToString("D"), 0,
                HttpStatusCode.BadRequest, "CQL_CURSOR_REQUEST_BINDING");
            await Send(Intent("not-a-protected-token"), Guid.NewGuid().ToString("D"), 0,
                HttpStatusCode.BadRequest, "CQL_CURSOR_INVALID");

            // A fresh host/key-provider instance selects the new key while retaining
            // the old key for decrypting the previously issued page. Mongo outbox survives.
            rotatedKeys.GetRequiredService<IKeyManager>().CreateNewKey(
                DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.AddDays(3));
            await Start(rotatedKeys.GetRequiredService<IDataProtectionProvider>());
            var second = await Send(Intent(oldCursor.Cursor), oldCursor.Key, 3);
            observed.AddRange(RowIds(second));
            var started = authority.Started.Count;
            await Send(Intent(oldCursor.Cursor), oldCursor.Key, 0, HttpStatusCode.Conflict);
            Assert.AreEqual(started, authority.Started.Count);
            var next = Next(second);
            var third = await Send(Intent(next.Cursor), next.Key, 3);
            observed.AddRange(RowIds(third));
            CollectionAssert.AreEqual(new[] { ids[0], ids[2], ids[3], ids[3], ids[3], ids[4] }, observed.ToArray());
            Assert.IsFalse(third.GetProperty("result").GetProperty("hasMore").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, third.GetProperty("result").GetProperty("continuation").ValueKind);

            rotatedKeys.GetRequiredService<IKeyManager>().RevokeKey(oldKey.KeyId, "owned cursor acceptance revocation");
            await Start(revokedKeys.GetRequiredService<IDataProtectionProvider>());
            await Send(Intent(oldCursor.Cursor), Guid.NewGuid().ToString("D"), 0,
                HttpStatusCode.BadRequest, "CQL_CURSOR_INVALID");

            var intentRoot = Next(await Send(Intent(), Guid.NewGuid().ToString("D"), 3));
            await Send(Intent(intentRoot.Cursor, take: 3), intentRoot.Key, 0,
                HttpStatusCode.BadRequest, "CQL_CURSOR_INTENT_INVALID");
            var policyRoot = Next(await Send(Intent(), Guid.NewGuid().ToString("D"), 3));
            policy.Mode = 2;
            await Send(Intent(policyRoot.Cursor), policyRoot.Key, 0, HttpStatusCode.Forbidden, "CQL_ACCESS_FORBIDDEN");
            policy.Mode = 0;

            var concurrent = Next(await Send(Intent(), Guid.NewGuid().ToString("D"), 3));
            started = authority.Started.Count;
            var beforeDuplicates = duplicates;
            await Task.WhenAll(Send(Intent(concurrent.Cursor), concurrent.Key, 3, racing: true),
                Send(Intent(concurrent.Cursor), concurrent.Key, 3, racing: true));
            Assert.AreEqual(beforeDuplicates + 1, duplicates);
            Assert.AreEqual(started + 1, authority.Started.Count);

            sources.Default = ProviderType.SQLLiteDBOASIS;
            var captured = Next(await Send(Intent(take: 1, scope: "CurrentDefault"), Guid.NewGuid().ToString("D"), 1));
            sources.Default = ProviderType.Neo4jOASIS;
            var pinned = await Send(Intent(captured.Cursor, take: 1, scope: "CurrentDefault"), captured.Key, 1);
            Assert.AreEqual(ProviderType.SQLLiteDBOASIS.ToString(), pinned.GetProperty("result").GetProperty("providers")[0].GetString());

            var limited = Next(await Send(Intent(take: 1, maximumAttempts: 6), Guid.NewGuid().ToString("D"), 3));
            var exhausted = (await Send(Intent(limited.Cursor, take: 1, maximumAttempts: 6), limited.Key, 3)).GetProperty("result");
            Assert.IsTrue(exhausted.GetProperty("hasMore").GetBoolean());
            Assert.AreEqual(JsonValueKind.Null, exhausted.GetProperty("continuation").ValueKind);
            Assert.AreEqual("BudgetExhausted", exhausted.GetProperty("continuationUnavailable").GetString());

            var expiring = Next(await Send(Intent(), Guid.NewGuid().ToString("D"), 3));
            clock.Now = clock.Now.AddMinutes(6);
            await Send(Intent(expiring.Cursor), expiring.Key, 0, HttpStatusCode.BadRequest, "CQL_CURSOR_INVALID");
            Assert.AreEqual(20, requests);
            Assert.AreEqual(18, settled);
            Assert.AreEqual(32, attempts);
            Assert.AreEqual(2, duplicates);
            await journal("hosted-cursor-verified", new { requests, settled, attempts, duplicates, hostRestarts = 2, rotations = 1, revocations = 1 });
            return (requests, settled, attempts, duplicates);
        }
        finally
        {
            client?.Dispose();
            if (app != null) { await app.StopAsync(); await app.DisposeAsync(); }
        }
    }
}

extern alias ShippingWeb4;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using ShippingPolicy = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.ICqlReadAccessPolicy;
using ShippingProviders = ShippingWeb4::NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql.ICqlV1Providers;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1Web4NativeTests
{
    private sealed class ShippingFederationSources(Dictionary<ProviderType, IOASISStorageProvider> providers) : ShippingProviders
    {
        internal int Lookups;
        internal ProviderType Default = ProviderType.SQLLiteDBOASIS;
        internal bool Encrypted, WrongKey;
        internal static EncryptionSettings Encryption(bool wrong = false) => new()
        { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string(wrong ? 'w' : 'e', 32) };
        public IOASISStorageProvider Resolve(ProviderType? requested)
        { Lookups++; return providers[requested ?? Default]; }
        public HolonManager CreateManager(IOASISStorageProvider storage)
        {
            var manager = new Sources(providers).CreateManager(storage);
            if (Encrypted) manager.OASISDNA.OASIS.Security = new SecuritySettings { HolonDataEncryption = Encryption(WrongKey) };
            return manager;
        }
    }

    private sealed class ShippingFederationPolicy(Guid actor, Guid[] ids, Guid encryptedId) : ShippingPolicy
    {
        internal int Mode, Calls;
        internal Action? OnAuthorize;
        public OASISResult<CosmicQueryReadAccess> Authorize(Guid capturedActor, ProviderType authority)
        {
            Assert.AreEqual(actor, capturedActor);
            Calls++;
            OnAuthorize?.Invoke();
            if (Mode == 2 && authority == ProviderType.SQLLiteDBOASIS)
                return new() { IsError = true, ErrorCode = "owned-denial" };
            var granted = authority switch
            {
                ProviderType.MongoDBOASIS => new[] { ids[0], ids[3] },
                ProviderType.SQLLiteDBOASIS => new[] { ids[2], ids[3] },
                ProviderType.Neo4jOASIS => new[] { ids[4], ids[3] },
                _ => throw new InvalidOperationException("Unexpected fixture authority.")
            };
            if (Mode == 4 || Mode == 1 && authority == ProviderType.SQLLiteDBOASIS) granted = Array.Empty<Guid>();
            if (Mode == 5) granted = new[] { encryptedId };
            return CosmicQueryReadAccess.AuthoritativeRecords(actor,
                Mode == 3 && authority == ProviderType.Neo4jOASIS ? ProviderType.MongoDBOASIS : authority, granted);
        }
    }

    private static async Task<(int Requests, int Settled, int Attempts)> VerifyShippingFederation(
        Dictionary<ProviderType, IOASISStorageProvider> providers, MongoUsageOutboxStore outbox,
        Authority authority, string secret, Guid actor, Guid[] ids, Guid encryptedId, string tag,
        Func<string, object?, Task> journal)
    {
        var sources = new ShippingFederationSources(providers);
        var policy = new ShippingFederationPolicy(actor, ids, encryptedId);
        var selected = providers.Keys.ToArray();
        var requests = 0; var settled = 0; var totalAttempts = 0;
        await using var app = ShippingReadHost(sources, policy, selected, outbox, authority, secret);
        await app.StartAsync();
        try
        {
            var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var client = new HttpClient { BaseAddress = new Uri(address), Timeout = TimeSpan.FromSeconds(30) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authority.Tokens.Single(pair => pair.Value == actor).Key);
            var filters = new[] { new { MetadataPath = new[] { "fixture" }, Operator = "Equal", Scalar = "String", Value = tag } };
            object Intent(string identity = "PreserveCopies", int skip = 0, int take = 100, int rows = 300, int bytes = 1048576)
                => new { Scope = "Named", Providers = selected.Select(value => value.ToString()).ToArray(),
                    Source = new { Filters = filters, OrderBy = "Name", Take = 100 }, IdentityMode = identity,
                    ProviderPriority = identity == "ProviderPriority" ? selected.Reverse().Select(value => value.ToString()).ToArray() : Array.Empty<string>(),
                    Skip = skip, Take = take, MaximumInputRows = rows, MaximumOutputBytes = bytes };

            async Task<JsonElement> Send(object payload, int attempts, HttpStatusCode expected = HttpStatusCode.OK,
                int lookups = 3, string? error = null)
            {
                var key = "federation-" + Guid.NewGuid().ToString("N");
                var beforeLookups = sources.Lookups; var beforeCalls = policy.Calls;
                await journal("before-hosted-federation", new { actor, key, policy.Mode, expectedAttempts = attempts });
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/data/query-cql/federation")
                { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
                request.Headers.Add("Idempotency-Key", key);
                using var response = await client.SendAsync(request);
                var raw = await response.Content.ReadAsStringAsync();
                Assert.AreEqual(expected, response.StatusCode, raw);
                var operationId = response.Headers.GetValues("X-OASIS-Operation-Id").Single();
                var operation = await outbox.GetAsync(operationId, CancellationToken.None);
                Assert.AreEqual("settled", operation.State);
                Assert.AreEqual(actor.ToString("D"), operation.UserId);
                Assert.AreEqual(attempts, operation.ProviderAttempts.Count);
                Assert.AreEqual(attempts, operation.Receipts.Count);
                Assert.AreEqual(attempts * 0.002m, authority.Settlements[operationId].ActualCostUsd);
                Assert.AreEqual(lookups, sources.Lookups - beforeLookups);
                Assert.AreEqual(lookups, policy.Calls - beforeCalls);
                if (attempts == 0) Assert.IsFalse(string.IsNullOrWhiteSpace(operation.NoProviderExecutionReason));
                if (attempts == 3)
                    CollectionAssert.AreEquivalent(selected.Select(value => value.ToString()).ToArray(), operation.Receipts.Select(value => value.Provider).ToArray());
                foreach (var receipt in operation.Receipts)
                { Assert.AreEqual(0.002m, receipt.ActualCostUsd); Assert.AreEqual("owned-fixture-v1", receipt.PricingCatalogueVersion); }
                Assert.IsFalse(raw.Contains("providerUniqueStorageKey", StringComparison.OrdinalIgnoreCase));
                using var json = JsonDocument.Parse(raw);
                if (error != null)
                {
                    Assert.AreEqual(error, json.RootElement.GetProperty("errorCode").GetString());
                    Assert.IsTrue(!json.RootElement.TryGetProperty("result", out var result) || result.ValueKind == JsonValueKind.Null);
                }
                requests++; settled++; totalAttempts += attempts;
                return json.RootElement.Clone();
            }

            var page = (await Send(Intent(skip: 1, take: 4), 3)).GetProperty("result");
            Assert.AreEqual(6, page.GetProperty("observedInputRows").GetInt32());
            CollectionAssert.AreEqual(new[] { ids[2], ids[3], ids[3], ids[3] },
                page.GetProperty("rows").EnumerateArray().Select(row => row.GetProperty("selected").GetProperty("row").GetProperty("id").GetGuid()).ToArray());
            var priority = (await Send(Intent("ProviderPriority"), 3)).GetProperty("result");
            Assert.AreEqual(4, priority.GetProperty("resolvedRows").GetInt32());
            var publicCopy = priority.GetProperty("rows").EnumerateArray().Single(row => row.GetProperty("selected").GetProperty("row").GetProperty("id").GetGuid() == ids[3]);
            Assert.AreEqual(selected.Last().ToString(), publicCopy.GetProperty("selected").GetProperty("provider").GetString());
            Assert.AreEqual(3, publicCopy.GetProperty("copies").GetArrayLength());
            Assert.IsFalse(publicCopy.GetProperty("causalAgreementEstablished").GetBoolean());
            policy.Mode = 1;
            var revoked = (await Send(Intent("ProviderPriority"), 3)).GetProperty("result");
            Assert.AreEqual(4, revoked.GetProperty("observedInputRows").GetInt32());
            Assert.AreEqual(3, revoked.GetProperty("rows").GetArrayLength());
            policy.Mode = 2;
            await Send(Intent(), 0, HttpStatusCode.Forbidden, 2, "CQL_ACCESS_FORBIDDEN");
            policy.Mode = 3;
            await Send(Intent(), 0, HttpStatusCode.ServiceUnavailable, 3, "CQL_ACCESS_POLICY_INVALID");
            policy.Mode = 0;
            await Send(Intent(rows: 1), 1, HttpStatusCode.ServiceUnavailable, error: "CQL_FEDERATION_INPUT_LIMIT");
            await Send(Intent(bytes: 250), 3, HttpStatusCode.RequestEntityTooLarge, error: "CQL_FEDERATION_OUTPUT_LIMIT");
            policy.Mode = 4;
            Assert.AreEqual(0, (await Send(Intent(), 3)).GetProperty("result").GetProperty("rows").GetArrayLength());
            await Send(new { Cursor = "caller-cannot-inject-continuity" }, 0, HttpStatusCode.BadRequest, 0);
            policy.Mode = 0;
            policy.OnAuthorize = () => sources.Default = ProviderType.Neo4jOASIS;
            var current = (await Send(new { Source = new { Filters = filters } }, 1, lookups: 1)).GetProperty("result");
            Assert.AreEqual(ProviderType.SQLLiteDBOASIS.ToString(), current.GetProperty("providers")[0].GetString());
            Assert.AreEqual(2, current.GetProperty("rows").GetArrayLength());
            policy.OnAuthorize = null;

            // Fresh owned private records exercise the actual shipping manager's
            // decryption path. Query only their canonical IDs, never ciphertext.
            await journal("before-federation-encrypted-seed", new { encryptedId });
            var ciphertext = Core.Helpers.PasswordEncryptionHelper.EncryptValue("{\"privateNote\":\"native decrypted value\"}", ShippingFederationSources.Encryption());
            foreach (var provider in providers.Values)
            {
                var seeded = await provider.SaveHolonAsync(new Holon { Id = encryptedId, CreatedByAvatarId = Guid.NewGuid(),
                    Name = "encrypted federation", MetaData = new() { ["__oasis_enc__"] = ciphertext } }, saveChildren: false);
                Assert.IsFalse(seeded.IsError, seeded.Message + seeded.Exception);
            }
            policy.Mode = 5; sources.Encrypted = true;
            var encryptedRequest = new { Scope = "Named", Providers = selected.Select(value => value.ToString()).ToArray(),
                Source = new { IncludeMetadata = true, Filters = new[] { new { Field = "Id", Operator = "Equal", Scalar = "Guid", Value = encryptedId.ToString("D") } } } };
            var decrypted = (await Send(encryptedRequest, 3)).GetProperty("result");
            Assert.AreEqual(3, decrypted.GetProperty("rows").GetArrayLength());
            foreach (var row in decrypted.GetProperty("rows").EnumerateArray())
                Assert.AreEqual("native decrypted value", row.GetProperty("selected").GetProperty("row").GetProperty("metadata").GetProperty("privateNote").GetString());
            sources.WrongKey = true;
            await Send(encryptedRequest, 3, HttpStatusCode.ServiceUnavailable, error: "HOLON_METADATA_DECRYPTION_FAILED");
            foreach (var provider in providers.Values)
            {
                var stored = await provider.LoadHolonAsync(encryptedId, loadChildren: false);
                Assert.IsFalse(stored.IsError, stored.Message);
                Assert.AreEqual(ciphertext, stored.Result.MetaData["__oasis_enc__"].ToString());
                Assert.IsFalse(stored.Result.MetaData.ContainsKey("privateNote"));
                var removed = await provider.DeleteHolonAsync(encryptedId);
                Assert.IsFalse(removed.IsError, removed.Message);
            }
            await journal("after-federation-encrypted-teardown", new { encryptedId });

            var started = authority.Started.Count; var policyCalls = policy.Calls;
            client.DefaultRequestHeaders.Authorization = null;
            using var anonymous = await client.PostAsync("/api/data/query-cql/federation", new StringContent("{}", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymous.StatusCode);
            Assert.AreEqual(started, authority.Started.Count);
            Assert.AreEqual(policyCalls, policy.Calls);
            requests++;
        }
        finally { await app.StopAsync(); }
        return (requests, settled, totalAttempts);
    }
}

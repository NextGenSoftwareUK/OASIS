using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosRoutingInspectionTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task InspectExactFailedProfileOriginalsWithoutMutation()
    {
        string Required(string key) => Environment.GetEnvironmentVariable(key) ?? throw new InvalidOperationException(key);
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var root = Required("OASIS_HYPERDRIVE_TEST_ROOT");
        var journal = Path.Combine(root, "independent-routing-pending.json");
        const string journalHash = "F901B2959830F84882BBC338992EF734612D7009BBF318E36F178E279CE98BC1";
        const string oldHash = "9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        Assert.Equal(journalHash, Hash(journal));
        Assert.Equal(oldHash, Hash(Path.Combine(root, "replication-pending.json")));
        using var intent = JsonDocument.Parse(File.ReadAllText(journal));
        Assert.Equal("537d7453-bfa6-12e7-8cb8-e3af7c758df5", intent.RootElement.GetProperty("operationId").GetString());
        Assert.Equal("usage_cosmos_independent_routing", intent.RootElement.GetProperty("database").GetString());
        Assert.Equal("http://127.0.0.1:26657", Required("OASIS_COSMOS_TEST_RPC"));
        Assert.Equal("oasis-cosmos-local", Required("OASIS_COSMOS_TEST_CHAIN"));
        Assert.Equal(Required("OASIS_COSMOS_TEST_CONTRACT"), intent.RootElement.GetProperty("contract").GetString());
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection, Required("SUBSCRIPTION_TEST_MONGODB_URI"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var token = deadline.Token;
        var store = new MongoUsageOutboxStore(connection, "usage_cosmos_independent_routing");
        var operation = await store.GetAsync("537d7453-bfa6-12e7-8cb8-e3af7c758df5", token);
        Assert.NotNull(operation);
        var before = JsonSerializer.Serialize(operation);
        Assert.Null(operation.Settlement);
        // No signing key or provider activation: this path only verifies persisted signatures and reads indexed originals.
        using var cosmos = new CosmosBlockChainOASIS(rpcEndpoint: Required("OASIS_COSMOS_TEST_RPC"),
            chainId: "oasis-cosmos-local", contractAddress: Required("OASIS_COSMOS_TEST_CONTRACT"),
            addressPrefix: "wasm", gasPrice: "0.025stake", nativeDenom: "stake", nativeDecimals: 0,
            storageEncryptionKeyResolver: () => SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")));
        var originals = operation.ProviderAttempts.Where(attempt => attempt.Provider == "CosmosBlockChainOASIS").ToArray();
        Assert.Equal(5, originals.Length);
        Assert.Equal(5, originals.Select(attempt => attempt.ProviderRequestId).Distinct().Count());
        var receipts = new List<object>();
        foreach (var original in originals)
        {
            var receipt = await cosmos.GetPreparedNativeFeeReceiptAsync(original, token);
            Assert.False(receipt.IsError, receipt.Message);
            Assert.Equal(original.ProviderRequestId, receipt.Result.GetProperty("transactionHash").GetString());
            Assert.True(receipt.Result.GetProperty("success").GetBoolean());
            receipts.Add(new { original, anteFeeReceipt = receipt.Result });
        }
        // Read exact known records directly; public provider activation intentionally requires a signing key.
        var storage = (IKeyValueBackend)typeof(CosmosBlockChainOASIS).GetField("_privateStorage", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cosmos)!;
        var avatarId = intent.RootElement.GetProperty("avatarId").GetGuid();
        var leafId = intent.RootElement.GetProperty("holonId").GetGuid();
        foreach (var key in new[] { "oasis/avatar/" + avatarId.ToString("N"), "oasis/avatardetail/" + avatarId.ToString("N"), "oasis/holon/" + leafId.ToString("N") })
        {
            var record = await storage.GetAsync(key, token);
            Assert.NotNull(record);
            using var parsed = JsonDocument.Parse(record);
            Assert.Equal(key.EndsWith(leafId.ToString("N")) ? leafId : avatarId, parsed.RootElement.GetProperty("Id").GetGuid());
        }
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        try
        {
            var byId = await local.LoadAvatarAsync(avatarId);
            Assert.False(byId.IsError, byId.Message);
            Assert.Equal(avatarId, byId.Result?.Id);
            Assert.Empty(byId.Result!.ProviderUniqueStorageKey);
            var byKey = await local.LoadAvatarByProviderKeyAsync(avatarId.ToString());
            Assert.False(byKey.IsError, byKey.Message);
            Assert.Null(byKey.Result); // Exact retained failure; do not rewrite the incident's evidence.
        }
        finally { await local.DeActivateProviderAsync(); }
        Assert.Equal(before, JsonSerializer.Serialize(await store.GetAsync("537d7453-bfa6-12e7-8cb8-e3af7c758df5", token)));
        Assert.Equal(journalHash, Hash(journal));
        Assert.Equal(oldHash, Hash(Path.Combine(root, "replication-pending.json")));
        Assert.False(cosmos.IsProviderActivated);
        var report = JsonSerializer.Serialize(new { originalJournalSha256 = journalHash, operationId = "537d7453-bfa6-12e7-8cb8-e3af7c758df5",
            originals = receipts, readOnly = true, ledgerUnchanged = true, replacementAuthorized = false,
            threeNativeRecordsVerified = true, retainedLocalProviderKeyGapVerified = true,
            finalFeeCoverageVerified = false, logicalOutcomeResolved = false });
        await File.WriteAllTextAsync(Path.Combine(root, "routing-profile-original-inspection-current.json"), report, token);
        output.WriteLine(report);
    }
}

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
    public async Task ReconcileExactFailedProfileWithNewAdmittedCleanupWithoutReplay()
    {
        // Revalidate original signatures, native state and exact local failure before authorizing any cleanup.
        await InspectExactFailedProfileOriginalsWithoutMutation();
        var root = Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        var journal = Path.Combine(root, "independent-routing-pending.json");
        var cleanupJournal = Path.Combine(root, "routing-cleanup-pending.json");
        var archive = Path.Combine(root, "routing-profile-reviewed-reconciliation.json");
        var originalArchive = Path.Combine(root, "routing-profile-original-intent-resolved.json");
        var cleanupArchive = Path.Combine(root, "routing-profile-cleanup-intent-resolved.json");
        foreach (var path in new[] { cleanupJournal, archive, originalArchive, cleanupArchive }) Assert.False(File.Exists(path));
        using var intent = JsonDocument.Parse(File.ReadAllText(journal));
        var avatarId = intent.RootElement.GetProperty("avatarId").GetGuid();
        var leafId = intent.RootElement.GetProperty("holonId").GetGuid();
        const string originalId = "537d7453-bfa6-12e7-8cb8-e3af7c758df5";
        const string connection = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(150)); var token = deadline.Token;
        var store = new MongoUsageOutboxStore(connection, "usage_cosmos_independent_routing");
        var original = await store.GetAsync(originalId, token); var originalSnapshot = JsonSerializer.Serialize(original);
        var sender = original.ProviderAttempts.First(attempt => attempt.Provider == "CosmosBlockChainOASIS").NativePreparation.Sender;
        using var cosmos = CosmosBlockChainOASIS.CreateWithNativeFeeLimits(
            CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string, string> { ["stake"] = "50000" }, "oasis-cosmos-local"),
            rpcEndpoint: "http://127.0.0.1:26657", chainId: "oasis-cosmos-local",
            privateKey: Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_KEY")!,
            contractAddress: intent.RootElement.GetProperty("contract").GetString()!,
            addressPrefix: "wasm", gasPrice: "0.025stake", nativeDenom: "stake", nativeDecimals: 0,
            storageEncryptionKeyResolver: () => SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")));
        var user = Guid.NewGuid().ToString("D");
        using var http = new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority = new Web4SubscriptionAuthorizationClient(http, "https://offline-authority.invalid", "WEB6", "test-only-key");
        var execution = await new Web4UsageExecution(authority, store, new Web4UsageLedgerOptions { Service = "WEB6" }).BeginAsync(
            "test-only", user, "reviewed-profile-cleanup-" + originalId, new string('c', 64), "POST /reviewed-profile-cleanup",
            new UsageEndpointPolicy { MeterCategory = "api.request", ReservedUnits = 1, EstimatedCostUsd = .25m, MaximumNativeFeeCostUsd = .25m }, token);
        var now = DateTime.UtcNow.AddSeconds(-1);
        execution = execution.WithNativePrices(new[] { new NativeFeePriceQuote(Guid.Parse(execution.OperationId), "CosmosBlockChainOASIS", "oasis-cosmos-local",
            "stake", "local-test-stake", 0, .000001m, "synthetic-local-price", "fixture-v1", "fixture-v1", now, now, now, now.AddMinutes(5)) });
        using var file = new FileStream(cleanupJournal, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(file, new { originalId, avatarId, leafId, cleanupOperationId = execution.OperationId,
            originalJournalSha256 = Hash(journal), database = "usage_cosmos_independent_routing", recovery = "Inspect original cleanup attempts; never replay uncertain deletes." }, cancellationToken: token);
        file.Flush(true);
        await cosmos.ExecutePreparedStorageAsync(async () => {
            var profile = await cosmos.LoadAvatarAsync(avatarId);
            Assert.False(profile.IsError, profile.Message);
            Assert.Equal(intent.RootElement.GetProperty("Username").GetString(), profile.Result.Username);
            var leaf = await cosmos.LoadHolonAsync(leafId, loadChildren: false);
            Assert.False(leaf.IsError, leaf.Message); Assert.Equal("independent-cosmos-v2-leaf", leaf.Result.Name);
            var deleted = await cosmos.DeleteHolonAsync(leafId);
            Assert.False(deleted.IsError, deleted.Message);
            var deletedProfile = await cosmos.DeleteAvatarAsync(avatarId, softDelete: false);
            Assert.False(deletedProfile.IsError, deletedProfile.Message); Assert.True(deletedProfile.Result);
            return true;
        }, execution, "reviewed-profile-cleanup", sender, token);
        var cleanup = await store.GetAsync(execution.OperationId, token);
        Assert.Equal(5, cleanup.ProviderAttempts.Count);
        var fees = new List<object>();
        foreach (var attempt in cleanup.ProviderAttempts)
        {
            var receipt = await cosmos.GetPreparedNativeFeeReceiptAsync(attempt, token);
            Assert.False(receipt.IsError, receipt.Message); Assert.True(receipt.Result.GetProperty("success").GetBoolean());
            fees.Add(new { attempt, anteFeeReceipt = receipt.Result });
        }
        Assert.Null((await cosmos.LoadAvatarAsync(avatarId)).Result);
        Assert.Null((await cosmos.LoadAvatarDetailAsync(avatarId)).Result);
        Assert.Null((await cosmos.LoadHolonAsync(leafId, loadChildren: false)).Result);
        Assert.Null((await cosmos.LoadAvatarByUsernameAsync(intent.RootElement.GetProperty("Username").GetString()!)).Result);
        Assert.Null((await cosmos.LoadAvatarByEmailAsync(intent.RootElement.GetProperty("Email").GetString()!)).Result);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        try
        {
            var deleted = await local.DeleteHolonAsync(leafId); Assert.False(deleted.IsError, deleted.Message);
            var profileDeleted = await local.DeleteAvatarAsync(avatarId, softDelete: false); Assert.False(profileDeleted.IsError, profileDeleted.Message);
            Assert.Null((await local.LoadAvatarAsync(avatarId)).Result);
            Assert.Null((await local.LoadAvatarDetailAsync(avatarId)).Result);
            Assert.Null((await local.LoadHolonAsync(leafId, loadChildren: false)).Result);
        }
        finally { await local.DeActivateProviderAsync(); }
        Assert.Equal(originalSnapshot, JsonSerializer.Serialize(await store.GetAsync(originalId, token)));
        Assert.Equal("9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81", Hash(Path.Combine(root, "replication-pending.json")));
        var evidence = JsonSerializer.Serialize(new { originalIntent = intent.RootElement, original,
            cleanup, cleanupOriginals = fees, exactFixtureCleanupVerified = true, originalLedgerRetained = true,
            originalLogicalTestSucceeded = false, finalFeeCoverageVerified = false, settlementAuthorized = false });
        using (var archiveFile = new FileStream(archive, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        { await archiveFile.WriteAsync(Encoding.UTF8.GetBytes(evidence), token); archiveFile.Flush(true); }
        Assert.Equal(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(evidence))), Hash(archive));
        file.Dispose();
        File.Move(journal, originalArchive); File.Move(cleanupJournal, cleanupArchive);
        output.WriteLine("Exact synthetic fixture cleaned using five NEW admitted deletes; original ledger retained; archive SHA256=" + Hash(archive));
    }

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

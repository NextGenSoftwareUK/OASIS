using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public class TonNativeStorageTests
{
    [TestMethod]
    [TestCategory("NativeTonTVM")]
    public async Task CanonicalEncryptedHolonsSurviveIndependentNativeWorkersAndReadOnlyReopen()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_TON_NATIVE_ROOT");
        if (string.IsNullOrWhiteSpace(root)) Assert.Inconclusive("Native TVM acceptance requires the explicit native runner.");
        var directory = Path.Combine(root!, "TestResults/TonCqlCurrent/csharp-store-tests");
        Assert.IsFalse(Directory.Exists(directory), "Preserve an unfinished fixture; never reset it.");
        Directory.CreateDirectory(directory);
        var options = new TonSandboxStorageOptions(
            Environment.GetEnvironmentVariable("OASIS_TON_NODE")!,
            Path.Combine(root!, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/native-worker.cjs"),
            Path.Combine(root!, "TestResults/TonCqlCurrent/compiled/TonHolonStore_TonHolonStore.cjs"),
            directory, Guid.NewGuid(), Environment.GetEnvironmentVariable("OASIS_TON_CODE_HASH")!, ReadOnly: false);
        var native = new TonSandboxStorageBackend(options);
        var created = await native.CreateSourceAsync();
        var creationId = created.GetProperty("operationId").GetGuid();
        // Public deterministic test material; no real credential is created.
        var key = SHA256.HashData(Encoding.UTF8.GetBytes("COSMIC native TON synthetic storage test key"));
        var actor = Guid.NewGuid();
        var rows = new[] { "Espresso ☕", "Latte", "Tea" }.Select(name => new Holon {
            Id = Guid.NewGuid(), Name = name, Description = "Native TVM canonical storage fixture", CreatedByAvatarId = actor
        }).ToArray();
        using var provider = new TONOASIS(options, () => (byte[])key.Clone()) { IsVersionControlEnabled = false };
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        try
        {
            foreach (var row in rows)
            {
                var saved = await provider.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message + saved.Exception);
            }
        }
        finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }

        var physicalKeys = await native.ListKeysAsync(string.Empty);
        Assert.AreEqual(3, physicalKeys.Count);
        foreach (var physical in physicalKeys)
        {
            StringAssert.StartsWith(physical, "oasis/private/v1/");
            var ciphertext = await native.GetAsync(physical);
            Assert.IsFalse(ciphertext.Contains("Espresso") || ciphertext.Contains("Native TVM canonical"));
        }
        var source = Path.Combine(directory, "source.json");
        var before = SHA256.HashData(await File.ReadAllBytesAsync(source));
        var acknowledgements = Directory.GetFiles(Path.Combine(directory, "operations"));
        Assert.AreEqual(4, acknowledgements.Length);
        var reader = new TonSandboxStorageBackend(options with { ReadOnly = true });
        using var reopened = new TONOASIS(options with { ReadOnly = true }, () => (byte[])key.Clone());
        Assert.IsFalse((await reopened.ActivateProviderAsync()).IsError);
        CosmicQueryPlan<Holon>? captured = null;
        try
        {
            foreach (var row in rows)
            {
                var loaded = await reopened.LoadHolonAsync(row.Id, loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
                Assert.AreEqual(row.Id, loaded.Result.Id);
                Assert.AreEqual(row.Name, loaded.Result.Name);
                Assert.AreEqual(actor, loaded.Result.CreatedByAvatarId);
            }
            var ids = rows.Select(row => row.Id).ToArray();
            foreach (var selected in new[] { ids, ids.Take(2).ToArray(), new[] { Guid.NewGuid() } })
            {
                var access = CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.TONOASIS, ids.Concat(selected).Distinct());
                Assert.IsFalse(access.IsError, access.Message);
                var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
                    .WithReadAccess(access.Result).Result
                    .Where(row => selected.Contains(row.Id)).Result;
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.TONOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.TONOASIS, reopened.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                captured = plan.Result;
                var result = await reopened.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Message + result.Exception);
                CollectionAssert.AreEquivalent(ids.Intersect(selected).ToArray(), result.Result.Select(row => row.Id).ToArray());
                if (selected.Length == 2)
                {
                    var overBudget = await reopened.ExecuteCosmicQueryAsync(plan.Result, maximumResults: 1);
                    Assert.IsTrue(overBudget.IsError);
                    Assert.AreEqual("CQL_QUERY_LIMIT", overBudget.ErrorCode);
                    Assert.IsNull(overBudget.Result);
                }
            }
            using (var cancelledQuery = new CancellationTokenSource())
            {
                cancelledQuery.Cancel();
                var result = await reopened.ExecuteCosmicQueryAsync(captured!, cancellationToken: cancelledQuery.Token);
                Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
                Assert.IsNull(result.Result);
            }
            var denied = await reopened.SaveHolonAsync(new Holon { Id = Guid.NewGuid(), Name = "must not write" }, saveChildren: false);
            Assert.IsTrue(denied.IsError);
            Assert.IsFalse(denied.IsSaved);
        }
        finally { Assert.IsFalse((await reopened.DeActivateProviderAsync()).IsError); }
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await reopened.ExecuteCosmicQueryAsync(captured!)).ErrorCode);
        CollectionAssert.AreEqual(before, SHA256.HashData(await File.ReadAllBytesAsync(source)));
        Assert.AreEqual(4, Directory.GetFiles(Path.Combine(directory, "operations")).Length);
        Assert.AreEqual("Acknowledged", (await reader.InspectOperationAsync(creationId)).GetProperty("status").GetString());
        using (var cancelled = new CancellationTokenSource())
        {
            cancelled.Cancel();
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => reader.GetAsync(physicalKeys[0], cancelled.Token));
        }
        using (var otherOwner = new FileStream(Path.Combine(directory, "source.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            await Assert.ThrowsExceptionAsync<IOException>(() => reader.GetAsync(physicalKeys[0]));
        using (var wrongKey = PersistedPrivateKeyValueBackend.Create(reader, () => new byte[32]))
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => wrongKey.VerifyAsync());
        CollectionAssert.AreEqual(before, SHA256.HashData(await File.ReadAllBytesAsync(source)));

        // Only this newly created synthetic fixture is disposable. Validate its
        // exact source/operation ownership before deleting named files.
        Assert.IsFalse(File.Exists(Path.Combine(directory, "pending.json")));
        using var state = JsonDocument.Parse(await File.ReadAllTextAsync(source));
        Assert.AreEqual(options.SourceId, state.RootElement.GetProperty("sourceId").GetGuid());
        foreach (var file in acknowledgements)
        {
            using var ack = JsonDocument.Parse(await File.ReadAllTextAsync(file));
            Assert.AreEqual(options.SourceId, ack.RootElement.GetProperty("sourceId").GetGuid());
            File.Delete(file);
        }
        Directory.Delete(Path.Combine(directory, "operations"));
        File.Delete(source);
        File.Delete(Path.Combine(directory, "source.lock"));
        Directory.Delete(directory);
        CryptographicOperations.ZeroMemory(key);
    }
}

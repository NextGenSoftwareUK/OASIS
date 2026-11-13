using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class ConvexStorageConformanceTests
{
    private sealed record RecoveryRow(string LogicalKey, string PhysicalKey, string Revision, string CipherSha256, string ValueSha256);
    private sealed record RecoverySnapshot(string FailureReportSha256, List<RecoveryRow> Rows);
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException("Missing " + name);
    private static (string Url, string Admin) Configuration()
    {
        if (Environment.GetEnvironmentVariable("OASIS_CONVEX_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Explicit owned Convex native conformance opt-in required.");
        var url = Required("OASIS_CONVEX_CONFORMANCE_URL");
        var uri = new Uri(url);
        Assert.IsTrue(uri.Scheme == "http" && uri.Host == "127.0.0.1" && uri.AbsolutePath == "/" && uri.UserInfo == "" && uri.Query == "" && uri.Fragment == "");
        return (url, Required("OASIS_CONVEX_CONFORMANCE_ADMIN_KEY"));
    }
    private static string Work()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        return Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "ConvexOASIS");
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task ShippingProviderPassesTheSharedStorageSuite(bool synchronous)
    {
        var config = Configuration();
        var directory = Path.Combine(Work(), synchronous ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var reportPath = Path.Combine(directory, "report.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve previous outcomes before another run.");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var native = new ConvexRecordBackend(config.Url, config.Admin, http);
        Assert.AreEqual(0, (await native.ListKeysAsync("")).Count, "Dedicated native table must be empty before dispatch.");
        var key = Convert.FromBase64String(Required("OASIS_CONVEX_CONFORMANCE_STORAGE_KEY"));
        Assert.AreEqual(32, key.Length);
        try
        {
            using var provider = new ConvexOASIS(config.Url, config.Admin, http, () => key.ToArray());
            var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(reportPath);
            var report = await CoreStorageConformance.RunAsync(provider, "ConvexOASIS", "native", synchronous, checkpoint.WriteAsync,
                (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
                expectedDefaultDeletion: "soft");
            Assert.IsTrue(report.Succeeded, report.CurrentCase + ": " + report.FailureType);
            Assert.AreEqual(0, (await native.ListKeysAsync("")).Count, "Independent native table inspection must confirm exact fixture cleanup.");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }

    [TestMethod]
    public async Task NativeRevisionsRejectStaleAndMissingMutations()
    {
        var config = Configuration();
        var directory = Work(); Directory.CreateDirectory(directory);
        var intent = Path.Combine(directory, "conditional-intent.json");
        Assert.IsFalse(File.Exists(intent), "Preserve the original conditional fixture before another run.");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var native = new ConvexRecordBackend(config.Url, config.Admin, http);
        Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
        var key = "conditional-probe-" + Guid.NewGuid().ToString("N");
        await File.WriteAllTextAsync(intent, System.Text.Json.JsonSerializer.Serialize(new { key, scope = "owned native conditional fixture" }));
        await native.CreateOnlyAsync(key, "synthetic-first");
        var first = await native.ReadObservedAsync(key); Assert.IsNotNull(first);
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => native.CreateOnlyAsync(key, "must-not-overwrite"));
        await native.ReplaceUnchangedAsync(key, "synthetic-second", first.Revision);
        var second = await native.ReadObservedAsync(key); Assert.IsNotNull(second);
        Assert.AreNotEqual(first.Revision, second.Revision);
        Assert.AreEqual("synthetic-second", second.Value);
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => native.ReplaceUnchangedAsync(key, "must-not-overwrite", first.Revision));
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => native.DeleteUnchangedAsync(key, first.Revision));
        await native.DeleteUnchangedAsync(key, second.Revision);
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => native.ReplaceUnchangedAsync(key, "must-not-create", second.Revision));
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => native.DeleteUnchangedAsync(key, second.Revision));
        Assert.IsNull(await native.ReadObservedAsync(key));
        Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
        await File.WriteAllTextAsync(Path.Combine(directory, "conditional-complete.json"), "{\"succeeded\":true,\"nativeRowsAfter\":0}");
    }

    [TestMethod]
    public async Task NativeListingSpansPagesAndCleansOnlyItsFixture()
    {
        var config = Configuration();
        var directory = Work(); Directory.CreateDirectory(directory);
        var intent = Path.Combine(directory, "pagination-intent.json");
        Assert.IsFalse(File.Exists(intent), "Preserve the original paging fixture.");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var native = new ConvexRecordBackend(config.Url, config.Admin, http);
        Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
        var prefix = "page-☃-" + Guid.NewGuid().ToString("N") + "/";
        var keys = Enumerable.Range(0, 105).Select(i => prefix + i.ToString("D3")).ToArray();
        await File.WriteAllTextAsync(intent, JsonSerializer.Serialize(new { keys, scope = "owned native pagination fixture" }));
        foreach (var key in keys) await native.CreateOnlyAsync(key, "synthetic-paging-value");
        var listed = await native.ListKeysAsync(prefix);
        CollectionAssert.AreEqual(keys, listed.ToArray());
        foreach (var key in keys)
        {
            var observed = await native.ReadObservedAsync(key); Assert.IsNotNull(observed);
            Assert.AreEqual("synthetic-paging-value", observed.Value);
            await native.DeleteUnchangedAsync(key, observed.Revision);
        }
        Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
        await File.WriteAllTextAsync(Path.Combine(directory, "pagination-complete.json"), "{\"succeeded\":true,\"records\":105,\"nativeRowsAfter\":0}");
    }

    [TestMethod]
    public async Task InspectOrCleanupPreservedPublicationFixture()
    {
        var action = Environment.GetEnvironmentVariable("OASIS_CONVEX_RECOVERY_ACTION");
        if (action is not ("inspect" or "cleanup")) Assert.Inconclusive("Explicit publication-fixture inspection/cleanup required.");
        var config = Configuration();
        var pending = Path.Combine(Work(), "async", "report.json.next");
        var pendingBytes = await File.ReadAllBytesAsync(pending);
        var report = JsonSerializer.Deserialize<CoreStorageConformance.Report>(pendingBytes)!;
        Assert.AreEqual("ConvexOASIS", report.Provider);
        Assert.AreEqual("core-storage-v2", report.SuiteVersion);
        Assert.AreEqual("native", report.Mode);
        Assert.IsFalse(report.Succeeded);
        Assert.AreEqual("parent-query", report.CurrentCase, "This recovery only owns the inspected pre-batch publication failure.");
        Assert.AreEqual(13, report.Passed.Count);
        var marker = "conformance-" + report.OwnerId.ToString("N");
        static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            $"oasis/avatar/{report.OwnerId:N}", $"oasis/avatardetail/{report.OwnerId:N}",
            "oasis/avatar-username/" + Encode(marker), "oasis/avatar-email/" + Encode(marker + "@example.invalid"),
            $"oasis/holon/{report.RootId:N}", $"oasis/holon/{report.ChildId:N}", $"oasis/holon-parent/{report.RootId:N}/{report.ChildId:N}"
        };
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var native = new ConvexRecordBackend(config.Url, config.Admin, http);
        var key = Convert.FromBase64String(Required("OASIS_CONVEX_CONFORMANCE_STORAGE_KEY"));
        try
        {
            using var backend = PersistedPrivateKeyValueBackend.Create(native, () => key.ToArray());
            var conditional = (IConditionalKeyValueBackend)backend;
            var logical = await backend.ListKeysAsync("");
            Assert.IsTrue(expected.SetEquals(logical), "Unknown logical record; do not delete anything.");
            var physical = await native.ListKeysAsync("");
            Assert.AreEqual(7, physical.Count, "Unknown native record; do not delete anything.");
            var observed = new Dictionary<string, (string Key, KeyValueObservation Observation)>();
            foreach (var physicalKey in physical)
            {
                var value = await native.ReadObservedAsync(physicalKey); Assert.IsNotNull(value);
                Assert.IsFalse(observed.ContainsKey(value.Revision));
                observed.Add(value.Revision, (physicalKey, value));
            }
            var rows = new List<RecoveryRow>();
            foreach (var logicalKey in logical.Order(StringComparer.Ordinal))
            {
                var value = await conditional.ReadObservedAsync(logicalKey); Assert.IsNotNull(value);
                Assert.IsTrue(observed.TryGetValue(value.Revision, out var raw));
                rows.Add(new(logicalKey, raw.Key, value.Revision,
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw.Observation.Value))),
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Value)))));
            }
            var snapshot = new RecoverySnapshot(Convert.ToHexString(SHA256.HashData(pendingBytes)), rows);
            var path = Path.Combine(Work(), "publication-failure-inspection.json");
            if (action == "inspect")
            {
                await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                await JsonSerializer.SerializeAsync(file, snapshot); file.Flush(true);
                return;
            }
            var original = JsonSerializer.Deserialize<RecoverySnapshot>(await File.ReadAllBytesAsync(path))!;
            Assert.AreEqual(JsonSerializer.Serialize(original), JsonSerializer.Serialize(snapshot), "Fixture bytes/revisions changed; do not replay cleanup.");
            foreach (var row in original.Rows)
                await conditional.DeleteUnchangedAsync(row.LogicalKey, row.Revision);
            Assert.AreEqual(0, (await native.ListKeysAsync("")).Count);
            await File.WriteAllTextAsync(Path.Combine(Work(), "publication-failure-cleanup.json"),
                JsonSerializer.Serialize(new { original.FailureReportSha256, DeletedRecords = 7, NativeRowsAfter = 0, Scope = "exact revision/hash-verified synthetic fixture only" }));
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}

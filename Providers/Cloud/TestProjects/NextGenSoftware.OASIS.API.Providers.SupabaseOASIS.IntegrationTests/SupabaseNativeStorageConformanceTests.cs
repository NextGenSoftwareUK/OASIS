using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class SupabaseNativeStorageConformanceTests
{
    [DataTestMethod]
    [DataRow("holon", "OASIS_SUPABASE_PAGINATION_IDS")]
    [DataRow("avatar", "OASIS_SUPABASE_PAGINATION_AVATAR_IDS")]
    [DataRow("detail", "OASIS_SUPABASE_PAGINATION_DETAIL_IDS")]
    public async Task ListingReturnsEveryPreseededIdentityAcrossPages(string kind, string manifestVariable)
    {
        if (Environment.GetEnvironmentVariable("OASIS_SUPABASE_PAGINATION") != "1")
            Assert.Inconclusive("Enable only for a dedicated Supabase pagination fixture.");

        var url = Environment.GetEnvironmentVariable("OASIS_SUPABASE_CONFORMANCE_URL");
        var key = Environment.GetEnvironmentVariable("OASIS_SUPABASE_CONFORMANCE_SERVICE_KEY");
        var manifest = Environment.GetEnvironmentVariable(manifestVariable);
        Assert.IsTrue(Uri.TryCreate(url, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps &&
            endpoint.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) &&
            endpoint.AbsolutePath == "/" && string.IsNullOrEmpty(endpoint.Query) &&
            string.IsNullOrEmpty(endpoint.Fragment) && string.IsNullOrEmpty(endpoint.UserInfo));
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));
        if (string.IsNullOrWhiteSpace(manifest))
            Assert.Inconclusive($"Configure {manifestVariable} for this listing.");

        var expected = JsonSerializer.Deserialize<Guid[]>(await File.ReadAllTextAsync(manifest!));
        Assert.IsNotNull(expected);
        Assert.IsTrue(expected.Length > 100 && expected.Distinct().Count() == expected.Length);

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var provider = new SupabaseOASIS(url!, key!, http);
        var active = await provider.ActivateProviderAsync();
        Assert.IsTrue(active.Result, active.Exception?.GetType().FullName);
        try
        {
            void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<T>> loaded, Func<T, Guid> getId)
            {
                Assert.IsFalse(loaded.IsError, loaded.Exception?.GetType().FullName);
                Assert.IsTrue(loaded.IsLoaded);
                Assert.AreEqual(expected.Length, loaded.LoadedCount);
                CollectionAssert.AreEquivalent(expected, loaded.Result.Select(getId).ToArray());
            }

            switch (kind)
            {
                case "holon":
                    Check(await provider.LoadAllHolonsAsync(loadChildren: false), holon => holon.Id);
                    break;
                case "avatar":
                    Check(await provider.LoadAllAvatarsAsync(), avatar => avatar.Id);
                    break;
                case "detail":
                    Check(await provider.LoadAllAvatarDetailsAsync(), detail => detail.Id);
                    break;
                default:
                    Assert.Fail($"Unknown listing kind {kind}.");
                    break;
            }
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task DuplicateActiveAvatarsBlockLookupExportAndDeletion()
    {
        if (Environment.GetEnvironmentVariable("OASIS_SUPABASE_DUPLICATE_IDENTITY_TEST") != "1")
            Assert.Inconclusive("Enable only for a dedicated hosted Supabase test project.");

        var url = Environment.GetEnvironmentVariable("OASIS_SUPABASE_CONFORMANCE_URL");
        var key = Environment.GetEnvironmentVariable("OASIS_SUPABASE_CONFORMANCE_SERVICE_KEY");
        Assert.IsTrue(Uri.TryCreate(url, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps &&
            endpoint.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) &&
            endpoint.AbsolutePath == "/" && string.IsNullOrEmpty(endpoint.Query) &&
            string.IsNullOrEmpty(endpoint.Fragment) && string.IsNullOrEmpty(endpoint.UserInfo));
        Assert.IsFalse(string.IsNullOrWhiteSpace(key));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var provider = new SupabaseOASIS(url!, key!, http);
        var marker = $"oasis-duplicate-{Guid.NewGuid():N}";
        var email = $"{marker}@example.test";
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var savedIds = new List<Guid>();
        try
        {
            foreach (var id in new[] { firstId, secondId })
            {
                var saved = await provider.SaveAvatarAsync(new Core.Holons.Avatar
                {
                    Id = id,
                    Username = marker,
                    Email = email
                });
                Assert.IsTrue(saved.IsSaved, saved.Exception?.GetType().Name);
                savedIds.Add(id);
            }

            var username = await provider.LoadAvatarByUsernameAsync(marker);
            var address = await provider.LoadAvatarByEmailAsync(email);
            var exported = await provider.ExportAllDataForAvatarByUsernameAsync(marker);
            var exportedByEmail = await provider.ExportAllDataForAvatarByEmailAsync(email);
            var deleted = await provider.DeleteAvatarByEmailAsync(email, softDelete: false);
            var deletedByUsername = await provider.DeleteAvatarByUsernameAsync(marker, softDelete: false);
            foreach (var code in new[] { username.ErrorCode, address.ErrorCode, exported.ErrorCode, exportedByEmail.ErrorCode, deleted.ErrorCode, deletedByUsername.ErrorCode })
                Assert.AreEqual(Common.OASISResultErrorCodes.AmbiguousIdentity, code);

            foreach (var id in savedIds)
                Assert.IsTrue((await provider.LoadAvatarAsync(id)).IsLoaded);
        }
        finally
        {
            foreach (var id in savedIds)
                Assert.IsTrue((await provider.DeleteAvatarAsync(id, softDelete: false)).IsDeleted,
                    $"Could not remove fixture avatar {id}.");
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstSupabase(bool sync)
    {
        if (Environment.GetEnvironmentVariable("OASIS_SUPABASE_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Enable this suite only for a dedicated Supabase test project.");

        var url = Environment.GetEnvironmentVariable("OASIS_SUPABASE_CONFORMANCE_URL");
        var key = Environment.GetEnvironmentVariable("OASIS_SUPABASE_CONFORMANCE_SERVICE_KEY");
        Assert.IsTrue(Uri.TryCreate(url, UriKind.Absolute, out var endpoint) &&
            endpoint.Scheme == Uri.UriSchemeHttps &&
            endpoint.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase) &&
            endpoint.AbsolutePath == "/" && string.IsNullOrEmpty(endpoint.Query) &&
            string.IsNullOrEmpty(endpoint.Fragment) && string.IsNullOrEmpty(endpoint.UserInfo),
            "Supply the dedicated hosted Supabase project URL.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Supply the dedicated project's protected server key.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root, "Run this test from the repository.");

        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(SupabaseOASIS), sync ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(Path.GetDirectoryName(directory)!, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var reportPath = Path.Combine(directory, "report.json");
        var pendingPath = reportPath + ".next";
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(pendingPath),
            "Preserve the previous native report and resolve its fixture before running again.");

        var otherReportPath = Path.Combine(Path.GetDirectoryName(directory)!, sync ? "async" : "sync", "report.json");
        Assert.IsFalse(File.Exists(otherReportPath + ".next"), "Reconcile the other mode's interrupted checkpoint first.");
        if (File.Exists(otherReportPath))
        {
            await using var prior = File.OpenRead(otherReportPath);
            var priorReport = await JsonSerializer.DeserializeAsync<CoreStorageConformance.Report>(prior);
            Assert.IsTrue(priorReport?.FixtureCleanupComplete == true,
                "Reconcile the other mode's fixture before connecting to Supabase.");
        }

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, report);
                file.Flush(flushToDisk: true);
            }
            File.Move(pendingPath, reportPath, overwrite: true);
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var provider = new SupabaseOASIS(url!, key!, http);

        async Task RequireEmptyTables(string phase)
        {
            foreach (var table in new[] { "oasis_avatars", "oasis_avatar_details", "oasis_holons" })
            {
                using var response = await http.GetAsync(new Uri(endpoint!, $"rest/v1/{table}?select=id&limit=1"));
                response.EnsureSuccessStatusCode();
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.IsTrue(document.RootElement.ValueKind == JsonValueKind.Array &&
                    document.RootElement.GetArrayLength() == 0,
                    $"The dedicated Supabase test table {table} must be empty {phase}.");
            }
        }

        await RequireEmptyTables("before dispatch");

        async Task CleanTombstone(Guid id)
        {
            using var response = await http.DeleteAsync(
                new Uri(endpoint!, $"rest/v1/oasis_holons?id=eq.{id:D}&is_deleted=eq.true&select=id"));
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var rows = document.RootElement;
            var removed = rows.ValueKind == JsonValueKind.Array && rows.GetArrayLength() == 1 &&
                rows[0].GetProperty("id").GetGuid() == id;
            Assert.IsTrue(removed, "PostgREST must acknowledge removal of the exact fixture tombstone.");
        }

        async Task CheckpointWithCleanup(CoreStorageConformance.Report report)
        {
            if (report.Succeeded && report.FixtureCleanupComplete)
            {
                report.Succeeded = false;
                report.FixtureCleanupComplete = false;
                foreach (var id in new[] { report.ChildId, report.RootId, report.ImportId }.Concat(report.BatchIds))
                {
                    report.CurrentCase = "supabase-tombstone-cleanup-" + id.ToString("N");
                    await Checkpoint(report);
                    await CleanTombstone(id);
                    report.HardCleanedFixtures.Add(id);
                }
                report.CurrentCase = "supabase-verify-empty-tables";
                await Checkpoint(report);
                await RequireEmptyTables("after cleanup");
                report.CurrentCase = "complete";
                report.FixtureCleanupComplete = true;
                report.Succeeded = true;
            }
            await Checkpoint(report);
        }

        var result = await CoreStorageConformance.RunAsync(
            provider, nameof(SupabaseOASIS), "native", sync, CheckpointWithCleanup,
            expectedDefaultDeletion: "absent");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}

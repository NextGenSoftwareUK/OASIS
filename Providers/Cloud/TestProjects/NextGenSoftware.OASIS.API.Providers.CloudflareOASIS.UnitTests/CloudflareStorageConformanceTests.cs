using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class CloudflareStorageConformanceTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
        ? value : throw new InvalidOperationException("Missing " + name);
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task ShippingProviderPassesTheSharedStorageSuite(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_CLOUDFLARE_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Dedicated owned Cloudflare native namespace opt-in required; no namespace is provisioned by this test.");
        var account = Required("OASIS_CLOUDFLARE_CONFORMANCE_ACCOUNT");
        var space = Required("OASIS_CLOUDFLARE_CONFORMANCE_NAMESPACE");
        var token = Required("OASIS_CLOUDFLARE_CONFORMANCE_TOKEN");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "CloudflareOASIS-native", synchronous ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var path = Path.Combine(directory, "report.json");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".next"), "Preserve previous outcomes before another run.");
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
        var key = Convert.FromBase64String(Required("OASIS_CLOUDFLARE_CONFORMANCE_STORAGE_KEY"));
        Assert.AreEqual(32, key.Length);
        try
        {
            using var provider = new CloudflareOASIS(account, space, token, http, () => key.ToArray());
            var native = provider.NativeBackend;
            const string prefix = "oasis/private/v1/";
            Assert.AreEqual(0, (await native.ListKeysAsync(prefix)).Count, "Dedicated native namespace must be empty before dispatch.");
            var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(path);
            var report = await CoreStorageConformance.RunAsync(provider, "CloudflareOASIS", "native", synchronous, checkpoint.WriteAsync,
                (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
                expectedDefaultDeletion: "soft");
            Assert.IsTrue(report.Succeeded, report.CurrentCase + ": " + report.FailureType);
            // No sleeps or local read cache: native visibility/rate-limit failures must remain visible.
            Assert.AreEqual(0, (await native.ListKeysAsync(prefix)).Count, "Independent native listing must confirm cleanup.");
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}

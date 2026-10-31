using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;

// This is compatible-service evidence, never hosted AWS acceptance.
[TestClass, DoNotParallelize]
public class S3CompatibleStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task SharedMinioStorageInterface(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_MINIO_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Explicit retained MinIO conformance opt-in required.");
        static string Required(string name) => Environment.GetEnvironmentVariable(name) is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Missing {name}.");
        var endpoint = Required("OASIS_MINIO_CONFORMANCE_ENDPOINT");
        var uri = new Uri(endpoint);
        Assert.IsTrue(uri.Scheme == "http" && uri.Host == "127.0.0.1" && uri.AbsolutePath == "/"
            && uri.UserInfo == "" && uri.Query == "" && uri.Fragment == "", "Explicit loopback service only.");
        var bucket = Required("OASIS_MINIO_CONFORMANCE_BUCKET");
        var key = Convert.FromBase64String(Required("OASIS_MINIO_CONFORMANCE_STORAGE_KEY"));
        Assert.AreEqual(32, key.Length, "Reuse one protected 32-byte storage key.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "AWSS3OASIS-MinIO", synchronous ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var path = Path.Combine(directory, "report.json");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".next"), "Preserve prior reports before another run.");
        var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(path);
        using var client = new AmazonS3Client(new BasicAWSCredentials(Required("MINIO_ACCESS_KEY"), Required("MINIO_SECRET_KEY")),
            new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "us-east-1", MaxErrorRetry = 0 });
        // Both modes own the same dedicated bucket sequentially. Refuse an occupied
        // bucket rather than deleting unknown records or adopting someone else's data.
        var before = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 });
        Assert.AreEqual(System.Net.HttpStatusCode.OK, before.HttpStatusCode);
        Assert.AreEqual(0, before.S3Objects.Count, "Dedicated bucket must be empty before dispatch.");
        Assert.IsFalse(before.IsTruncated);
        using var provider = new NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.AWSS3OASIS(client, bucket, () => key, "oasis-conformance/");
        var report = await CoreStorageConformance.RunAsync(provider, "AWSS3OASIS-MinIO", "native", synchronous, checkpoint.WriteAsync, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded, report.CurrentCase + ": " + report.FailureType);
        var after = await client.ListObjectsV2Async(new ListObjectsV2Request { BucketName = bucket, MaxKeys = 1 });
        Assert.AreEqual(System.Net.HttpStatusCode.OK, after.HttpStatusCode);
        Assert.AreEqual(0, after.S3Objects.Count, "Successful fixture cleanup must leave the bucket empty.");
        Assert.IsFalse(after.IsTruncated);
    }
}

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;
using NextGenSoftware.OASIS.Providers.Shared.S3;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass, DoNotParallelize]
public class AwsS3LiveDriverTests
{
    [TestMethod]
    public async Task Journal_region_cannot_disagree_with_sdk_target()
    {
        using var store = new AwsS3EncryptedLifecycleTests.Store("oasis-acceptance/");
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => AwsS3LiveTransportAcceptance.RunAsync(
            store.Client, "us-west-2", "test-bucket", "oasis-acceptance/", "unused"));
        Assert.AreEqual(0, store.Requests);
    }
    [DataTestMethod]
    [DataRow("success")]
    [DataRow("occupied")]
    [DataRow("lost-create")]
    [DataRow("pending")]
    [DataRow("interrupted-snapshot")]
    public async Task Native_entry_point_preserves_ownership_and_never_replays_unknown_writes(string outcome)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "S3Current", "LiveDriverControlled");
        Directory.CreateDirectory(work); // One reusable controlled scope; never the live evidence directory.
        var pending = Path.Combine(work, "s3-live-transport.pending.json");
        var report = Path.Combine(work, "s3-live-transport.current.json");
        var next = pending + ".next";
        foreach (var path in new[] { pending, report, next, report + ".next" }) Assert.IsFalse(File.Exists(path), "Review original fixture before reuse.");
        using var store = new AwsS3EncryptedLifecycleTests.Store("oasis-acceptance/");
        if (outcome == "occupied") store.Objects["oasis-acceptance/transport-probe"] = "foreign-object";
        if (outcome == "lost-create") store.LoseCreateAcknowledgement = true;
        var blocker = outcome == "pending" ? pending : next;
        if (outcome is "pending" or "interrupted-snapshot")
        {
            using (var file = new FileStream(blocker, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes("owned controlled evidence blocker");
                await file.WriteAsync(bytes); file.Flush(true);
            }
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => AwsS3LiveTransportAcceptance.RunAsync(store.Client, "us-east-1", "test-bucket", "oasis-acceptance/", work));
            Assert.AreEqual(0, store.Requests);
            Assert.AreEqual("owned controlled evidence blocker", await File.ReadAllTextAsync(blocker));
            File.Delete(blocker); // Exact verified synthetic file only.
            return;
        }
        if (outcome == "success") await AwsS3LiveTransportAcceptance.RunAsync(store.Client, "us-east-1", "test-bucket", "oasis-acceptance/", work);
        else if (outcome == "occupied") await Assert.ThrowsExceptionAsync<InvalidDataException>(() => AwsS3LiveTransportAcceptance.RunAsync(store.Client, "us-east-1", "test-bucket", "oasis-acceptance/", work));
        else await Assert.ThrowsExceptionAsync<S3MutationReconciliationException>(() => AwsS3LiveTransportAcceptance.RunAsync(store.Client, "us-east-1", "test-bucket", "oasis-acceptance/", work));
        if (outcome == "occupied")
        {
            Assert.AreEqual(2, store.Requests); Assert.AreEqual(0, store.Writes); Assert.AreEqual(0, store.Deletes);
            Assert.AreEqual("foreign-object", store.Objects["oasis-acceptance/transport-probe"]);
            Assert.IsFalse(File.Exists(pending) || File.Exists(report)); return;
        }
        var evidence = outcome == "success" ? report : pending;
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(evidence));
        var data = document.RootElement;
        Assert.AreEqual("test-bucket", data.GetProperty("bucket").GetString());
        Assert.AreEqual("oasis-acceptance/transport-probe", data.GetProperty("objectName").GetString());
        Assert.AreEqual("us-east-1", data.GetProperty("region").GetString());
        Assert.AreEqual(outcome == "success" ? "verified" : "before-create", data.GetProperty("state").GetString());
        if (outcome == "success")
        {
            Assert.AreEqual(12, store.Requests); Assert.AreEqual(0, store.Objects.Count); Assert.AreEqual(1, store.Deletes);
            Assert.IsFalse(File.Exists(pending)); Assert.IsFalse(data.GetProperty("fullProviderVerified").GetBoolean());
        }
        else
        {
            Assert.AreEqual(3, store.Requests); Assert.AreEqual(1, store.Writes); Assert.AreEqual(0, store.Deletes);
            Assert.AreEqual(1, store.Objects.Count); Assert.IsFalse(File.Exists(report));
        }
        File.Delete(evidence); // Verified controlled fixture only; live runner never cleans an error.
    }
}

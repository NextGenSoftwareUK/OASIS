using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.Shared.S3;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass, DoNotParallelize]
public class AwsS3ProviderDriverTests
{
    private static string Work()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var path = Path.Combine(root.FullName, "TestResults", "S3Current", "ProviderDriverControlled");
        Directory.CreateDirectory(path);
        return path;
    }
    private static string[] Evidence(string work) => new[] { "s3-live-records.pending.json", "s3-live-records.current.json",
        "s3-live-records.pending.json.next", "s3-live-records.current.json.next" }.Select(name => Path.Combine(work, name)).ToArray();
    private static void Empty(string work) { foreach (var file in Evidence(work)) Assert.IsFalse(File.Exists(file), "Preserve original controlled evidence."); }

    [TestMethod]
    public async Task RecordTargetRegionMustMatchBeforeAnySdkRequest()
    {
        using var store = new AwsS3EncryptedLifecycleTests.Store(AwsS3LiveProviderAcceptance.Prefix);
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => AwsS3LiveProviderAcceptance.RunAsync(store.Client,
            "us-west-2", "test-bucket", () => throw new InvalidOperationException("Key must not resolve"), "unused"));
        Assert.AreEqual(0, store.Requests);
    }

    [DataTestMethod]
    [DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task ExistingRecordEvidenceFencesSdkAndKeyResolution(int index)
    {
        var work = Work(); Empty(work); var file = Evidence(work)[index];
        const string original = "owned original unresolved S3 record evidence";
        using var store = new AwsS3EncryptedLifecycleTests.Store(AwsS3LiveProviderAcceptance.Prefix);
        await File.WriteAllTextAsync(file, original); var resolutions = 0;
        try
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => AwsS3LiveProviderAcceptance.RunAsync(store.Client,
                "us-east-1", "test-bucket", () => { resolutions++; throw new InvalidOperationException(); }, work));
            Assert.AreEqual(0, resolutions); Assert.AreEqual(0, store.Requests);
        }
        finally { Assert.AreEqual(original, await File.ReadAllTextAsync(file)); File.Delete(file); }
    }

    [DataTestMethod][DataRow(0)][DataRow(31)]
    public async Task MissingOrInvalidPersistedKeyStopsBeforeSdk(int length)
    {
        var work = Work(); Empty(work); var key = length == 0 ? null : Enumerable.Repeat((byte)9, length).ToArray();
        using var store = new AwsS3EncryptedLifecycleTests.Store(AwsS3LiveProviderAcceptance.Prefix);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => AwsS3LiveProviderAcceptance.RunAsync(store.Client,
            "us-east-1", "test-bucket", () => key, work));
        Assert.AreEqual(0, store.Requests); if (key != null) Assert.IsTrue(key.All(value => value == 0)); Empty(work);
    }

    [DataTestMethod]
    [DataRow("success")][DataRow("occupied")][DataRow("lost-create")][DataRow("changed-intent")][DataRow("appeared-intent")]
    public async Task PublicRecordGatePreservesOriginalUncertainOrChangedIntent(string outcome)
    {
        var work = Work(); Empty(work); var pending = Evidence(work)[0]; var report = Evidence(work)[1];
        var key = Enumerable.Repeat((byte)81, 32).ToArray(); byte[] originalIntent = null;
        const string external = "owned synthetic competing marker; preserve exact bytes";
        using var store = new AwsS3EncryptedLifecycleTests.Store(AwsS3LiveProviderAcceptance.Prefix);
        if (outcome == "occupied") store.Objects[AwsS3LiveProviderAcceptance.Prefix + "foreign"] = "not-owned";
        if (outcome == "lost-create") store.LoseCreateAcknowledgement = true;
        if (outcome == "changed-intent") store.AfterFirstWrite = () => { originalIntent = File.ReadAllBytes(pending); File.WriteAllText(pending, external); };
        if (outcome == "appeared-intent") store.BeforeListReply = () => File.WriteAllText(pending, external);
        try
        {
            if (outcome == "success")
            {
                await AwsS3LiveProviderAcceptance.RunAsync(store.Client, "us-east-1", "test-bucket", () => key.ToArray(), work);
                using var proof = JsonDocument.Parse(await File.ReadAllBytesAsync(report));
                Assert.AreEqual("verified", proof.RootElement.GetProperty("state").GetString());
                Assert.AreEqual("us-east-1", proof.RootElement.GetProperty("region").GetString());
                Assert.AreEqual(9, proof.RootElement.GetProperty("acknowledgedMutations").GetInt32());
                foreach (var flag in new[] { "fullProviderVerified", "hyperDriveVerified", "cloudCostMeasured", "sdkRequestsMeasured", "bucketVersionsVerified" })
                    Assert.IsFalse(proof.RootElement.GetProperty(flag).GetBoolean());
                Assert.IsTrue(proof.RootElement.GetProperty("retainedVersionsMayIncurCharges").GetBoolean());
                Assert.AreEqual(0, store.Objects.Count); Assert.IsFalse(File.Exists(pending)); File.Delete(report); return;
            }
            await Assert.ThrowsExceptionAsync<InvalidDataException>(() => AwsS3LiveProviderAcceptance.RunAsync(store.Client,
                "us-east-1", "test-bucket", () => key.ToArray(), work));
            Assert.IsFalse(File.Exists(report)); Assert.AreEqual(0, store.Deletes);
            if (outcome == "occupied") { Assert.AreEqual(0, store.Writes); Assert.AreEqual("not-owned", store.Objects.Single().Value); Empty(work); return; }
            var markerBytes = await File.ReadAllBytesAsync(pending);
            if (outcome is "changed-intent" or "appeared-intent") Assert.AreEqual(external, await File.ReadAllTextAsync(pending));
            if (outcome == "appeared-intent") { Assert.AreEqual(0, store.Writes); Assert.AreEqual(0, store.Objects.Count); }
            else
            {
                using var original = JsonDocument.Parse(originalIntent ?? markerBytes);
                var id = original.RootElement.GetProperty("avatarId").GetGuid();
                var native = new S3ObjectBackend(store.Client, "test-bucket", AwsS3LiveProviderAcceptance.Prefix);
                using var privateRead = PersistedPrivateKeyValueBackend.Create(native, () => key.ToArray());
                var raw = await privateRead.GetAsync(AwsS3LiveProviderAcceptance.Prefix + "avatar/" + id.ToString("N"));
                using var record = JsonDocument.Parse(raw); Assert.AreEqual(id, record.RootElement.GetProperty("Id").GetGuid());
                // TEST-only reconciliation after observing the original synthetic record; driver never error-cleans.
                foreach (var objectKey in (await native.ListKeysAsync("")).ToArray())
                { var observed = await native.ReadObservedAsync(objectKey); await native.DeleteUnchangedAsync(objectKey, observed.Revision); }
                Assert.AreEqual(0, store.Objects.Count);
                if (outcome == "lost-create") Assert.AreEqual(1, store.Writes);
            }
            CollectionAssert.AreEqual(markerBytes, await File.ReadAllBytesAsync(pending)); File.Delete(pending); Empty(work);
        }
        finally { CryptographicOperations.ZeroMemory(key); }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class HolonMigrationBarrierPlanPersistenceTests
{
    private sealed class Files : IDisposable
    {
        public readonly string Folder, Final;
        public bool Complete;
        public Files(Guid id)
        {
            var root = new DirectoryInfo(AppContext.BaseDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1"))) root = root.Parent;
            Assert.IsNotNull(root);
            Folder = Path.Combine(root.FullName, "TestResults/BarrierPlanCurrent");
            Directory.CreateDirectory(Folder);
            Final = Path.Combine(Folder, id.ToString("D") + ".barrier-plan.json");
            Assert.IsFalse(File.Exists(Final)); Assert.IsFalse(File.Exists(Final + ".pending"));
        }
        public void Dispose()
        {
            if (!Complete) return; // Preserve failure evidence for exact review.
            foreach (var path in new[] { Final, Final + ".pending", Final + ".lock" })
            {
                Assert.AreEqual(Path.GetFullPath(Folder), Path.GetDirectoryName(Path.GetFullPath(path)));
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
    private static async Task<HolonReplicationMigrationBarrierCoordinatorTests.Setup> Setup()
    { var f = new HolonReplicationMigrationBarrierCoordinatorTests.Setup(); await f.Initialize(); return f; }

    [TestMethod]
    public async Task RedactedPlanRestoresExactScopesWithoutTheOriginalPreviewObject()
    {
        var f = await Setup(); var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var restored = HolonReplicationMigrationBarrierPlan.ReadCanonical(plan.CanonicalJson, f.Context);
        CollectionAssert.AreEqual(plan.Scopes.Select(s => s.Fingerprint).ToArray(), restored.Scopes.Select(s => s.Fingerprint).ToArray());
        Assert.AreEqual(plan.Fingerprint, restored.Fingerprint);
        Assert.IsFalse(plan.CanonicalJson.Contains("private-business-value", StringComparison.Ordinal));
        f.Preview = null;
        var acquired = await HolonReplicationMigrationBarrierCoordinator.AcquireAsync(restored, f.Permission, f.Participants);
        Assert.IsFalse(acquired.IsError, acquired.Message); Assert.IsTrue(acquired.Result.AllBarriersHeld);
    }

    [DataTestMethod]
    [DataRow("preview")] [DataRow("record")] [DataRow("context")] [DataRow("extra")] [DataRow("whitespace")] [DataRow("duplicate")]
    public async Task AlteredRecoveryEvidenceCannotChangeApprovedRecordOrRouteScope(string change)
    {
        var f = await Setup(); var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var node = JsonNode.Parse(plan.CanonicalJson)!;
        if (change == "preview") node["previewMaterial"] = node["previewMaterial"]!.GetValue<string>().Replace("legacy-source", "changed-source");
        if (change == "record")
        {
            var summary = JsonNode.Parse(node["receiptMaterials"]![0]!.GetValue<string>())!;
            summary["Records"]![0]!["Original"]!["HolonId"] = Guid.NewGuid().ToString("D");
            node["receiptMaterials"]![0] = summary.ToJsonString();
        }
        if (change == "context") node["contextDigest"] = new string('F', 64);
        if (change == "extra") node["extra"] = true;
        var json = change == "whitespace" ? plan.CanonicalJson + "\n" : node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = false });
        if (change == "duplicate") json = "{\"format\":\"cosmic-migration-barrier-plan-v1\"," + plan.CanonicalJson[1..];
        Assert.ThrowsException<InvalidDataException>(() => HolonReplicationMigrationBarrierPlan.ReadCanonical(json, f.Context));
        Assert.IsTrue(f.Providers.All(p => p.Reads == 0 && p.Acquires == 0));
    }

    [TestMethod]
    public async Task SavePrecedesNativeAcquisitionAndNewStoreResumesOnlyOriginalEvidence()
    {
        var f = await Setup(); using var files = new Files(f.Context.MigrationId);
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        f.Providers[0].AfterAcquire = () => Assert.AreEqual(plan.CanonicalJson, File.ReadAllText(files.Final));
        var result = await HolonReplicationMigrationBarrierCoordinator.AcquirePersistedAsync(plan,
            new(files.Folder), f.Permission, f.Participants);
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        f.Preview = null;
        var resumed = await HolonReplicationMigrationBarrierCoordinator.ResumePersistedAsync(f.Context,
            new(files.Folder), f.Permission, f.Participants);
        Assert.IsFalse(resumed.IsError); Assert.IsTrue(resumed.Result.AllBarriersHeld); Assert.IsFalse(resumed.IsSaved);
        Assert.IsTrue(f.Providers.All(p => p.Acquires == 1));
        var repeated = await new FileHolonMigrationBarrierPlanStore(files.Folder).SaveAsync(plan);
        Assert.IsFalse(repeated.IsError); Assert.IsFalse(repeated.IsSaved);
        files.Complete = true;
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task CorruptOrMissingHostEvidenceNeverTriggersNativeRecovery(bool corrupt)
    {
        var f = await Setup(); using var files = new Files(f.Context.MigrationId);
        if (corrupt) await File.WriteAllTextAsync(files.Final, "{broken");
        var result = await HolonReplicationMigrationBarrierCoordinator.ResumePersistedAsync(f.Context,
            new(files.Folder), f.Permission, f.Participants);
        Assert.IsTrue(result.IsError); Assert.IsTrue(f.Providers.All(p => p.Reads == 0 && p.Acquires == 0));
        if (corrupt) Assert.AreEqual("{broken", await File.ReadAllTextAsync(files.Final));
        files.Complete = true;
    }

    [TestMethod]
    public async Task CompletePendingEvidenceNeedsExplicitRecoveryAndTruncatedEvidenceIsPreserved()
    {
        var f = await Setup(); using var files = new Files(f.Context.MigrationId);
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var store = new FileHolonMigrationBarrierPlanStore(files.Folder);
        await File.WriteAllTextAsync(files.Final + ".pending", plan.CanonicalJson[..20]);
        Assert.IsTrue((await store.RecoverPendingAsync(f.Context)).IsError);
        Assert.AreEqual(plan.CanonicalJson[..20], await File.ReadAllTextAsync(files.Final + ".pending"));
        // Controlled replacement of this test-owned truncated fixture only.
        await File.WriteAllTextAsync(files.Final + ".pending", plan.CanonicalJson);
        Assert.IsTrue((await store.ReadAsync(f.Context)).IsError);
        Assert.IsTrue((await store.SaveAsync(plan)).IsError);
        var recovered = await store.RecoverPendingAsync(f.Context);
        Assert.IsFalse(recovered.IsError, recovered.Message + recovered.Exception); Assert.IsTrue(recovered.IsSaved);
        Assert.AreEqual(plan.Fingerprint, recovered.Result.Fingerprint);
        Assert.IsFalse(File.Exists(files.Final + ".pending"));
        files.Complete = true;
    }

    [TestMethod]
    public async Task ExistingEvidenceCannotBeReplacedByAnotherReviewedScopeWithSameMigrationIdentity()
    {
        var f = await Setup(); using var files = new Files(f.Context.MigrationId);
        var original = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var store = new FileHolonMigrationBarrierPlanStore(files.Folder);
        Assert.IsFalse((await store.SaveAsync(original)).IsError);
        var other = await Setup();
        var conflictingContext = other.Preview.AuthorizationContext(f.Context.MigrationId, f.Context.OperatorId, f.Context.AuthorizationPolicyBinding);
        var conflicting = HolonReplicationMigrationBarrierPlan.Create(other.Preview, conflictingContext);
        Assert.IsTrue((await store.SaveAsync(conflicting)).IsError);
        Assert.AreEqual(original.CanonicalJson, await File.ReadAllTextAsync(files.Final));
        Assert.IsFalse(File.Exists(files.Final + ".pending"));
        files.Complete = true;
    }

    [TestMethod]
    public async Task ConcurrentWriterOrDeniedPolicyCannotOverwriteOrStartNativeAcquisition()
    {
        var f = await Setup(); using var files = new Files(f.Context.MigrationId);
        var plan = HolonReplicationMigrationBarrierPlan.Create(f.Preview, f.Context);
        var store = new FileHolonMigrationBarrierPlanStore(files.Folder);
        using (var held = new FileStream(files.Final + ".lock", FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None))
        {
            var denied = await HolonReplicationMigrationBarrierCoordinator.AcquirePersistedAsync(plan, store, f.Permission, f.Participants);
            Assert.IsTrue(denied.IsError); Assert.IsFalse(File.Exists(files.Final));
            Assert.IsTrue(f.Providers.All(p => p.Reads == 0));
        }
        f.Permission.Deny = true;
        Assert.IsTrue((await HolonReplicationMigrationBarrierCoordinator.AcquirePersistedAsync(plan, store, f.Permission, f.Participants)).IsError);
        Assert.IsFalse(File.Exists(files.Final)); Assert.IsTrue(f.Providers.All(p => p.Reads == 0));
        files.Complete = true;
    }
}

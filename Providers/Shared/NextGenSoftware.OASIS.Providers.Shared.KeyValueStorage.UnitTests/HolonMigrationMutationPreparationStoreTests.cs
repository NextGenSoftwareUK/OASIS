using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
[DoNotParallelize]
public class HolonMigrationMutationPreparationStoreTests
{
    private string folder;
    [TestInitialize]
    public void Initialize()
    {
        folder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../..", "TestResults/MutationPreparationCurrent/store-fixture"));
        if (Directory.Exists(folder)) Assert.AreEqual(0, Directory.GetFileSystemEntries(folder).Length,
            "Preserve previous failed fixture evidence for review.");
        Directory.CreateDirectory(folder);
    }

    private void CleanVerifiedFixture()
    {
        foreach (var file in Directory.GetFiles(folder)) File.Delete(file);
        Assert.AreEqual(0, Directory.GetFileSystemEntries(folder).Length);
    }

    [TestMethod]
    public async Task RecreationLoadsExactEvidenceAndNeverReplacesOriginal()
    {
        var (plan, preparation) = await HolonMigrationMutationAuthorizationTests.PrepareWithPlan();
        var store = new FileHolonMigrationMutationPreparationStore(folder);
        var saved = await store.SaveAsync(preparation, plan, preparation.Fingerprint);
        Assert.IsFalse(saved.IsError, saved.Message + saved.Exception); Assert.IsTrue(saved.IsSaved);
        var restored = await new FileHolonMigrationMutationPreparationStore(folder).ReadAsync(plan, preparation.Fingerprint);
        Assert.IsFalse(restored.IsError, restored.Exception?.ToString());
        Assert.AreEqual(preparation.CanonicalJson, restored.Result.CanonicalJson);
        Assert.IsFalse((await store.SaveAsync(preparation, plan, preparation.Fingerprint)).IsSaved);
        Assert.IsTrue((await store.ReadAsync(plan, new string('A',64))).IsError);
        var path = Path.Combine(folder, plan.Context.MigrationId + ".mutation-preparation.json");
        Assert.AreEqual(preparation.CanonicalJson, await File.ReadAllTextAsync(path));
        CleanVerifiedFixture();
    }

    [TestMethod]
    public async Task CompletePendingEvidenceRequiresExplicitRecovery()
    {
        var (plan, preparation) = await HolonMigrationMutationAuthorizationTests.PrepareWithPlan();
        var path = Path.Combine(folder, plan.Context.MigrationId + ".mutation-preparation.json");
        await File.WriteAllTextAsync(path + ".pending", preparation.CanonicalJson);
        var store = new FileHolonMigrationMutationPreparationStore(folder);
        Assert.IsTrue((await store.ReadAsync(plan, preparation.Fingerprint)).IsError);
        Assert.IsTrue((await store.SaveAsync(preparation, plan, preparation.Fingerprint)).IsError);
        var recovered = await store.RecoverPendingAsync(plan, preparation.Fingerprint);
        Assert.IsFalse(recovered.IsError, recovered.Exception?.ToString());
        Assert.AreEqual(preparation.Fingerprint, recovered.Result.Fingerprint);
        Assert.IsFalse(File.Exists(path + ".pending"));
        CleanVerifiedFixture();
    }

    [TestMethod]
    public async Task CorruptionAndAmbiguousPendingEvidenceArePreserved()
    {
        var (plan, preparation) = await HolonMigrationMutationAuthorizationTests.PrepareWithPlan();
        var path = Path.Combine(folder, plan.Context.MigrationId + ".mutation-preparation.json");
        var store = new FileHolonMigrationMutationPreparationStore(folder);
        await File.WriteAllTextAsync(path + ".pending", "{incomplete");
        Assert.IsTrue((await store.RecoverPendingAsync(plan, preparation.Fingerprint)).IsError);
        Assert.AreEqual("{incomplete", await File.ReadAllTextAsync(path + ".pending"));
        await File.WriteAllTextAsync(path, preparation.CanonicalJson);
        Assert.IsTrue((await store.RecoverPendingAsync(plan, preparation.Fingerprint)).IsError);
        Assert.AreEqual(preparation.CanonicalJson, await File.ReadAllTextAsync(path));
        CleanVerifiedFixture();
    }

    [TestMethod]
    public async Task CanonicalRestorationRejectsChangedEvidenceDuplicateKeysAndOtherPlan()
    {
        var (plan, preparation) = await HolonMigrationMutationAuthorizationTests.PrepareWithPlan();
        var (otherPlan, _) = await HolonMigrationMutationAuthorizationTests.PrepareWithPlan();
        Assert.ThrowsException<InvalidDataException>(() => HolonMigrationMutationPreparation.ReadCanonical(
            preparation.CanonicalJson + " ", plan, preparation.Fingerprint));
        Assert.ThrowsException<InvalidDataException>(() => HolonMigrationMutationPreparation.ReadCanonical(
            preparation.CanonicalJson.Insert(1, "\"format\":\"duplicate\","), plan, preparation.Fingerprint));
        Assert.ThrowsException<InvalidDataException>(() => HolonMigrationMutationPreparation.ReadCanonical(
            preparation.CanonicalJson, otherPlan, preparation.Fingerprint));
        CleanVerifiedFixture();
    }
}

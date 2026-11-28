using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Providers.TestSupport;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.TelosOASIS.IntegrationTests
{
    [TestClass]
    public class TelosOASISIntegrationTests
    {
        private static TelosOASIS CreateProvider()
        {
            string endpoint = Require("OASIS_ANTELOPE_ENDPOINT");
            string account = Require("OASIS_ANTELOPE_ACCOUNT");
            string chainId = Require("OASIS_ANTELOPE_CHAIN_ID");
            string privateKey = Require("OASIS_ANTELOPE_PRIVATE_KEY");
            return new TelosOASIS(endpoint, account, chainId, privateKey);
        }

        [TestMethod]
        public async Task AvatarCrud_PersistsThroughTelosProviderOnOfficialAntelopeNode()
        {
            TelosOASIS provider = CreateProvider();
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var avatar = new Avatar
            {
                Id = Guid.NewGuid(),
                Username = $"telos-{Guid.NewGuid():N}",
                Email = $"telos-{Guid.NewGuid():N}@oasis.test",
                FirstName = "Before"
            };

            var created = await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(created.IsError, created.Message);
            var loaded = await provider.LoadAvatarAsync(avatar.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("Before", loaded.Result.FirstName);

            avatar.FirstName = "After";
            var updated = await provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(updated.IsError, updated.Message);
            loaded = await provider.LoadAvatarAsync(avatar.Id);
            Assert.AreEqual("After", loaded.Result.FirstName);

            var deleted = await provider.DeleteAvatarAsync(avatar.Id, false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }

        [TestMethod]
        public async Task HolonCrud_PersistsThroughTelosProviderOnOfficialAntelopeNode()
        {
            TelosOASIS provider = CreateProvider();
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);

            var holon = new Holon { Id = Guid.NewGuid(), Name = "Before" };
            var created = await provider.SaveHolonAsync(holon);
            Assert.IsFalse(created.IsError, created.Message);
            var loaded = await provider.LoadHolonAsync(holon.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("Before", loaded.Result.Name);

            holon.Name = "After";
            var updated = await provider.SaveHolonAsync(holon);
            Assert.IsFalse(updated.IsError, updated.Message);
            loaded = await provider.LoadHolonAsync(holon.Id);
            Assert.AreEqual("After", loaded.Result.Name);

            var deleted = await provider.DeleteHolonAsync(holon.Id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }

        [TestMethod]
        public async Task CoreStorageConformanceOnOfficialAntelope()
        {
            var mode = Environment.GetEnvironmentVariable("OASIS_ANTELOPE_STORAGE_MODE");
            if (mode is not ("sync" or "async"))
                Assert.Inconclusive("Select a storage mode on the disposable Antelope fixture.");
            if (Environment.GetEnvironmentVariable("OASIS_ANTELOPE_LIFECYCLE_WRITES") != "1")
                Assert.Inconclusive("Enable writes only for the dedicated disposable Antelope contract.");
            Assert.AreEqual("http://127.0.0.1:18888", Environment.GetEnvironmentVariable("OASIS_ANTELOPE_ENDPOINT"));

            TelosOASIS provider = CreateProvider();
            var directory = Require("OASIS_TELOS_RESULTS");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "core-storage.json");
            Assert.IsFalse(File.Exists(path));
            var report = await CoreStorageConformance.RunAsync(provider, nameof(TelosOASIS), "native",
                mode == "sync", async checkpoint =>
                {
                    await File.WriteAllTextAsync(path + ".next", JsonSerializer.Serialize(checkpoint));
                    File.Move(path + ".next", path, overwrite: true);
                }, expectedDefaultDeletion: "absent");
            Assert.IsTrue(report.Succeeded,
                $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
            Assert.IsTrue(report.FixtureCleanupComplete);
            Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
            await DelegatedStorageIdentityConformance.RunAsync(provider,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.TelosOASIS,
                NextGenSoftware.OASIS.API.Core.Enums.ProviderType.EOSIOOASIS, mode == "sync");
        }

        private static string Require(string name) =>
            Environment.GetEnvironmentVariable(name) ??
            throw new AssertInconclusiveException($"Set {name} to run the official Antelope integration tests.");
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;

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

        private static string Require(string name) =>
            Environment.GetEnvironmentVariable(name) ??
            throw new AssertInconclusiveException($"Set {name} to run the official Antelope integration tests.");
    }
}

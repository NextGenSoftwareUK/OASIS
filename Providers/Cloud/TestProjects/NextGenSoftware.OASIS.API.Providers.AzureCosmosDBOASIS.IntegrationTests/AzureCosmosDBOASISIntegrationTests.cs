using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests
{
    [TestClass]
    public class AzureCosmosDBOASISIntegrationTests
    {
        private AzureCosmosDBOASIS _provider = null!;

        [TestInitialize]
        public async Task Setup()
        {
            string endpoint = Environment.GetEnvironmentVariable("OASIS_AZURE_COSMOS_ENDPOINT")
                ?? "https://127.0.0.1:8081";
            string key = Environment.GetEnvironmentVariable("OASIS_AZURE_COSMOS_KEY")
                ?? throw new AssertInconclusiveException("OASIS_AZURE_COSMOS_KEY must identify a real Cosmos account or emulator.");
            string database = Environment.GetEnvironmentVariable("OASIS_AZURE_COSMOS_DATABASE")
                ?? $"oasis-integration-{Guid.NewGuid():N}";

            _provider = new AzureCosmosDBOASIS(
                new Uri(endpoint),
                key,
                database,
                new List<string> { "avatarItems", "avatarDetailItems", "holonItems" });

            var activation = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            Assert.IsTrue(activation.Result, activation.Message);
        }

        [TestMethod]
        public async Task AvatarCrud_PersistsThroughOfficialCosmosSdk()
        {
            var avatar = new Avatar
            {
                Username = $"cosmos-{Guid.NewGuid():N}",
                Email = $"cosmos-{Guid.NewGuid():N}@example.test",
                FirstName = "Azure",
                LastName = "Cosmos"
            };

            var saved = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsNotNull(saved.Result);

            var loaded = await _provider.LoadAvatarAsync(saved.Result.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(avatar.Username, loaded.Result.Username);

            loaded.Result.FirstName = "Updated";
            var updated = await _provider.SaveAvatarAsync(loaded.Result);
            Assert.IsFalse(updated.IsError, updated.Message);
            Assert.AreEqual(saved.Result.Id, updated.Result.Id);

            var byEmail = await _provider.LoadAvatarByEmailAsync(avatar.Email);
            Assert.IsFalse(byEmail.IsError, byEmail.Message);
            Assert.AreEqual("Updated", byEmail.Result.FirstName);

            var byUsername = await _provider.LoadAvatarByUsernameAsync(avatar.Username);
            Assert.IsFalse(byUsername.IsError, byUsername.Message);
            Assert.AreEqual(saved.Result.Id, byUsername.Result.Id);

            var deleted = await _provider.DeleteAvatarAsync(saved.Result.Id, false);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            Assert.IsTrue(deleted.Result, deleted.Message);
        }

        [TestMethod]
        public async Task HolonCrud_PersistsThroughOfficialCosmosSdk()
        {
            var holon = new Holon
            {
                Name = $"cosmos-holon-{Guid.NewGuid():N}",
                Description = "Official Cosmos SDK integration verification"
            };

            var saved = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);
            Assert.IsNotNull(saved.Result);

            var loaded = await _provider.LoadHolonAsync(saved.Result.Id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(holon.Name, loaded.Result.Name);

            var deleted = await _provider.DeleteHolonAsync(saved.Result.Id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _provider?.DeActivateProvider();
        }
    }
}

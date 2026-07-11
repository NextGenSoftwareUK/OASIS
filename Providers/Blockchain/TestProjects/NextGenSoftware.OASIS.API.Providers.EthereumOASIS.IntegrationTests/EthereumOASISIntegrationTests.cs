using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests
{
    [TestClass]
    public class EthereumOASISIntegrationTests
    {
        private EthereumOASIS _provider;
        private const string TestHost = "https://mainnet.infura.io/v3/test";
        private const string TestKey = "0x0000000000000000000000000000000000000000000000000000000000000001";
        private static readonly BigInteger TestChainId = 1;
        private const string TestContract = "0x0000000000000000000000000000000000000000";

        [TestInitialize]
        public void Setup()
        {
            _provider = new EthereumOASIS(TestHost, TestKey, TestChainId, TestContract);
        }

        [TestMethod]
        public async Task FullProviderLifecycle_ShouldWork()
        {
            // Activate Provider
            var activateResult = await _provider.ActivateProviderAsync();
            Assert.IsTrue(activateResult.IsError, "Provider should activate successfully");

            // Test Avatar Operations
            var avatar = new Avatar
            {
                Username = "integrationtest",
                Email = "integration@test.com",
                FirstName = "Integration",
                LastName = "Test"
            };

            var saveAvatarResult = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saveAvatarResult.IsError, "SaveAvatar should return not supported");
            Assert.IsTrue(saveAvatarResult.Message.Contains("not supported yet by Ethereum provider"));

            var loadAvatarResult = await _provider.LoadAvatarByEmailAsync("integration@test.com");
            Assert.IsFalse(loadAvatarResult.IsError, "LoadAvatar should return not supported");
            Assert.IsTrue(loadAvatarResult.Message.Contains("not supported yet by Ethereum provider"));

            // Test Holon Operations
            var holon = new Holon
            {
                Name = "Integration Test Holon",
                Description = "Integration Test Description"
            };

            var saveHolonResult = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saveHolonResult.IsError, "SaveHolon should return not supported");
            Assert.IsTrue(saveHolonResult.Message.Contains("not supported yet by Ethereum provider"));

            var loadHolonResult = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsFalse(loadHolonResult.IsError, "LoadHolon should return not supported");
            Assert.IsTrue(loadHolonResult.Message.Contains("not supported yet by Ethereum provider"));

            // Test NFT Operations
            var mintRequest = new MintWeb3NFTRequest();
            var mintResult = await _provider.MintNFTAsync(mintRequest);
            Assert.IsFalse(mintResult.IsError, "MintNFT should return not supported");
            Assert.IsTrue(mintResult.Message.Contains("not supported yet by Ethereum provider"));

            var loadNFTResult = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsTrue(loadNFTResult.IsError, "LoadHolon for new guid should error");

            // Test Transaction Operations
            var sendTransactionResult = await _provider.SendTransactionAsync("0x0", "0x0", 0m, "test");
            Assert.IsTrue(sendTransactionResult.IsError);
            Assert.IsTrue(sendTransactionResult.Message.Contains("not supported yet by Ethereum provider"));

            // Test Search Operations
            var searchParams = new SearchParams();
            var searchResult = _provider.Search(searchParams);
            Assert.IsTrue(searchResult.IsError);
            Assert.IsTrue(searchResult.Message.Contains("not supported yet by Ethereum provider"));

            // Test Import/Export Operations
            var holons = new List<IHolon>();
            var importResult = await _provider.ImportAsync(holons);
            Assert.IsTrue(importResult.IsError);
            Assert.IsTrue(importResult.Message.Contains("not supported yet by Ethereum provider"));

            var exportResult = await _provider.ExportAllAsync();
            Assert.IsTrue(exportResult.IsError);
            Assert.IsTrue(exportResult.Message.Contains("not supported yet by Ethereum provider"));

            // Deactivate Provider
            var deactivateResult = await _provider.DeActivateProviderAsync();
            Assert.IsFalse(deactivateResult.IsError, "Provider should deactivate successfully");
        }

        [TestMethod]
        public async Task ProviderConfiguration_ShouldBeCorrect()
        {
            // Test Provider Type
            Assert.AreEqual(ProviderType.EthereumOASIS, _provider.ProviderType.Value);
            Assert.AreEqual(ProviderCategory.Blockchain, _provider.ProviderCategory.Value);

            // Test Provider Status
            var activateResult = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activateResult.IsError);
            Assert.IsTrue(_provider.IsProviderActivated);

            var deactivateResult = await _provider.DeActivateProviderAsync();
            Assert.IsFalse(deactivateResult.IsError);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task ErrorHandling_ShouldBeConsistent()
        {
            // Test that all methods return consistent error messages
            var avatar = new Avatar();
            var saveAvatarResult = await _provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(saveAvatarResult.IsError);
            Assert.IsTrue(saveAvatarResult.Message.Contains("not supported yet by Ethereum provider"));

            var holon = new Holon();
            var saveHolonResult = await _provider.SaveHolonAsync(holon);
            Assert.IsTrue(saveHolonResult.IsError);
            Assert.IsTrue(saveHolonResult.Message.Contains("not supported yet by Ethereum provider"));

            var sendTransactionResult = await _provider.SendTransactionAsync("0x0", "0x0", 0m, "test");
            Assert.IsTrue(sendTransactionResult.IsError);
            Assert.IsTrue(sendTransactionResult.Message.Contains("not supported yet by Ethereum provider"));

            var nftTransaction = new SendWeb3NFTRequest();
            var sendNFTResult = await _provider.SendNFTAsync(nftTransaction);
            Assert.IsTrue(sendNFTResult.IsError);
            Assert.IsTrue(sendNFTResult.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task AsyncOperations_ShouldComplete()
        {
            // Test that async operations complete without hanging
            var tasks = new List<Task>
            {
                _provider.LoadAllAvatarsAsync(),
                _provider.LoadAllHolonsAsync(),
                _provider.LoadAllAvatarsAsync(),
                _provider.LoadAllHolonsAsync(),
                _provider.ExportAllAsync(),
                _provider.ImportAsync(new List<IHolon>())
            };

            await Task.WhenAll(tasks);
            Assert.AreEqual(6, tasks.Count);
        }

        [TestMethod]
        public async Task ProviderActivation_ShouldBeIdempotent()
        {
            // Activate multiple times
            var result1 = await _provider.ActivateProviderAsync();
            var result2 = await _provider.ActivateProviderAsync();
            var result3 = await _provider.ActivateProviderAsync();

            // All should succeed
            Assert.IsFalse(result1.IsError);
            Assert.IsFalse(result2.IsError);
            Assert.IsFalse(result3.IsError);

            // Deactivate multiple times
            var deactivateResult1 = await _provider.DeActivateProviderAsync();
            var deactivateResult2 = await _provider.DeActivateProviderAsync();

            // All should succeed
            Assert.IsFalse(deactivateResult1.IsError);
            Assert.IsFalse(deactivateResult2.IsError);
        }

        [TestMethod]
        public async Task ConcurrentOperations_ShouldNotThrow()
        {
            // Test concurrent operations
            var tasks = new List<Task>
            {
                Task.Run(async () => await _provider.LoadAvatarAsync(Guid.NewGuid())),
                Task.Run(async () => await _provider.LoadHolonAsync(Guid.NewGuid())),
                Task.Run(async () => await _provider.LoadHolonAsync(Guid.NewGuid())),
                Task.Run(async () => await _provider.LoadAllAvatarsAsync()),
                Task.Run(async () => await _provider.LoadAllHolonsAsync())
            };

            // Should not throw exceptions
            await Task.WhenAll(tasks);
        }

        [TestMethod]
        public async Task ProviderMetadata_ShouldBeCorrect()
        {
            // Test provider metadata
            Assert.AreEqual("EthereumOASIS", _provider.ProviderName);
            Assert.AreEqual(ProviderType.EthereumOASIS, _provider.ProviderType.Value);
            Assert.AreEqual(ProviderCategory.Blockchain, _provider.ProviderCategory.Value);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }
    }
}

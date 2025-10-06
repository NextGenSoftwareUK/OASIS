using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.PolygonOASIS;

namespace NextGenSoftware.OASIS.API.Providers.PolygonOASIS.IntegrationTests
{
    [TestClass]
    public class PolygonOASISIntegrationTests
    {
        private PolygonOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new PolygonOASIS();
        }

        [TestMethod]
        public async Task FullProviderLifecycle_ShouldWork()
        {
            // Activate Provider
            var activateResult = await _provider.ActivateProviderAsync();
            Assert.IsTrue(activateResult.IsSuccess, "Provider should activate successfully");

            // Test Avatar Operations
            var avatar = new Avatar
            {
                Username = "integrationtest",
                Email = "integration@test.com",
                FirstName = "Integration",
                LastName = "Test"
            };

            var saveAvatarResult = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saveAvatarResult.IsSuccess, "SaveAvatar should return not supported");
            Assert.IsTrue(saveAvatarResult.Message.Contains("not supported yet by Polygon provider"));

            var loadAvatarResult = await _provider.LoadAvatarByEmailAsync("integration@test.com");
            Assert.IsFalse(loadAvatarResult.IsSuccess, "LoadAvatar should return not supported");
            Assert.IsTrue(loadAvatarResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Holon Operations
            var holon = new Holon
            {
                Name = "Integration Test Holon",
                Description = "Integration Test Description"
            };

            var saveHolonResult = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saveHolonResult.IsSuccess, "SaveHolon should return not supported");
            Assert.IsTrue(saveHolonResult.Message.Contains("not supported yet by Polygon provider"));

            var loadHolonResult = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsFalse(loadHolonResult.IsSuccess, "LoadHolon should return not supported");
            Assert.IsTrue(loadHolonResult.Message.Contains("not supported yet by Polygon provider"));

            // Test NFT Operations
            var mintRequest = new MintNFTTransactionRequest();
            var mintResult = await _provider.MintNFTAsync(mintRequest);
            Assert.IsFalse(mintResult.IsSuccess, "MintNFT should return not supported");
            Assert.IsTrue(mintResult.Message.Contains("not supported yet by Polygon provider"));

            var loadNFTResult = await _provider.LoadNFTAsync(Guid.NewGuid());
            Assert.IsFalse(loadNFTResult.IsSuccess, "LoadNFT should return not supported");
            Assert.IsTrue(loadNFTResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Transaction Operations
            var transaction = new WalletTransactionRequest();
            var sendTransactionResult = await _provider.SendTransactionAsync(transaction);
            Assert.IsFalse(sendTransactionResult.IsSuccess, "SendTransaction should return not supported");
            Assert.IsTrue(sendTransactionResult.Message.Contains("not supported yet by Polygon provider"));

            // Test GeoNFT Operations
            var placeRequest = new PlaceGeoSpatialNFTRequest();
            var placeGeoNFTResult = await _provider.PlaceGeoNFTAsync(placeRequest);
            Assert.IsFalse(placeGeoNFTResult.IsSuccess, "PlaceGeoNFT should return not supported");
            Assert.IsTrue(placeGeoNFTResult.Message.Contains("not supported yet by Polygon provider"));

            var loadGeoNFTsResult = await _provider.LoadAllGeoNFTsForAvatarAsync(Guid.NewGuid());
            Assert.IsFalse(loadGeoNFTsResult.IsSuccess, "LoadAllGeoNFTsForAvatar should return not supported");
            Assert.IsTrue(loadGeoNFTsResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Search Operations
            var searchParams = new SearchParams();
            var searchResult = _provider.Search(searchParams);
            Assert.IsFalse(searchResult.IsSuccess, "Search should return not supported");
            Assert.IsTrue(searchResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Import/Export Operations
            var holons = new List<IHolon>();
            var importResult = await _provider.ImportAsync(holons);
            Assert.IsFalse(importResult.IsSuccess, "Import should return not supported");
            Assert.IsTrue(importResult.Message.Contains("not supported yet by Polygon provider"));

            var exportResult = await _provider.ExportAllAsync();
            Assert.IsFalse(exportResult.IsSuccess, "ExportAll should return not supported");
            Assert.IsTrue(exportResult.Message.Contains("not supported yet by Polygon provider"));

            // Deactivate Provider
            var deactivateResult = await _provider.DeActivateProviderAsync();
            Assert.IsTrue(deactivateResult.IsSuccess, "Provider should deactivate successfully");
        }

        [TestMethod]
        public async Task ProviderConfiguration_ShouldBeCorrect()
        {
            // Test Provider Type
            Assert.AreEqual(ProviderType.PolygonOASIS, _provider.ProviderType);
            Assert.AreEqual(ProviderCategory.BlockchainStorage, _provider.ProviderCategory);

            // Test Provider Status
            var activateResult = await _provider.ActivateProviderAsync();
            Assert.IsTrue(activateResult.IsSuccess);
            Assert.IsTrue(_provider.IsProviderActivated);

            var deactivateResult = await _provider.DeActivateProviderAsync();
            Assert.IsTrue(deactivateResult.IsSuccess);
            Assert.IsFalse(_provider.IsProviderActivated);
        }

        [TestMethod]
        public async Task ErrorHandling_ShouldBeConsistent()
        {
            // Test that all methods return consistent error messages
            var avatar = new Avatar();
            var saveAvatarResult = await _provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(saveAvatarResult.IsSuccess);
            Assert.IsTrue(saveAvatarResult.Message.Contains("not supported yet by Polygon provider"));

            var holon = new Holon();
            var saveHolonResult = await _provider.SaveHolonAsync(holon);
            Assert.IsFalse(saveHolonResult.IsSuccess);
            Assert.IsTrue(saveHolonResult.Message.Contains("not supported yet by Polygon provider"));

            var transaction = new WalletTransactionRequest();
            var sendTransactionResult = await _provider.SendTransactionAsync(transaction);
            Assert.IsFalse(sendTransactionResult.IsSuccess);
            Assert.IsTrue(sendTransactionResult.Message.Contains("not supported yet by Polygon provider"));

            var nftTransaction = new NFTWalletTransactionRequest();
            var sendNFTResult = await _provider.SendNFTAsync(nftTransaction);
            Assert.IsFalse(sendNFTResult.IsSuccess);
            Assert.IsTrue(sendNFTResult.Message.Contains("not supported yet by Polygon provider"));
        }

        [TestMethod]
        public async Task AsyncOperations_ShouldComplete()
        {
            // Test that async operations complete without hanging
            var tasks = new List<Task>
            {
                _provider.LoadAllAvatarsAsync(),
                _provider.LoadAllHolonsAsync(),
                _provider.LoadAllNFTsForAvatarAsync(Guid.NewGuid()),
                _provider.LoadAllGeoNFTsForAvatarAsync(Guid.NewGuid()),
                _provider.ExportAllAsync(),
                _provider.ImportAsync(new List<IHolon>())
            };

            var results = await Task.WhenAll(tasks);
            
            // All operations should complete (even if they return not supported)
            Assert.AreEqual(6, results.Length);
        }

        [TestMethod]
        public async Task ProviderActivation_ShouldBeIdempotent()
        {
            // Activate multiple times
            var result1 = await _provider.ActivateProviderAsync();
            var result2 = await _provider.ActivateProviderAsync();
            var result3 = await _provider.ActivateProviderAsync();

            // All should succeed
            Assert.IsTrue(result1.IsSuccess);
            Assert.IsTrue(result2.IsSuccess);
            Assert.IsTrue(result3.IsSuccess);

            // Deactivate multiple times
            var deactivateResult1 = await _provider.DeActivateProviderAsync();
            var deactivateResult2 = await _provider.DeActivateProviderAsync();

            // All should succeed
            Assert.IsTrue(deactivateResult1.IsSuccess);
            Assert.IsTrue(deactivateResult2.IsSuccess);
        }

        [TestMethod]
        public async Task ConcurrentOperations_ShouldNotThrow()
        {
            // Test concurrent operations
            var tasks = new List<Task>
            {
                Task.Run(async () => await _provider.LoadAvatarAsync(Guid.NewGuid())),
                Task.Run(async () => await _provider.LoadHolonAsync(Guid.NewGuid())),
                Task.Run(async () => await _provider.LoadNFTAsync(Guid.NewGuid())),
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
            Assert.AreEqual("PolygonOASIS", _provider.ProviderName);
            Assert.AreEqual(ProviderType.PolygonOASIS, _provider.ProviderType);
            Assert.AreEqual(ProviderCategory.BlockchainStorage, _provider.ProviderCategory);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.Description));
        }
    }
}

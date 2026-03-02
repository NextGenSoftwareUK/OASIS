using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Request;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
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
            _provider = new PolygonOASIS("https://polygon-rpc.com", "pk", "0xContract");
        }

        [TestMethod]
        public async Task FullProviderLifecycle_ShouldWork()
        {
            // Activate Provider
            var activateResult = await _provider.ActivateProviderAsync();
            Assert.IsFalse(activateResult.IsError, "Provider should activate successfully");

            // Test Avatar Operations
            var avatar = new Avatar
            {
                Username = "integrationtest",
                Email = "integration@test.com",
                FirstName = "Integration",
                LastName = "Test"
            };

            var saveAvatarResult = await _provider.SaveAvatarAsync(avatar);
            Assert.IsTrue(saveAvatarResult.IsError, "SaveAvatar should return not supported");
            Assert.IsTrue(saveAvatarResult.Message.Contains("not supported yet by Polygon provider"));

            var loadAvatarResult = await _provider.LoadAvatarByEmailAsync("integration@test.com");
            Assert.IsTrue(loadAvatarResult.IsError, "LoadAvatar should return not supported");
            Assert.IsTrue(loadAvatarResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Holon Operations
            var holon = new Holon
            {
                Name = "Integration Test Holon",
                Description = "Integration Test Description"
            };

            var saveHolonResult = await _provider.SaveHolonAsync(holon);
            Assert.IsTrue(saveHolonResult.IsError, "SaveHolon should return not supported");
            Assert.IsTrue(saveHolonResult.Message.Contains("not supported yet by Polygon provider"));

            var loadHolonResult = await _provider.LoadHolonAsync(Guid.NewGuid());
            Assert.IsTrue(loadHolonResult.IsError, "LoadHolon should return not supported");
            Assert.IsTrue(loadHolonResult.Message.Contains("not supported yet by Polygon provider"));

            // Test NFT Operations
            var mintRequest = new MintWeb3NFTRequest();
            var mintResult = await _provider.MintNFTAsync(mintRequest);
            Assert.IsTrue(mintResult.IsError, "MintNFT should return not supported");
            Assert.IsTrue(mintResult.Message.Contains("not supported yet by Polygon provider"));

            var loadNFTResult = await _provider.LoadNFTAsync(Guid.NewGuid());
            Assert.IsTrue(loadNFTResult.IsError, "LoadNFT should return not supported");
            Assert.IsTrue(loadNFTResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Transaction Operations
            var transaction = new WalletTransactionRequest();
            var sendTransactionResult = await _provider.SendTransactionAsync(transaction);
            Assert.IsTrue(sendTransactionResult.IsError, "SendTransaction should return not supported");
            Assert.IsTrue(sendTransactionResult.Message.Contains("not supported yet by Polygon provider"));

            // Test GeoNFT Operations
            var placeRequest = new PlaceWeb4GeoSpatialNFTRequest();
            var placeGeoNFTResult = await _provider.PlaceGeoNFTAsync(placeRequest);
            Assert.IsTrue(placeGeoNFTResult.IsError, "PlaceGeoNFT should return not supported");
            Assert.IsTrue(placeGeoNFTResult.Message.Contains("not supported yet by Polygon provider"));

            var loadGeoNFTsResult = await _provider.LoadAllGeoNFTsForAvatarAsync(Guid.NewGuid());
            Assert.IsTrue(loadGeoNFTsResult.IsError, "LoadAllGeoNFTsForAvatar should return not supported");
            Assert.IsTrue(loadGeoNFTsResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Search Operations
            var searchParams = new SearchParams();
            var searchResult = _provider.Search(searchParams);
            Assert.IsTrue(searchResult.IsError, "Search should return not supported");
            Assert.IsTrue(searchResult.Message.Contains("not supported yet by Polygon provider"));

            // Test Import/Export Operations
            var holons = new List<IHolon>();
            var importResult = await _provider.ImportAsync(holons);
            Assert.IsTrue(importResult.IsError, "Import should return not supported");
            Assert.IsTrue(importResult.Message.Contains("not supported yet by Polygon provider"));

            var exportResult = await _provider.ExportAllAsync();
            Assert.IsTrue(exportResult.IsError, "ExportAll should return not supported");
            Assert.IsTrue(exportResult.Message.Contains("not supported yet by Polygon provider"));

            // Deactivate Provider
            var deactivateResult = await _provider.DeActivateProviderAsync();
            Assert.IsFalse(deactivateResult.IsError, "Provider should deactivate successfully");
        }

        [TestMethod]
        public async Task ProviderConfiguration_ShouldBeCorrect()
        {
            // Test Provider Type
            Assert.AreEqual(ProviderType.PolygonOASIS, _provider.ProviderType.Value);
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
            Assert.IsTrue(saveAvatarResult.Message.Contains("not supported yet by Polygon provider"));

            var holon = new Holon();
            var saveHolonResult = await _provider.SaveHolonAsync(holon);
            Assert.IsTrue(saveHolonResult.IsError);
            Assert.IsTrue(saveHolonResult.Message.Contains("not supported yet by Polygon provider"));

            var transaction = new WalletTransactionRequest();
            var sendTransactionResult = await _provider.SendTransactionAsync(transaction);
            Assert.IsTrue(sendTransactionResult.IsError);
            Assert.IsTrue(sendTransactionResult.Message.Contains("not supported yet by Polygon provider"));

            var nftTransaction = new SendWeb3NFTRequest();
            var sendNFTResult = await _provider.SendNFTAsync(nftTransaction);
            Assert.IsTrue(sendNFTResult.IsError);
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
            Assert.AreEqual(ProviderType.PolygonOASIS, _provider.ProviderType.Value);
            Assert.AreEqual(ProviderCategory.Blockchain, _provider.ProviderCategory.Value);
            Assert.IsFalse(string.IsNullOrEmpty(_provider.ProviderDescription));
        }
    }
}

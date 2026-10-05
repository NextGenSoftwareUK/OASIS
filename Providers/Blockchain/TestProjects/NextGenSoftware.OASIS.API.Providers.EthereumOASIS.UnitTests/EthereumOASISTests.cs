using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.UnitTests
{
    [TestClass]
    public class EthereumOASISTests
    {
        private EthereumOASIS _provider;

        [TestInitialize]
        public void Setup()
        {
            _provider = new EthereumOASIS();
        }

        [TestMethod]
        public async Task ActivateProvider_ShouldReturnSuccess()
        {
            // Act
            var result = await _provider.ActivateProviderAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public async Task DeActivateProvider_ShouldReturnSuccess()
        {
            // Act
            var result = await _provider.DeActivateProviderAsync();

            // Assert
            Assert.IsTrue(result.IsSuccess);
        }

        [TestMethod]
        public async Task SaveAvatar_ShouldReturnNotSupported()
        {
            // Arrange
            var avatar = new Avatar
            {
                Username = "testuser",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            // Act
            var result = await _provider.SaveAvatarAsync(avatar);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAllAvatars_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAllAvatarsAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarByEmail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarByEmailAsync("test@example.com");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarByUsername_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarByUsernameAsync("testuser");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarByProviderKey_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarByProviderKeyAsync("testkey");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task SaveHolon_ShouldReturnNotSupported()
        {
            // Arrange
            var holon = new Holon
            {
                Name = "Test Holon",
                Description = "Test Description"
            };

            // Act
            var result = await _provider.SaveHolonAsync(holon);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAllHolons_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAllHolonsAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadHolonAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadHolonsForParent_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadHolonsForParentAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task DeleteHolon_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.DeleteHolonAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task SaveAvatarDetail_ShouldReturnNotSupported()
        {
            // Arrange
            var avatarDetail = new AvatarDetail
            {
                Username = "testuser",
                Email = "test@example.com"
            };

            // Act
            var result = await _provider.SaveAvatarDetailAsync(avatarDetail);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarDetail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarDetailAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarDetailByEmail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarDetailByEmailAsync("test@example.com");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarDetailByUsername_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarDetailByUsernameAsync("testuser");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public void GetPlayersNearMe_ShouldReturnNotSupported()
        {
            // Act
            var result = _provider.GetPlayersNearMe();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported by Ethereum provider"));
        }

        [TestMethod]
        public void GetHolonsNearMe_ShouldReturnNotSupported()
        {
            // Act
            var result = _provider.GetHolonsNearMe(HolonType.All);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported by Ethereum provider"));
        }

        [TestMethod]
        public async Task SendTransaction_ShouldReturnNotSupported()
        {
            // Arrange
            var transaction = new WalletTransactionRequest();

            // Act
            var result = await _provider.SendTransactionAsync(transaction);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task SendNFT_ShouldReturnNotSupported()
        {
            // Arrange
            var nftTransaction = new NFTWalletTransactionRequest();

            // Act
            var result = await _provider.SendNFTAsync(nftTransaction);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task MintNFT_ShouldReturnNotSupported()
        {
            // Arrange
            var mintRequest = new MintNFTTransactionRequest();

            // Act
            var result = await _provider.MintNFTAsync(mintRequest);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadNFT_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadNFTAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadNFTByHash_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadNFTAsync("testhash");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAllNFTsForAvatar_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAllNFTsForAvatarAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task LoadAllGeoNFTsForAvatar_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAllGeoNFTsForAvatarAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task PlaceGeoNFT_ShouldReturnNotSupported()
        {
            // Arrange
            var placeRequest = new PlaceGeoSpatialNFTRequest();

            // Act
            var result = await _provider.PlaceGeoNFTAsync(placeRequest);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task MintAndPlaceGeoNFT_ShouldReturnNotSupported()
        {
            // Arrange
            var mintAndPlaceRequest = new MintAndPlaceGeoSpatialNFTRequest();

            // Act
            var result = await _provider.MintAndPlaceGeoNFTAsync(mintAndPlaceRequest);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task Search_ShouldReturnNotSupported()
        {
            // Arrange
            var searchParams = new SearchParams();

            // Act
            var result = _provider.Search(searchParams);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task Import_ShouldReturnNotSupported()
        {
            // Arrange
            var holons = new List<IHolon>();

            // Act
            var result = await _provider.ImportAsync(holons);

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task ExportAll_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllAsync();

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task ExportAllDataForAvatarById_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllDataForAvatarByIdAsync(Guid.NewGuid());

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task ExportAllDataForAvatarByUsername_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllDataForAvatarByUsernameAsync("testuser");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public async Task ExportAllDataForAvatarByEmail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllDataForAvatarByEmailAsync("test@example.com");

            // Assert
            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Message.Contains("not supported yet by Ethereum provider"));
        }

        [TestMethod]
        public void ProviderType_ShouldBeEthereumOASIS()
        {
            // Assert
            Assert.AreEqual(ProviderType.EthereumOASIS, _provider.ProviderType);
        }

        [TestMethod]
        public void ProviderCategory_ShouldBeBlockchainStorage()
        {
            // Assert
            Assert.AreEqual(ProviderCategory.BlockchainStorage, _provider.ProviderCategory);
        }

        [TestMethod]
        public void ProviderName_ShouldBeEthereumOASIS()
        {
            // Assert
            Assert.AreEqual("EthereumOASIS", _provider.ProviderName);
        }
    }
}

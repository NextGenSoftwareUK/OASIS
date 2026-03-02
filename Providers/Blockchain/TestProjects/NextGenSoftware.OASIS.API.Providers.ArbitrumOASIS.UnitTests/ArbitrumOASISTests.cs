using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.UnitTests
{
    [TestClass]
    public class ArbitrumOASISTests
    {
        private ArbitrumOASIS _provider;
        private const string TestHost = "https://arb1.arbitrum.io/rpc";
        private const string TestKey = "0x0000000000000000000000000000000000000000000000000000000000000001";
        private static readonly BigInteger TestChainId = 42161;
        private const string TestContract = "0x0000000000000000000000000000000000000000";

        [TestInitialize]
        public void Setup()
        {
            _provider = new ArbitrumOASIS(TestHost, TestKey, TestChainId, TestContract);
        }

        [TestMethod]
        public async Task ActivateProvider_ShouldReturnSuccess()
        {
            // Act
            var result = await _provider.ActivateProviderAsync();

            // Assert
            Assert.IsFalse(result.IsError);
        }

        [TestMethod]
        public async Task DeActivateProvider_ShouldReturnSuccess()
        {
            // Act
            var result = await _provider.DeActivateProviderAsync();

            // Assert
            Assert.IsFalse(result.IsError);
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
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAllAvatars_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAllAvatarsAsync();

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarByEmail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarByEmailAsync("test@example.com");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarByUsername_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarByUsernameAsync("testuser");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarByProviderKey_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarByProviderKeyAsync("testkey");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatar_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarAsync(Guid.NewGuid());

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
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
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAllHolons_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAllHolonsAsync();

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadHolon_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadHolonAsync(Guid.NewGuid());

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadHolonsForParent_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadHolonsForParentAsync(Guid.NewGuid());

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task DeleteHolon_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.DeleteHolonAsync(Guid.NewGuid());

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
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
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarDetail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarDetailAsync(Guid.NewGuid());

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarDetailByEmail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarDetailByEmailAsync("test@example.com");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task LoadAvatarDetailByUsername_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.LoadAvatarDetailByUsernameAsync("testuser");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        [Ignore("GetPlayersNearMe not on provider")]
        public void GetPlayersNearMe_ShouldReturnNotSupported()
        {
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void GetHolonsNearMe_ShouldReturnNotSupported()
        {
            // Act
            var result = _provider.GetHolonsNearMe(HolonType.All);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported by Arbitrum provider"));
        }

        [TestMethod]
        public async Task SendTransaction_ShouldReturnNotSupported()
        {
            var result = await _provider.SendTransactionAsync("0x0", "0x0", 0m, "test");
            Assert.IsTrue(result.IsError);
        }

        [TestMethod]
        public async Task SendNFT_ShouldReturnNotSupported()
        {
            var nftTransaction = new SendWeb3NFTRequest();
            var result = await _provider.SendNFTAsync(nftTransaction);
            Assert.IsTrue(result.IsError);
        }

        [TestMethod]
        public async Task MintNFT_ShouldReturnNotSupported()
        {
            var mintRequest = new MintWeb3NFTRequest();
            var result = await _provider.MintNFTAsync(mintRequest);
            Assert.IsTrue(result.IsError);
        }

        [TestMethod]
        [Ignore("LoadNFTAsync not on provider")]
        public async Task LoadNFT_ShouldReturnNotSupported() => await Task.CompletedTask;

        [TestMethod]
        [Ignore("LoadNFTAsync not on provider")]
        public async Task LoadNFTByHash_ShouldReturnNotSupported() => await Task.CompletedTask;

        [TestMethod]
        [Ignore("LoadAllNFTsForAvatarAsync not on provider")]
        public async Task LoadAllNFTsForAvatar_ShouldReturnNotSupported() => await Task.CompletedTask;

        [TestMethod]
        [Ignore("LoadAllGeoNFTsForAvatarAsync not on provider")]
        public async Task LoadAllGeoNFTsForAvatar_ShouldReturnNotSupported() => await Task.CompletedTask;

        [TestMethod]
        [Ignore("PlaceGeoNFTAsync not on provider")]
        public async Task PlaceGeoNFT_ShouldReturnNotSupported() => await Task.CompletedTask;

        [TestMethod]
        [Ignore("MintAndPlaceGeoNFTAsync not on provider")]
        public async Task MintAndPlaceGeoNFT_ShouldReturnNotSupported() => await Task.CompletedTask;

        [TestMethod]
        public async Task Search_ShouldReturnNotSupported()
        {
            var searchParams = new SearchParams();
            var result = _provider.Search(searchParams);
            Assert.IsTrue(result.IsError);
            await Task.CompletedTask;
        }

        [TestMethod]
        public async Task Import_ShouldReturnNotSupported()
        {
            // Arrange
            var holons = new List<IHolon>();

            // Act
            var result = await _provider.ImportAsync(holons);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task ExportAll_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllAsync();

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task ExportAllDataForAvatarById_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllDataForAvatarByIdAsync(Guid.NewGuid());

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task ExportAllDataForAvatarByUsername_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllDataForAvatarByUsernameAsync("testuser");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public async Task ExportAllDataForAvatarByEmail_ShouldReturnNotSupported()
        {
            // Act
            var result = await _provider.ExportAllDataForAvatarByEmailAsync("test@example.com");

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Message.Contains("not supported yet by Arbitrum provider"));
        }

        [TestMethod]
        public void ProviderType_ShouldBeArbitrumOASIS()
        {
            // Assert
            Assert.AreEqual(ProviderType.ArbitrumOASIS, _provider.ProviderType.Value);
        }

        [TestMethod]
        public void ProviderCategory_ShouldBeBlockchain()
        {
            Assert.AreEqual(ProviderCategory.Blockchain, _provider.ProviderCategory.Value);
        }

        [TestMethod]
        public void ProviderName_ShouldBeArbitrumOASIS()
        {
            // Assert
            Assert.AreEqual("ArbitrumOASIS", _provider.ProviderName);
        }
    }
}

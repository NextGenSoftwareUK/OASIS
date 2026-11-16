using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.UnitTests
{
    public class HoloOASISTests
    {
        [Fact]
        public void AvatarSerializationOmitsAuthenticationStateFromBothModes()
        {
            var avatar = new Avatar
            {
                Username = "public-test-avatar",
                Password = "synthetic-password",
                JwtToken = "synthetic-session",
                RefreshToken = "synthetic-refresh",
                ResetToken = "synthetic-reset",
                VerificationToken = "synthetic-verification",
                ProviderUniqueStorageKey = new Dictionary<ProviderType, string>()
            };

            object direct = DataHelper.ConvertAvatarToParamsObject(avatar);
            foreach (var field in new[] { "password", "jwt_token", "refresh_token", "refresh_tokens",
                "reset_token", "verification_token", "password_reset", "reset_token_expires" })
                Assert.Null(direct.GetType().GetProperty(field));
            Assert.Equal(avatar.Username, direct.GetType().GetProperty("username").GetValue(direct));

            var reflection = DataHelper.ConvertAvatarToHoloOASISAvatar(avatar, new HcAvatar());
            Assert.Equal(avatar.Username, reflection.Username);
            Assert.Null(reflection.Password);
            Assert.Null(reflection.JwtToken);
            Assert.Null(reflection.RefreshToken);
            Assert.Null(reflection.ResetToken);
            Assert.Null(reflection.VerificationToken);
        }

        [Theory]
        [InlineData("synthetic-private-key", null)]
        [InlineData(null, "synthetic-recovery-phrase")]
        public void AvatarSerializationRejectsWalletCredentialsInBothModes(string privateKey, string recoveryPhrase)
        {
            var avatar = new Avatar
            {
                ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>
                {
                    [ProviderType.HoloOASIS] = new List<IProviderWallet>
                    {
                        new ProviderWallet { PrivateKey = privateKey, SecretRecoveryPhrase = recoveryPhrase }
                    }
                }
            };

            var direct = Assert.Throws<InvalidOperationException>(() => { DataHelper.ConvertAvatarToParamsObject(avatar); });
            var reflection = Assert.Throws<InvalidOperationException>(() => DataHelper.ConvertAvatarToHoloOASISAvatar(avatar, null));
            Assert.Equal("HoloOASIS public avatar entries cannot store wallet signing credentials.", direct.Message);
            Assert.Equal(direct.Message, reflection.Message);
        }

        [Fact]
        public void Constructor_ShouldInitializeProviderCorrectly()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Assert
            Assert.Equal("HoloOASIS", holoProvider.ProviderName);
            Assert.Equal("Holochain Provider", holoProvider.ProviderDescription);
            Assert.Equal(ProviderType.HoloOASIS, holoProvider.ProviderType.Value);
            Assert.Equal(ProviderCategory.StorageLocalAndNetwork, holoProvider.ProviderCategory.Value);
        }

        [Fact]
        public void ProviderName_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Assert
            Assert.Equal("HoloOASIS", holoProvider.ProviderName);
        }

        [Fact]
        public void ProviderDescription_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Assert
            Assert.Equal("Holochain Provider", holoProvider.ProviderDescription);
        }

        [Fact]
        public void ProviderType_ShouldBeHoloOASIS()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Assert
            Assert.Equal(ProviderType.HoloOASIS, holoProvider.ProviderType.Value);
        }

        [Fact]
        public void ProviderCategory_ShouldBeStorageAndNetwork()
        {
            // Arrange & Act
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Assert
            Assert.Equal(ProviderCategory.StorageLocalAndNetwork, holoProvider.ProviderCategory.Value);
        }

        [Fact(Skip = "Needs a running Holochain conductor: activation starts or connects to one.")]
        public async Task ActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Act
            var result = await holoProvider.ActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Holo provider activated successfully", result.Message);
        }

        [Fact(Skip = "Needs a running Holochain conductor: activation starts or connects to one.")]
        public async Task DeActivateProviderAsync_ShouldReturnSuccess()
        {
            // Arrange
            var holoProvider = new HoloOASIS("ws://localhost:7777");

            // Act
            var result = await holoProvider.DeActivateProviderAsync();

            // Assert
            Assert.False(result.IsError);
            Assert.True(result.Result);
            Assert.Contains("Holo provider deactivated successfully", result.Message);
        }

        [Fact]
        public void LoadOnChainNFTData_ShouldReturnHolochainNFT()
        {
            // Arrange
            var holoProvider = new HoloOASIS("ws://localhost:7777");
            var tokenAddress = "HoloTestTokenAddress123";

            // Act
            var result = holoProvider.LoadOnChainNFTData(tokenAddress);

            // Assert
            Assert.False(result.IsError);
            Assert.NotNull(result.Result);
            // IWeb3NFT no longer has TokenId / Name (renamed in the NFT model), so these no longer compile; re-point them at the current NFT properties.
            //Assert.Equal(tokenAddress, result.Result.TokenId);
            //Assert.Contains("Holochain NFT", result.Result.Name);
            Assert.Contains("Holochain DHT", result.Result.Description);
        }
    }
}

using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletKeyAuthorizationTests
{
    [Theory]
    [InlineData("id", false)] [InlineData("id", true)]
    [InlineData("username", false)] [InlineData("username", true)]
    [InlineData("empty-id", false)] [InlineData("empty-username", false)]
    public void DeniedPrivateKeyLookupDoesNotReadWallets(string route, bool anonymous)
    {
        var previous = OASISRequestContext.CurrentAvatar;
        try
        {
            var actor = new Avatar { Id = anonymous ? Guid.Empty : Guid.NewGuid(), Username = anonymous ? null : "owned-actor" };
            OASISRequestContext.CurrentAvatar = actor;
            var provider = new Mock<IOASISStorageProvider>();
            provider.SetupAllProperties();
            var local = provider.As<IOASISLocalStorageProvider>();
            provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
            provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
            provider.Object.ProviderName = "owned-auth-boundary";
            provider.Object.IsProviderActivated = true;
            provider.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
            local.Setup(p => p.LoadProviderWalletsForAvatarById(It.IsAny<Guid>())).Throws(new InvalidOperationException("Credential store must not be read."));
            provider.Setup(p => p.LoadAvatarByUsername(It.IsAny<string>(), 0)).Throws(new InvalidOperationException("Identity store must not be read."));
            var dna = new OASISDNA();
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            var providers = new ProviderManager(null, dna);
            providers.RegisterProvider(provider.Object);
            Assert.False(providers.SetAndActivateCurrentStorageProvider(provider.Object).IsError);
            var manager = new KeyManager(null, dna, providers);
            provider.Invocations.Clear();
            var result = route.Contains("username")
                ? manager.GetProviderPrivateKeysForAvatarByUsername(route == "empty-username" ? "" : "other-avatar", ProviderType.EthereumOASIS)
                : manager.GetProviderPrivateKeysForAvatarById(route == "empty-id" ? Guid.Empty : Guid.NewGuid(), ProviderType.EthereumOASIS);
            Assert.True(result.IsError);
            Assert.Null(result.Result);
            Assert.Equal("OASIS_PRIVATE_KEY_ACCESS_DENIED", result.ErrorCode);
            Assert.IsType<UnauthorizedAccessException>(result.Exception);
            local.Verify(p => p.LoadProviderWalletsForAvatarById(It.IsAny<Guid>()), Times.Never);
            provider.Verify(p => p.LoadAvatarByUsername(It.IsAny<string>(), 0), Times.Never);
        }
        finally { OASISRequestContext.CurrentAvatar = previous; }
    }
}

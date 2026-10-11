using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Moq;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletCredentialIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultWalletLookupDoesNotDecryptStoredWalletInPlace(bool showPrivateKey)
    {
        var avatarId = Guid.NewGuid();
        var wallet = new ProviderWallet
        {
            Id = Guid.NewGuid(), AvatarId = avatarId, IsDefaultWallet = true,
            ProviderType = ProviderType.TRONOASIS, PrivateKey = "invalid-encrypted-key"
        };
        var stored = new Dictionary<ProviderType, List<IProviderWallet>>
        {
            [ProviderType.TRONOASIS] = new List<IProviderWallet> { wallet }
        };
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        var local = provider.As<IOASISLocalStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        provider.Object.ProviderName = "wallet-isolation-test";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        local.Setup(x => x.LoadProviderWalletsForAvatarByIdAsync(avatarId)).ReturnsAsync(
            new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(stored));
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
        var providers = new ProviderManager(null, dna);
        providers.RegisterProvider(provider.Object);
        var activation = providers.SetAndActivateCurrentStorageProvider(provider.Object);
        Assert.False(activation.IsError, activation.Message);
        var manager = new WalletManager(null, dna, providers);
        var result = await manager.GetAvatarDefaultWalletByIdAsync(avatarId, ProviderType.TRONOASIS,
            showPrivateKeys: showPrivateKey);
        Assert.Equal("invalid-encrypted-key", wallet.PrivateKey);
        if (showPrivateKey)
        {
            Assert.True(result.IsError);
            Assert.Null(result.Result);
        }
        else
        {
            Assert.False(result.IsError);
            Assert.NotSame(wallet, result.Result);
            Assert.Equal(wallet.Id, result.Result.Id);
            Assert.Equal(wallet.AvatarId, result.Result.AvatarId);
        }
    }

    [Fact]
    public void FailedCredentialDecryptionReturnsNoWalletAndPreservesStoredValue()
    {
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
        var manager = new WalletManager(null, dna, new ProviderManager(null, dna));
        var wallet = new ProviderWallet { Id = Guid.NewGuid(), PrivateKey = "not-valid-encrypted-key" };
        var result = manager.ProcessDecryption(wallet, showPrivateKey: true);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.Equal("not-valid-encrypted-key", wallet.PrivateKey);
    }

    [Fact]
    public void CredentialCopyPreservesIdentityWithoutChangingStoredScalars()
    {
        var wallet = new ProviderWallet
        {
            Id = Guid.NewGuid(), AvatarId = Guid.NewGuid(), Name = "TRON wallet",
            PrivateKey = "encrypted-key", SecretRecoveryPhrase = "encrypted-seed",
            WalletAddress = "address", IsDefaultWallet = true
        };
        var copy = wallet.CopyForCredentialDecryption();
        Assert.NotSame(wallet, copy);
        Assert.Equal(wallet.Id, copy.Id);
        Assert.Equal(wallet.AvatarId, copy.AvatarId);
        Assert.Equal(wallet.WalletAddress, copy.WalletAddress);
        Assert.True(copy.IsDefaultWallet);
        copy.PrivateKey = "decrypted-key";
        copy.SecretRecoveryPhrase = "decrypted-seed";
        Assert.Equal("encrypted-key", wallet.PrivateKey);
        Assert.Equal("encrypted-seed", wallet.SecretRecoveryPhrase);
    }
}

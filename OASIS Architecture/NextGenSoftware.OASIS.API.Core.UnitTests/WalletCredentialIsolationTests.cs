using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Moq;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Holons;
using Rijndael256;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalletCredentialIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WalletListFilteringUsesDetachedCredentialCopies(bool valid)
    {
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
        dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings {
            Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32)
        } };
        var manager = new WalletManager(null, dna, new ProviderManager(null, dna));
        var encrypted = valid ? Rijndael.Encrypt("local-list-key", new string('k', 32), KeySize.Aes256) : "invalid-ciphertext";
        var wallet = new ProviderWallet { PrivateKey = encrypted, ProviderType = ProviderType.TRONOASIS };
        var stored = new Dictionary<ProviderType, List<IProviderWallet>> { [ProviderType.TRONOASIS] = new() { wallet } };
        var method = typeof(WalletManager).GetMethod("FilterWallets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        if (valid)
        {
            var result = (Dictionary<ProviderType, List<IProviderWallet>>)method.Invoke(manager,
                new object[] { stored, false, true, false, ProviderType.All })!;
            Assert.NotSame(wallet, result[ProviderType.TRONOASIS][0]);
            Assert.Equal("local-list-key", result[ProviderType.TRONOASIS][0].PrivateKey);
        }
        else
        {
            var error = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(manager,
                new object[] { stored, false, true, false, ProviderType.All }));
            Assert.IsType<InvalidOperationException>(error.InnerException);
        }
        Assert.Equal(encrypted, wallet.PrivateKey);
        var missing = (Dictionary<ProviderType, List<IProviderWallet>>)method.Invoke(manager,
            new object[] { stored, false, false, false, ProviderType.SuiOASIS })!;
        Assert.Empty(missing);
        Assert.Single(stored);
    }
    [Theory]
    [InlineData(false, "id", false, false)]
    [InlineData(true, "id", false, false)]
    [InlineData(false, "username", false, false)]
    [InlineData(true, "username", false, false)]
    [InlineData(false, "email", false, false)]
    [InlineData(true, "email", false, false)]
    [InlineData(true, "id", true, false)]
    [InlineData(true, "username", true, false)]
    [InlineData(true, "email", true, false)]
    [InlineData(true, "id", true, true)]
    [InlineData(true, "username", true, true)]
    [InlineData(true, "email", true, true)]
    public async Task DefaultWalletLookupDoesNotDecryptStoredWalletInPlace(bool showPrivateKey, string route, bool validKey, bool ambiguous)
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
        if (ambiguous) stored[ProviderType.TRONOASIS].Add(new ProviderWallet {
            Id = Guid.NewGuid(), AvatarId = avatarId, IsDefaultWallet = true,
            ProviderType = ProviderType.TRONOASIS, PrivateKey = "must-not-decrypt-second-default"
        });
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
        provider.Setup(x => x.LoadAvatarByUsernameAsync("isolation-user", 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Username = "isolation-user" }));
        provider.Setup(x => x.LoadAvatarByEmailAsync("isolation@example.test", 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Email = "isolation@example.test" }));
        var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA();
        dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings {
            Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32)
        } };
        if (validKey) wallet.PrivateKey = Rijndael.Encrypt("test-only-signing-key", new string('k', 32), KeySize.Aes256);
        var originalStoredKey = wallet.PrivateKey;
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
        var providers = new ProviderManager(null, dna);
        providers.RegisterProvider(provider.Object);
        var activation = providers.SetAndActivateCurrentStorageProvider(provider.Object);
        Assert.False(activation.IsError, activation.Message);
        var manager = new WalletManager(null, dna, providers);
        var result = route == "username"
            ? await manager.GetAvatarDefaultWalletByUsernameAsync("isolation-user", showPrivateKeys: showPrivateKey, providerType: ProviderType.TRONOASIS)
            : route == "email"
                ? await manager.GetAvatarDefaultWalletByEmailAsync("isolation@example.test", ProviderType.TRONOASIS, showPrivateKeys: showPrivateKey)
                : await manager.GetAvatarDefaultWalletByIdAsync(avatarId, ProviderType.TRONOASIS, showPrivateKeys: showPrivateKey);
        Assert.Equal(originalStoredKey, wallet.PrivateKey);
        if (ambiguous)
        {
            Assert.True(result.IsError);
            Assert.Null(result.Result);
            Assert.False(result.IsLoaded);
            Assert.IsType<InvalidDataException>(result.Exception);
            Assert.Equal("must-not-decrypt-second-default", stored[ProviderType.TRONOASIS][1].PrivateKey);
            Assert.All(stored[ProviderType.TRONOASIS], item => Assert.True(item.IsDefaultWallet));
        }
        else if (showPrivateKey && !validKey)
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
            if (validKey) Assert.Equal("test-only-signing-key", result.Result.PrivateKey);
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

using System.Reflection;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.Subscription;

public partial class SubscriptionEndpointsTests
{
    [Fact]
    public async Task HederaBurnResolvesEncryptedDefaultAvatarWalletAndRejectsConflictingKey()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !System.IO.File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HederaCurrent/AvatarBurn"); Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var priorBridge = Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var avatarId = Guid.Parse("8c523274-88da-47c2-8dad-4a2c9a4e2c82"); var saved = false;
        var encrypted = Rijndael256.Rijndael.Encrypt("fixture-burn-key", new string('k', 32), Rijndael256.KeySize.Aes256);
        try
        {
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            var prior = await local.LoadProviderWalletsForAvatarByIdAsync(avatarId);
            prior.IsError.Should().BeFalse(prior.Message); (prior.Result == null || prior.Result.Count == 0).Should().BeTrue();
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new() { [ProviderType.HashgraphOASIS] = new List<IProviderWallet> {
                new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, ProviderType = ProviderType.HashgraphOASIS, WalletAddress = "0.0.9999", IsDefaultWallet = false, PrivateKey = encrypted },
                new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, ProviderType = ProviderType.HashgraphOASIS, WalletAddress = "0.0.1001", IsDefaultWallet = true, PrivateKey = encrypted, PublicKey = "fixture-public" } } })).IsError.Should().BeFalse(); saved = true;
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32) } };
            var providers = new ProviderManager(null, dna); providers.RegisterProvider(local); providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            var constructor = typeof(WalletManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(value => value.GetParameters().Length == 4);
            var manager = (WalletManager)constructor.Invoke(new object?[] { local, dna, providers, null });
            var provider = new HashgraphOASIS(network: "testnet", chainId: "296", walletManager: manager);
            Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE", Path.Combine(directory.FullName, "Scripts/TestHosts/hedera-burn-wallet-fixture.mjs"));
            var request = new BurnWeb3TokenRequest { BurntByAvatarId = avatarId, Amount = 1, TokenAddress = "0.0.2001", OwnerPrivateKey = null, OwnerPublicKey = null, OwnerSeedPhrase = null };
            var result = await provider.BurnTokenAsync(request); result.IsError.Should().BeFalse(result.Message); result.Result.TransactionResult.Should().StartWith("0.0.1001@");
            var matching = new BurnWeb3TokenRequest { BurntByAvatarId = avatarId, Amount = 1, TokenAddress = "0.0.2001", OwnerPrivateKey = "fixture-burn-key", OwnerPublicKey = "fixture-public", OwnerSeedPhrase = null };
            var syncBurn = provider.BurnToken(matching); syncBurn.IsError.Should().BeFalse(syncBurn.Message);
            syncBurn.Result.TransactionResult.Should().NotBe(result.Result.TransactionResult);
            var mintRequest = new MintWeb3TokenRequest { MintedByAvatarId = avatarId, Amount = 1,
                MetaData = new() { ["TokenAddress"] = "0.0.2001" } };
            var minted = await provider.MintTokenAsync(mintRequest); minted.IsError.Should().BeFalse(minted.Message); minted.Result.TransactionResult.Should().StartWith("0.0.1001@");
            var syncMint = provider.MintToken(mintRequest); syncMint.IsError.Should().BeFalse(syncMint.Message);
            syncMint.Result.TransactionResult.Should().NotBe(minted.Result.TransactionResult);
            var conflicting = new BurnWeb3TokenRequest { BurntByAvatarId = avatarId, Amount = 1, TokenAddress = "0.0.2001", OwnerPrivateKey = "conflicting-key", OwnerPublicKey = null, OwnerSeedPhrase = null };
            var rejected = await provider.BurnTokenAsync(conflicting); rejected.IsError.Should().BeTrue(); rejected.Result.Should().BeNull(); rejected.Message.Should().Contain("does not match");
            (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[ProviderType.HashgraphOASIS][1].PrivateKey.Should().Be(encrypted);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE", priorBridge);
            if (saved) (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new())).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();
        }
    }
}

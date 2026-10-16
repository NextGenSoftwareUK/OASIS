using System.Reflection;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosAvatarMintSignerTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TokenMintUsesEncryptedAvatarDefaultWalletInsteadOfConfiguredOrFirstWallet(bool synchronous)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HyperDriveCosmos"); Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var previous = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", Path.Combine(directory.FullName,
            "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests/Fixtures/nft-signer.mjs"));
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        using var cosmos = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "different-configured-key", "fixture-contract");
        var avatarId = Guid.Parse("c1bcb045-d27a-4734-85c8-f56f9f5ef1b5");
        var encrypted = Rijndael256.Rijndael.Encrypt("test-only-explicit", new string('k', 32), Rijndael256.KeySize.Aes256);
        var saved = false;
        try
        {
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            var prior = await local.LoadProviderWalletsForAvatarByIdAsync(avatarId);
            prior.IsError.Should().BeFalse(prior.Message);
            (prior.Result == null || prior.Result.Count == 0).Should().BeTrue("preserve a pre-existing fixture wallet map");
            var wallets = new List<IProviderWallet> {
                new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, ProviderType = ProviderType.CosmosBlockChainOASIS,
                    IsDefaultWallet = false, WalletAddress = "decoy", PrivateKey = Rijndael256.Rijndael.Encrypt("decoy-key", new string('k', 32), Rijndael256.KeySize.Aes256) },
                new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, ProviderType = ProviderType.CosmosBlockChainOASIS,
                    IsDefaultWallet = true, WalletAddress = "fixture-owner", PublicKey = "fixture-public", PrivateKey = encrypted } };
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new() { [ProviderType.CosmosBlockChainOASIS] = wallets })).IsError.Should().BeFalse(); saved = true;
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32) } };
            var providers = new ProviderManager(null, dna); providers.RegisterProvider(local); providers.RegisterProvider(cosmos);
            providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            var constructor = typeof(WalletManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(value => value.GetParameters().Length == 4);
            cosmos.WalletManager = (WalletManager)constructor.Invoke(new object?[] { local, dna, providers, null });
            var request=new MintWeb3TokenRequest { Symbol = "fixture-token", Amount = 1, MintedByAvatarId = avatarId };
            var minted = synchronous ? cosmos.MintToken(request):await cosmos.MintTokenAsync(request);
            minted.IsError.Should().BeFalse(minted.Message);
            // nft-signer.mjs is a controlled transport fixture, not native consensus evidence.
            // Its execute branch rejects any signer except the decrypted explicit default key.
            minted.Result.TransactionResult.Should().Be(new string('a',64));
            (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[ProviderType.CosmosBlockChainOASIS][1].PrivateKey.Should().Be(encrypted);
            var absent=new MintWeb3TokenRequest { Symbol = "fixture-token", Amount = 1, MintedByAvatarId = Guid.NewGuid() };
            var missing = synchronous ? cosmos.MintToken(absent):await cosmos.MintTokenAsync(absent);
            missing.IsError.Should().BeTrue(); missing.Result.Should().BeNull();
            missing.Message.Should().Contain("sender wallet could not be loaded");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", previous);
            if (saved) (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new())).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();
        }
    }
}

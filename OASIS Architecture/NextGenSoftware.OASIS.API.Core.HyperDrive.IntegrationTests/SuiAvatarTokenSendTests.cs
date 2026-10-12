using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.Utilities;
using Rijndael256;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class SuiAvatarTokenSendTests
{
    [Fact]
    public async Task NftMintAndBurnUseStoredAvatarWalletWithoutConfiguredSigner()
    {
        string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");
        var rpc = Required("OASIS_SUI_TEST_RPC"); new Uri(rpc).IsLoopback.Should().BeTrue();
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT")); Path.GetFileName(root).Should().Be("HyperDriveSui");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = fixture.RootElement;
        using var sui = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!, settings.GetProperty("packageAddress").GetString()!);
        var issuer = await sui.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);
        issuer.IsError.Should().BeFalse(issuer.Message);
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var avatarId = Guid.Parse("5f675630-2622-43b5-a940-2897af23d5a7");
        var encrypted = Rijndael.Encrypt(issuer.Result.PrivateKey, new string('k', 32), KeySize.Aes256);
        string? ownedAddress = null; Guid ownedId = Guid.Empty; var walletSaved = false;
        try
        {
            (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
            var prior = await local.LoadProviderWalletsForAvatarByIdAsync(avatarId);
            prior.IsError.Should().BeFalse(prior.Message); (prior.Result == null || prior.Result.Count == 0).Should().BeTrue();
            var wallet = new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, IsDefaultWallet = true,
                ProviderType = ProviderType.SuiOASIS, WalletAddress = issuer.Result.WalletAddressLegacy, PublicKey = issuer.Result.PublicKey, PrivateKey = encrypted };
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new() { [ProviderType.SuiOASIS] = new List<IProviderWallet> { wallet } })).IsError.Should().BeFalse(); walletSaved = true;
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32) } };
            var providers = new ProviderManager(null, dna); providers.RegisterProvider(local); providers.RegisterProvider(sui);
            providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            var constructor = typeof(WalletManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(value => value.GetParameters().Length == 4);
            sui.WalletManager = (WalletManager)constructor.Invoke(new object?[] { local, dna, providers, null });
            var minted = await sui.MintNFTAsync(new Core.Objects.NFT.Requests.MintWeb3NFTRequest { MintedByAvatarId = avatarId,
                NumberToMint = 1, Title = "Stored-avatar Sui NFT fixture", NFTStandardType = NFTStandardType.SuiObject,
                SendToAddressAfterMinting = issuer.Result.WalletAddressLegacy });
            if (minted.Result?.Web3NFT != null) { ownedAddress = minted.Result.Web3NFT.NFTTokenAddress; ownedId = minted.Result.Web3NFT.Id; }
            minted.IsError.Should().BeFalse(minted.Message); ownedAddress.Should().NotBeNullOrWhiteSpace();
            minted.Result.TransactionResult.Should().NotBeNullOrWhiteSpace();
            var loaded = await sui.LoadOnChainNFTDataAsync(ownedAddress!);
            loaded.IsError.Should().BeFalse(loaded.Message); loaded.Result.MintedByAvatarId.Should().Be(avatarId);
            loaded.Result.SendToAddressAfterMinting.Should().Be(issuer.Result.WalletAddressLegacy);
            (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[ProviderType.SuiOASIS][0].PrivateKey.Should().Be(encrypted);
        }
        finally
        {
            if (ownedAddress != null)
            {
                var burned = await sui.BurnNFTAsync(new Core.Objects.NFT.Requests.BurnWeb3NFTRequest { NFTTokenAddress = ownedAddress,
                    Web3NFTId = ownedId, BurntByAvatarId = avatarId, OwnerSeedPhrase = null, OwnerPublicKey = null, OwnerPrivateKey = null });
                burned.IsError.Should().BeFalse(burned.Message); burned.Result.TransactionResult.Should().NotBeNullOrWhiteSpace();
                (await sui.LoadOnChainNFTDataAsync(ownedAddress)).IsError.Should().BeTrue();
            }
            if (walletSaved) (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new())).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();
        }
    }

    [Fact]
    public async Task SharedAvatarSendSignsWithStoredWalletAndCommitsRealSuiTransfer()
    {
        string Required(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required; no skipped evidence.");
        var rpc = Required("OASIS_SUI_TEST_RPC"); new Uri(rpc).IsLoopback.Should().BeTrue();
        var root = Path.GetFullPath(Required("OASIS_HYPERDRIVE_TEST_ROOT")); Path.GetFileName(root).Should().Be("HyperDriveSui");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root,"store.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        using var fixture = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = fixture.RootElement;
        using var sui = new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!);
        var issuer = await sui.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);
        issuer.IsError.Should().BeFalse(issuer.Message);
        var recipient = await sui.GenerateKeyPairAsync(); recipient.IsError.Should().BeFalse(recipient.Message);
        var local = new LocalFileOASIS(storageDirectory:Path.Combine(root,"files"));
        (await local.ActivateProviderAsync()).IsError.Should().BeFalse();
        var avatarId = Guid.Parse("8a6a5eac-c63a-4a11-841c-512745bcd98a");
        var encrypted = Rijndael.Encrypt(issuer.Result.PrivateKey,new string('k',32),KeySize.Aes256);
        var wallet = new ProviderWallet { Id=Guid.NewGuid(),AvatarId=avatarId,IsDefaultWallet=true,ProviderType=ProviderType.SuiOASIS,
            WalletAddress=issuer.Result.WalletAddressLegacy,PublicKey=issuer.Result.PublicKey,PrivateKey=encrypted };
        try
        {
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId,new() { [ProviderType.SuiOASIS]=new List<IProviderWallet> { wallet } })).IsError.Should().BeFalse();
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode=HyperDriveModes.V2;
            dna.OASIS.StorageProviders=new StorageProviderSettings { LogSwitchingProviders=false };
            dna.OASIS.Security=new SecuritySettings { OASISProviderPrivateKeys=new EncryptionSettings { Rijndael256EncryptionEnabled=true,Rijndael256Key=new string('k',32) } };
            var providers = new ProviderManager(null,dna); providers.RegisterProvider(local); providers.RegisterProvider(sui);
            var selected=providers.SetAndActivateCurrentStorageProvider(local); selected.IsError.Should().BeFalse(selected.Message);
            var constructor=typeof(WalletManager).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).Single(c=>c.GetParameters().Length==4);
            var manager=(WalletManager)constructor.Invoke(new object?[] {local,dna,providers,null});
            var before=await sui.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy); before.IsError.Should().BeFalse(before.Message);
            var request=new SendWeb4TokenRequest { FromProvider=new(ProviderType.SuiOASIS),ToProvider=new(ProviderType.SuiOASIS),
                FromTokenAddress="0x2::sui::SUI",ToWalletAddress=recipient.Result.WalletAddressLegacy,Amount=0.001m };
            var sent=await manager.SendTokenAsync(avatarId,request); sent.IsError.Should().BeFalse(sent.Message);
            sent.Result.SendTransactionResult.Should().NotBeNullOrWhiteSpace();
            var after=await sui.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy); after.IsError.Should().BeFalse(after.Message);
            (after.Result-before.Result).Should().Be(0.001m);
            request.OwnerPrivateKey.Should().BeNull(); request.FromWalletAddress.Should().BeNull();
            (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[ProviderType.SuiOASIS][0].PrivateKey.Should().Be(encrypted);
        }
        finally
        {
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId,new())).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();
        }
    }
}

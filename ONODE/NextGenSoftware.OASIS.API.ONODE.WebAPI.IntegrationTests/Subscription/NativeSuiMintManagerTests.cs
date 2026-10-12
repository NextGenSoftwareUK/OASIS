using System.Reflection;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using NextGenSoftware.Utilities;
using IOFile = System.IO.File;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests.Subscription;

public sealed class NativeSuiMintManagerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefaultThreeEntryBatchMintsExactlyThreeAndPersistsRealMetadata(bool encryptedAvatar)
    {
        var rpc = Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC") ?? throw new InvalidOperationException("Retained local Sui RPC required.");
        Assert.True(new Uri(rpc).IsLoopback);
        using var fixture = JsonDocument.Parse(await IOFile.ReadAllTextAsync(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")
            ?? throw new InvalidOperationException("Existing Sui deployment required.")));
        var settings = fixture.RootElement;
        using var sui = new NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!, settings.GetProperty("privateKey").GetString()!, settings.GetProperty("storageObjectId").GetString()!);
        var owner = await sui.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);
        Assert.False(owner.IsError, owner.Message);
        using var retainedWallet = JsonDocument.Parse(await IOFile.ReadAllTextAsync(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT") + ".token-wallet.json"));
        Assert.Equal(settings.GetProperty("chainId").GetString(), retainedWallet.RootElement.GetProperty("chainId").GetString());
        var mintKey = encryptedAvatar ? retainedWallet.RootElement.GetProperty("wallet").GetProperty("privateKey").GetString()!
            : settings.GetProperty("privateKey").GetString()!;
        var signer = await sui.RestoreKeyPairAsync(mintKey);
        Assert.False(signer.IsError, signer.Message);
        if (encryptedAvatar) Assert.NotEqual(owner.Result.WalletAddressLegacy, signer.Result.WalletAddressLegacy);
        var root = Environment.GetEnvironmentVariable("OASIS_NATIVE_MINT_TEST_ROOT") ?? throw new InvalidOperationException("Fixed reusable owned mint-test directory required.");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(root, "mint-intent.json");
        Assert.False(IOFile.Exists(journal), "An unresolved prior mint intent exists; reconcile it before any new submission.");
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        Assert.False((await local.ActivateProviderAsync()).IsError);
        var before = await local.LoadAllHolonsAsync(HolonType.All);
        Assert.False(before.IsError, before.Message);
        var originalIds = before.Result.Select(item => item.Id).ToHashSet();
        var dna = new OASISDNA();
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
        dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings {
            Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32) } };
        var registry = new ProviderManager(null!, dna);
        registry.RegisterProvider(local); registry.RegisterProvider(sui);
        var field = typeof(ProviderManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)!;
        var original = field.GetValue(null);
        IWeb4NFT? minted = null;
        var actorId = encryptedAvatar ? Guid.NewGuid() : Guid.Empty;
        var actorSaved = false;
        var encryptedKey = Rijndael256.Rijndael.Encrypt(mintKey, new string('k', 32), Rijndael256.KeySize.Aes256);
        var finished = false;
        try
        {
            field.SetValue(null, registry);
            if (encryptedAvatar)
            {
                Assert.False(registry.SetAndActivateCurrentStorageProvider(local).IsError);
                Assert.False((await local.SaveAvatarAsync(new NextGenSoftware.OASIS.API.Core.Holons.Avatar {
                    Id = actorId, Username = "owned-native-mint-" + actorId.ToString("N"), Email = actorId.ToString("N") + "@example.invalid" })).IsError);
                actorSaved = true;
                Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(actorId, new() {
                    [ProviderType.SuiOASIS] = new List<IProviderWallet> { new NextGenSoftware.OASIS.API.Core.Objects.ProviderWallet {
                        Id = Guid.NewGuid(), AvatarId = actorId, IsDefaultWallet = true, ProviderType = ProviderType.SuiOASIS,
                        WalletAddress = signer.Result.WalletAddressLegacy, PublicKey = signer.Result.PublicKey, PrivateKey = encryptedKey,
                    } } })).IsError);
                var walletConstructor = typeof(WalletManager).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single(item => item.GetParameters().Length == 4);
                sui.WalletManager = (WalletManager)walletConstructor.Invoke(new object?[] { local, dna, registry, null });
                var balance = await sui.GetAccountBalanceAsync(signer.Result.WalletAddressLegacy);
                Assert.False(balance.IsError, balance.Message);
                if (balance.Result < 0.1m)
                {
                    var funded = await sui.SendTransactionAsync(owner.Result.WalletAddressLegacy, signer.Result.WalletAddressLegacy, 0.1m - balance.Result);
                    Assert.False(funded.IsError, funded.Message);
                }
            }
            var manager = new NFTManager(local, Guid.Empty, dna);
            var constructor = typeof(HolonManager).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null,
                new[] { typeof(IOASISStorageProvider), typeof(OASISDNA), typeof(ProviderManager) }, null)!;
            manager.Data = (HolonManager)constructor.Invoke(new object[] { local, dna, registry });
            var request = new MintWeb4NFTRequest {
                NumberToMint = 3, Amount = 3, Title = "Retained native manager oak", ImageUrl = "https://example.invalid/oak.png",
                OnChainProvider = new EnumValue<ProviderType>(ProviderType.SuiOASIS),
                OffChainProvider = new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),
                NFTStandardType = new EnumValue<NFTStandardType>(NFTStandardType.SuiObject),
                NFTOffChainMetaType = new EnumValue<NFTOffChainMetaType>(NFTOffChainMetaType.OASIS),
                SendToAddressAfterMinting = signer.Result.WalletAddressLegacy, MintedByAvatarId = actorId,
            };
            await IOFile.WriteAllTextAsync(journal, JsonSerializer.Serialize(new { state = "intent", chain = settings.GetProperty("chainId").GetString(), count = 3 }));
            var result = await manager.MintNftAsync(request);
            minted = result.Result;
            await IOFile.WriteAllTextAsync(journal, JsonSerializer.Serialize(new { state = "returned", error = result.IsError,
                assets = minted?.NewlyMintedWeb3NFTs?.Select(item => new { item.NFTTokenAddress, item.MintTransactionHash }).ToArray() }));
            Assert.False(result.IsError, result.Message);
            Assert.NotNull(minted);
            Assert.Equal(3, minted.NewlyMintedWeb3NFTs.Count);
            Assert.Equal(3, minted.NewlyMintedWeb3NFTs.Select(item => item.NFTTokenAddress).Distinct().Count());
            foreach (var nft in minted.NewlyMintedWeb3NFTs)
            {
                var status = await sui.GetTransactionStatusAsync(nft.MintTransactionHash);
                Assert.False(status.IsError, status.Message);
                Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed, status.Result);
                var loaded = await sui.LoadOnChainNFTDataAsync(nft.NFTTokenAddress);
                Assert.False(loaded.IsError, loaded.Message);
                Assert.Equal(request.Title, loaded.Result.Title);
                Assert.Equal(signer.Result.WalletAddressLegacy, loaded.Result.NFTMintedUsingWalletAddress);
            }
            var persisted = await manager.LoadWeb4NftAsync(minted.Id, ProviderType.LocalFileOASIS);
            Assert.False(persisted.IsError, persisted.Message);
            Assert.Equal(3, persisted.Result.Web3NFTs.Count);
            if (encryptedAvatar)
                Assert.Equal(encryptedKey, (await local.LoadProviderWalletsForAvatarByIdAsync(actorId)).Result[ProviderType.SuiOASIS].Single().PrivateKey);
            finished = true;
        }
        finally
        {
            var errors = new List<Exception>();
            async Task Cleanup(Func<Task> action) { try { await action(); } catch (Exception error) { errors.Add(error); } }
            if (minted?.NewlyMintedWeb3NFTs != null)
                foreach (var nft in minted.NewlyMintedWeb3NFTs.DistinctBy(item => item.NFTTokenAddress))
                    await Cleanup(async () => {
                        var burn = await sui.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = nft.NFTTokenAddress,
                            OwnerPrivateKey = mintKey, OwnerSeedPhrase = "", OwnerPublicKey = "" });
                        Assert.False(burn.IsError, burn.Message);
                    });
            if (actorSaved)
            {
                await Cleanup(async () => { Assert.False((await local.SaveProviderWalletsForAvatarByIdAsync(actorId, new())).IsError); });
                await Cleanup(async () => { Assert.False((await local.DeleteAvatarAsync(actorId, softDelete: false)).IsError); });
            }
            await Cleanup(async () => {
                var after = await local.LoadAllHolonsAsync(HolonType.All); Assert.False(after.IsError, after.Message);
                foreach (var id in after.Result.Select(item => item.Id).Except(originalIds))
                    Assert.False((await local.DeleteHolonAsync(id)).IsError);
            });
            field.SetValue(null, original);
            await Cleanup(async () => { Assert.False((await sui.DeActivateProviderAsync()).IsError); });
            await Cleanup(async () => { Assert.False((await local.DeActivateProviderAsync()).IsError); });
            if (finished && errors.Count == 0)
            {
                await IOFile.WriteAllTextAsync(Path.Combine(root, "mint-completed.json"), JsonSerializer.Serialize(new {
                    state = "verified-and-cleaned", metadataId = minted!.Id,
                    assets = minted.NewlyMintedWeb3NFTs.Select(item => new { item.NFTTokenAddress, item.MintTransactionHash }).ToArray(),
                }));
                IOFile.Delete(journal);
            }
            if (errors.Count > 0) throw new AggregateException("Owned native mint cleanup incomplete; retain and reconcile intent.", errors);
        }
    }
}

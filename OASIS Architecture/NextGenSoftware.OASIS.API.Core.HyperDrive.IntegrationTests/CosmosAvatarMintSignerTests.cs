using System.Reflection;
using System.Text.Json;
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

// Shared retained signer/escrow harness for Cosmos and Sui; no second wallet environment.
public sealed class CosmosAvatarMintSignerTests
{
    [Theory]
    [InlineData(false, false, false, false)] [InlineData(true, false, false, false)]
    [InlineData(false, true, false, false)] [InlineData(true, true, false, false)]
    [InlineData(false, true, true, false)] [InlineData(true, true, true, false)]
    [InlineData(false, true, false, true)] [InlineData(true, true, false, true)]
    [InlineData(false, true, true, true)] [InlineData(true, true, true, true)]
    [InlineData(false, false, false, true)] [InlineData(true, false, false, true)]
    public async Task TokenOperationsUseEncryptedAvatarDefaultWalletInsteadOfConfiguredOrFirstWallet(bool synchronous, bool escrow, bool viaManager, bool useSui)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/Use-ReusableTronRuntime.ps1"))) directory = directory.Parent;
        directory.Should().NotBeNull();
        var root = Path.Combine(directory!.FullName, "TestResults/HyperDriveCosmos"); Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var bridgeVariable = useSui ? "OASIS_SUI_SDK_BRIDGE" : "OASIS_COSMOS_SDK_BRIDGE";
        var stateVariable = useSui ? "OASIS_SUI_ESCROW_FIXTURE_STATE" : "OASIS_COSMOS_ESCROW_FIXTURE_STATE";
        var providerType = useSui ? ProviderType.SuiOASIS : ProviderType.CosmosBlockChainOASIS;
        var escrowAddress = useSui ? "0x" + new string('2', 64) : "fixture-contract";
        var previous = Environment.GetEnvironmentVariable(bridgeVariable);
        var previousState = Environment.GetEnvironmentVariable(stateVariable);
        var state = Path.Combine(root, "escrow-state.json");
        if (escrow) {
            File.Exists(state).Should().BeFalse("preserve unresolved prior fixture state");
            File.WriteAllText(state, JsonSerializer.Serialize(new { mode = "valid", submissions = 0, deposit = (object?)null }));
            Environment.SetEnvironmentVariable(stateVariable, state);
        }
        Environment.SetEnvironmentVariable(bridgeVariable, Path.Combine(directory.FullName, useSui
            ? "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests/Fixtures/unlock-readback-failure.mjs"
            : "Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests/Fixtures/nft-signer.mjs"));
        var local = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var settings = new CosmosBlockChainOASISProviderSettings { ContractAddress = "fixture-contract",
            IdBoundTokenEscrow = escrow ? new() { ContractAddress = "fixture-contract", CodeSha256 = new string('c', 64) } : null! };
        using var cosmos = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "different-configured-key", "fixture-contract",
            tokenEscrow: NextGenSoftware.OASIS.OASISBootLoader.CosmosEscrowConfiguration.Create(settings));
        using var sui = new API.Providers.SuiOASIS.SuiOASIS("http://127.0.0.1:1", "localnet", contractAddress: "0x1", privateKey: "different-configured-key",
            tokenEscrow: useSui && escrow ? NextGenSoftware.OASIS.OASISBootLoader.SuiEscrowConfiguration.Create(new() {
                IdBoundTokenEscrow = new() { EscrowObjectId = escrowAddress } }) : null);
        IOASISBlockchainStorageProvider chain = useSui ? sui : cosmos;
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
                new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, ProviderType = providerType,
                    IsDefaultWallet = false, WalletAddress = "decoy", PrivateKey = Rijndael256.Rijndael.Encrypt("decoy-key", new string('k', 32), Rijndael256.KeySize.Aes256) },
                new ProviderWallet { Id = Guid.NewGuid(), AvatarId = avatarId, ProviderType = providerType,
                    IsDefaultWallet = true, WalletAddress = "fixture-owner", PublicKey = "fixture-public", PrivateKey = encrypted } };
            (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new() { [providerType] = wallets })).IsError.Should().BeFalse(); saved = true;
            var dna = new OASISDNA(); dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
            dna.OASIS.StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false };
            dna.OASIS.Security = new SecuritySettings { OASISProviderPrivateKeys = new EncryptionSettings { Rijndael256EncryptionEnabled = true, Rijndael256Key = new string('k', 32) } };
            var providers = new ProviderManager(null, dna); providers.RegisterProvider(local); providers.RegisterProvider(chain);
            providers.SetAndActivateCurrentStorageProvider(local).IsError.Should().BeFalse();
            var constructor = typeof(WalletManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(value => value.GetParameters().Length == 4);
            var manager = (WalletManager)constructor.Invoke(new object?[] { local, dna, providers, null });
            if (useSui) sui.WalletManager = manager; else cosmos.WalletManager = manager;
            if (useSui && !escrow) {
                var created = synchronous ? sui.CreateTokenEscrowRegistry(avatarId) : await sui.CreateTokenEscrowRegistryAsync(avatarId);
                created.IsError.Should().BeFalse(created.Message);created.Result.EscrowObjectId.Should().Be(escrowAddress);
                created.Result.Creator.Should().Be("fixture-owner");created.Result.Protocol.Should().Be(1);
                created.Result.TransactionResult.Should().Be(new string('1',32));sui.IsProviderActivated.Should().BeFalse();
            }
            if(useSui){
                var mintToken=new MintWeb3TokenRequest {MintedByAvatarId=avatarId,Amount=1.23m,
                    MetaData=new(){{"TokenAddress","0x1::token::TOKEN"},{"MintToWalletAddress","fixture-owner"}}};
                var mintedToken=synchronous?sui.MintToken(mintToken):await sui.MintTokenAsync(mintToken);
                mintedToken.IsError.Should().BeFalse(mintedToken.Message);mintedToken.Result.TransactionResult.Should().Be(new string('1',32));
                var burnToken=new BurnWeb3TokenRequest {BurntByAvatarId=avatarId,Amount=1.23m,TokenAddress="0x1::token::TOKEN",
                    OwnerPrivateKey="",OwnerPublicKey="fixture-public",OwnerSeedPhrase=""};
                var burntToken=synchronous?sui.BurnToken(burnToken):await sui.BurnTokenAsync(burnToken);
                burntToken.IsError.Should().BeFalse(burntToken.Message);burntToken.Result.TransactionResult.Should().Be(new string('1',32));
            }
            if (!useSui) {
            var request=new MintWeb3TokenRequest { Symbol = "fixture-token", Amount = 1, MintedByAvatarId = avatarId };
            var minted = synchronous ? cosmos.MintToken(request):await cosmos.MintTokenAsync(request);
            minted.IsError.Should().BeFalse(minted.Message);
            // nft-signer.mjs is a controlled transport fixture, not native consensus evidence.
            // Its execute branch rejects any signer except the decrypted explicit default key.
            minted.Result.TransactionResult.Should().Be(new string('a',64));
            var nftRequest=new Core.Objects.NFT.Requests.MintWeb3NFTRequest { MintedByAvatarId=avatarId,
                NFTStandardType=NFTStandardType.OASISCosmWasm,Title="Stored wallet NFT",Price=12.5m,Discount=2m,RoyaltyPercentage=7,MemoText="mint memo" };
            var nft=synchronous?cosmos.MintNFT(nftRequest):await cosmos.MintNFTAsync(nftRequest);
            nft.IsError.Should().BeFalse(nft.Message);
            nft.Result.TransactionResult.Should().Be(new string('a',64));
            var locator=nft.Result.Web3NFT.NFTTokenAddress;
            var nftLock=new Core.Objects.NFT.Requests.LockWeb3NFTRequest {LockedByAvatarId=avatarId,NFTTokenAddress=locator};
            var nftLocked=synchronous?cosmos.LockNFT(nftLock):await cosmos.LockNFTAsync(nftLock);
            nftLocked.IsError.Should().BeFalse(nftLocked.Message);
            var nftUnlock=new Core.Objects.NFT.Requests.UnlockWeb3NFTRequest {UnlockedByAvatarId=avatarId,NFTTokenAddress=locator};
            var nftReleased=synchronous?cosmos.UnlockNFT(nftUnlock):await cosmos.UnlockNFTAsync(nftUnlock);
            nftReleased.IsError.Should().BeFalse(nftReleased.Message);
            var nftBurn=new Core.Objects.NFT.Requests.BurnWeb3NFTRequest {BurntByAvatarId=avatarId,NFTTokenAddress=locator,
                OwnerPrivateKey="",OwnerPublicKey="fixture-public",OwnerSeedPhrase=""};
            var nftBurned=synchronous?cosmos.BurnNFT(nftBurn):await cosmos.BurnNFTAsync(nftBurn);
            nftBurned.IsError.Should().BeFalse(nftBurned.Message);
            }
            if (escrow) {
                var id = Guid.Parse("11111111-2222-3333-4444-555555555555");
                var locking = new LockWeb4TokenRequest { LockedByAvatarId = avatarId, LockId = id, Amount = 1.23m,
                    TokenAddress = useSui ? "0x1::token::TOKEN" : "TREE", EscrowContractAddress = escrowAddress, FromWalletAddress = "fixture-owner",
                    ProviderType = new(providerType) };
                var locked = viaManager ? synchronous ? manager.LockToken(locking) : await manager.LockTokenAsync(locking)
                    : synchronous ? chain.LockToken(locking) : await chain.LockTokenAsync(locking);
                locked.IsError.Should().BeFalse(locked.Message);
                (useSui ? ((API.Providers.SuiOASIS.SuiIdBoundEscrowResponse)locked.Result).LockId : ((CosmosIdBoundEscrowResponse)locked.Result).LockId).Should().Be(id);
                var unlocking = new UnlockWeb4TokenRequest { UnlockedByAvatarId = avatarId, LockId = id,
                    TokenAddress = locking.TokenAddress, EscrowContractAddress = escrowAddress, ProviderType = new(providerType) };
                var released = viaManager ? synchronous ? manager.UnlockToken(unlocking) : await manager.UnlockTokenAsync(unlocking)
                    : synchronous ? chain.UnlockToken(unlocking) : await chain.UnlockTokenAsync(unlocking);
                released.IsError.Should().BeFalse(released.Message);
                (useSui ? ((API.Providers.SuiOASIS.SuiIdBoundEscrowResponse)released.Result).LockId : ((CosmosIdBoundEscrowResponse)released.Result).LockId).Should().Be(id);
                using var observed = JsonDocument.Parse(File.ReadAllText(state));
                observed.RootElement.GetProperty("submissions").GetInt32().Should().Be(2);
                observed.RootElement.GetProperty("deposit").GetProperty("released").GetBoolean().Should().BeTrue();
                (useSui ? sui.IsProviderActivated : cosmos.IsProviderActivated).Should().BeFalse();
            }
            (await local.LoadProviderWalletsForAvatarByIdAsync(avatarId)).Result[providerType][1].PrivateKey.Should().Be(encrypted);
            if (!useSui) {
            var absent=new MintWeb3TokenRequest { Symbol = "fixture-token", Amount = 1, MintedByAvatarId = Guid.NewGuid() };
            var missing = synchronous ? cosmos.MintToken(absent):await cosmos.MintTokenAsync(absent);
            missing.IsError.Should().BeTrue(); missing.Result.Should().BeNull();
            missing.Message.Should().Be("Exact requesting avatar signing wallet is required.");
            missing.Exception.Should().BeOfType<InvalidOperationException>();
            }
            if (escrow) File.Delete(state);
        }
        finally
        {
            Environment.SetEnvironmentVariable(bridgeVariable, previous);
            Environment.SetEnvironmentVariable(stateVariable, previousState);
            if (saved) (await local.SaveProviderWalletsForAvatarByIdAsync(avatarId, new())).IsError.Should().BeFalse();
            await local.DeActivateProviderAsync();
        }
    }
}

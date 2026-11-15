using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SqliteWalletSummaryTests
{
    [Fact]
    public async Task ReadsWalletSummaryFromNativeSqlite()
    {
        var folder = Directory.CreateTempSubdirectory("oasis-wallet-summary-");
        try
        {
            using var provider = new SQLLiteDBOASIS($"Data Source={Path.Combine(folder.FullName, "wallets.db")};Pooling=False");
            var activated = await provider.ActivateProviderAsync();
            Assert.False(activated.IsError, activated.Message);

            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "wallet-summary", Email = "wallet-summary@test.local", IsActive = true };
            var savedAvatar = await provider.SaveAvatarAsync(avatar);
            Assert.False(savedAvatar.IsError, savedAvatar.Message);

            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            var providers = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            providers.RegisterProvider(provider);
            var selected = await providers.SetAndActivateCurrentStorageProviderAsync(provider.ProviderType.Value);
            Assert.False(selected.IsError, selected.Message);
            var manager = new WalletManager(provider, dna, providers);

            var solana = new ProviderWallet { AvatarId = avatar.Id, WalletId = Guid.NewGuid(), WalletAddress = "solana-fixture",
                ProviderType = ProviderType.SolanaOASIS, IsDefaultWallet = true, Balance = 0 };
            var ethereum = new ProviderWallet { AvatarId = avatar.Id, WalletId = Guid.NewGuid(), WalletAddress = "ethereum-fixture",
                ProviderType = ProviderType.EthereumOASIS, Balance = 12 };
            var wallets = new Dictionary<ProviderType, List<IProviderWallet>>
            {
                [ProviderType.SolanaOASIS] = [solana],
                [ProviderType.EthereumOASIS] = [ethereum]
            };

            var saved = await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, wallets);
            Assert.False(saved.IsError, saved.Message);
            var summary = await manager.LoadWalletSummaryAsync(avatar.Id);
            Assert.False(summary.IsError, summary.Message);
            Assert.Equal(2, summary.Result.Count);
            Assert.Equal(0d, summary.Result.DefaultBalance);

            ethereum.IsDefaultWallet = true;
            saved = await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, wallets);
            Assert.False(saved.IsError, saved.Message);
            var conflicting = await manager.LoadWalletSummaryAsync(avatar.Id);
            Assert.True(conflicting.IsError);
            Assert.Null(conflicting.Result);

            saved = await provider.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, new());
            Assert.False(saved.IsError, saved.Message);
            var empty = await manager.LoadWalletSummaryAsync(avatar.Id);
            Assert.False(empty.IsError, empty.Message);
            Assert.Equal(0, empty.Result.Count);
            Assert.Null(empty.Result.DefaultBalance);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }
}

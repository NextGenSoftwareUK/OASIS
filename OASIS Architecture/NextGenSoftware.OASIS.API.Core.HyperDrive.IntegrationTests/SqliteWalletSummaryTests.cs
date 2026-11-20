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
    public async Task WalletBatchContextKeepsBothOwnersAtomicWhenSecondNativeWriteFails()
    {
        var folder=Directory.CreateTempSubdirectory("oasis-wallet-context-");
        var connectionString=$"Data Source={Path.Combine(folder.FullName,"wallets.db")};Pooling=False";
        try
        {
            using var provider=new SQLLiteDBOASIS(connectionString);
            var activated=await provider.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
            var first=Guid.NewGuid();var second=Guid.NewGuid();
            foreach(var id in new[]{first,second})
            {
                var saved=await provider.SaveAvatarAsync(new Avatar{Id=id,Username="wallet-"+id.ToString("N"),
                    Email=id.ToString("N")+"@example.test",IsActive=true});
                Assert.False(saved.IsError,saved.Message);
            }
            ProviderWallet Wallet(Guid owner,string address)=>new(){AvatarId=owner,WalletId=Guid.NewGuid(),
                ProviderType=ProviderType.SolanaOASIS,WalletAddress=address};
            var a=Wallet(first,"original-first");var b=Wallet(second,"original-second");
            var batch=new Dictionary<ProviderType,List<IProviderWallet>>{{ProviderType.SolanaOASIS,[a,b]}};
            var initial=await provider.SaveProviderWalletsAsync(batch);Assert.False(initial.IsError,initial.Message);
            async Task<Dictionary<string,string>> ReadNative()
            {
                using var connection=new Microsoft.Data.Sqlite.SqliteConnection(connectionString);await connection.OpenAsync();
                using var command=connection.CreateCommand();
                command.CommandText="SELECT AvatarId,WalletsJson FROM AvatarWalletStores ORDER BY AvatarId";
                using var reader=await command.ExecuteReaderAsync();var rows=new Dictionary<string,string>();
                while(await reader.ReadAsync())rows.Add(reader.GetString(0),reader.GetString(1));
                return rows;
            }
            var before=await ReadNative();Assert.Equal(2,before.Count);
            using(var connection=new Microsoft.Data.Sqlite.SqliteConnection(connectionString))
            {
                await connection.OpenAsync();using var command=connection.CreateCommand();
                // The owned GUID is a fixture literal; SQLite trigger DDL cannot bind parameters.
                command.CommandText="CREATE TRIGGER refuse_second_wallet BEFORE UPDATE ON AvatarWalletStores WHEN NEW.AvatarId='"+
                    second.ToString("D")+"' BEGIN SELECT RAISE(ABORT,'owned wallet rollback'); END;";
                await command.ExecuteNonQueryAsync();
            }
            a.WalletAddress="changed-first";b.WalletAddress="changed-second";
            var failed=await provider.SaveProviderWalletsAsync(batch);Assert.True(failed.IsError);
            Assert.False(failed.Result);Assert.False(failed.IsSaved);
            var after=await ReadNative();Assert.Equal(before.Count,after.Count);
            foreach(var row in before)Assert.Equal(row.Value,after[row.Key]);
            var loaded=await provider.LoadProviderWalletsForAvatarByIdAsync(first);
            Assert.False(loaded.IsError,loaded.Message);
            Assert.Equal("original-first",Assert.Single(loaded.Result[ProviderType.SolanaOASIS]).WalletAddress);
            Console.WriteLine("SQLite scoped wallet batch native rollback verified: two owners unchanged after second native write refusal.");
        }
        finally
        {
            Assert.StartsWith(Path.GetFullPath(Path.GetTempPath()),Path.GetFullPath(folder.FullName));
            Assert.StartsWith("oasis-wallet-context-",folder.Name);folder.Delete(true);
            Assert.False(Directory.Exists(folder.FullName));
            Console.WriteLine("Owned SQLite wallet context fixture cleanup verified.");
        }
    }

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

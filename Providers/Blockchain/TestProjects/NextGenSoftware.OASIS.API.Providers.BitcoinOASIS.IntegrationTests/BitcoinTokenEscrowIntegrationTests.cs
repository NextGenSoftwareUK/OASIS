using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using System.Reflection;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using WalletFileProvider = NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests;

[TestClass]
public sealed class BitcoinTokenEscrowIntegrationTests
{
    [TestMethod]
    public async Task NativeBitcoinEscrowConsumesOnlyTheAuthorizedDepositAndReturnsExactAmount()
    {
        var api = Required("OASIS_BITCOIN_REGTEST_API");
        var ord = Required("OASIS_BITCOIN_ORD_API");
        var miner = Required("OASIS_BITCOIN_ORD_MINER");
        var pool = Required("OASIS_BITCOIN_BRIDGE_POOL_ADDRESS");
        var source = Required("OASIS_BITCOIN_ORD_RECIPIENT");
        var core = new RPCClient(RPCCredentialString.Parse(Required("OASIS_BITCOIN_CORE_RPC_AUTH")),
            new Uri(Required("OASIS_BITCOIN_CORE_RPC_URL")), Network.RegTest);
        using var reader = new HttpClient();
        var fixture = Required("OASIS_BITCOIN_ORD_WALLET_FIXTURE");
        Directory.CreateDirectory(fixture);
        var wallets = new WalletFileProvider(storageDirectory: fixture);
        Assert.IsFalse((await wallets.ActivateProviderAsync()).IsError);
        using var journal = new SqliteProvider($"Data Source={Path.Combine(fixture, "escrow.db")};Pooling=False");
        Assert.IsFalse((await journal.ActivateProviderAsync()).IsError);

        var dna = new OASISDNA();
        dna.OASIS.HyperDriveMode = HyperDriveModes.V2;
        dna.OASIS.StorageProviders = new StorageProviderSettings();
        var providers = new ProviderManager(null, dna)
            { IsAutoReplicationEnabled = false, IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providers.RegisterProvider(wallets);
        providers.RegisterProvider(journal);
        Assert.IsFalse(providers.SetAndActivateCurrentStorageProvider(wallets).IsError);
        var constructor = typeof(WalletManager).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(candidate => candidate.GetParameters().Length == 4);
        var manager = (WalletManager)constructor.Invoke(new object?[] { wallets, dna, providers, null });
        var receipts = new HolonManager(journal, dna, providers);
        var avatar = Guid.NewGuid();
        var wallet = new ProviderWallet
        {
            Id = Guid.NewGuid(), AvatarId = avatar, IsDefaultWallet = true,
            WalletAddress = source, ProviderType = ProviderType.BitcoinOASIS
        };
        var saved = await wallets.SaveProviderWalletsForAvatarByIdAsync(avatar,
            new() { [ProviderType.BitcoinOASIS] = new List<IProviderWallet> { wallet } });
        Assert.IsFalse(saved.IsError, saved.Message);

        var poolScalar = new byte[32];
        poolScalar[^1] = 1;
        var sourceScalar = new byte[32];
        sourceScalar[^1] = 2;
        using var poolKey = new Key(poolScalar);
        using var sourceKey = new Key(sourceScalar);
        using var provider = new BitcoinOASIS(api, "regtest", poolKey.GetWif(Network.RegTest).ToString(), manager, 2m, receipts);
        Assert.IsInstanceOfType(provider, typeof(IOASISIdBoundTokenEscrowProvider));
        var active = await provider.ActivateProviderAsync();
        Assert.IsFalse(active.IsError, active.Message);

        var asset = Guid.NewGuid();
        var lockId = Guid.NewGuid();
        var locking = new LockWeb3TokenRequest
        {
            LockId = lockId, Web3TokenId = asset, LockedByAvatarId = avatar,
            TokenAddress = "BTC", Amount = 0.00025m, EscrowContractAddress = pool,
            FromWalletAddress = source, FromWalletPrivateKey = sourceKey.GetWif(Network.RegTest).ToString()
        };
        var deposit = await provider.LockTokenAsync(locking);
        Assert.IsFalse(deposit.IsError, deposit.Message);
        var depositId = uint256.Parse(deposit.Result.TransactionResult);
        var onChainDeposit = await core.GetRawTransactionAsync(depositId);
        Assert.AreEqual(25_000L, onChainDeposit.Outputs[0].Value.Satoshi);
        var lockRetry = await provider.LockTokenAsync(locking);
        Assert.IsFalse(lockRetry.IsError, lockRetry.Message);
        Assert.AreEqual(depositId.ToString(), lockRetry.Result.TransactionResult);
        locking.Amount = 0.00026m;
        Assert.IsTrue((await provider.LockTokenAsync(locking)).IsError);

        var unlocking = new UnlockWeb3TokenRequest
        {
            LockId = lockId, Web3TokenId = asset, UnlockedByAvatarId = avatar,
            TokenAddress = "BTC", EscrowContractAddress = pool
        };
        Assert.IsTrue((await provider.UnlockTokenAsync(unlocking)).IsError);
        await core.SendCommandAsync("generatetoaddress", 1, miner);
        var tip = (await core.GetBestBlockHashAsync()).ToString();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if ((await reader.GetStringAsync(api + "/blocks/tip/hash")).Trim() == tip &&
                (await reader.GetStringAsync(ord + "/r/blockhash")).Trim().Trim('"') == tip)
                break;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        unlocking.UnlockedByAvatarId = Guid.NewGuid();
        Assert.IsTrue((await provider.UnlockTokenAsync(unlocking)).IsError);
        Assert.AreEqual(0, (await core.GetRawMempoolAsync()).Length);
        unlocking.UnlockedByAvatarId = avatar;
        var released = await provider.UnlockTokenAsync(unlocking);
        Assert.IsFalse(released.IsError, released.Message);
        var releaseId = uint256.Parse(released.Result.TransactionResult);
        var payout = await core.GetRawTransactionAsync(releaseId);
        Assert.AreEqual(new OutPoint(depositId, 0), payout.Inputs[0].PrevOut);
        Assert.AreEqual(25_000L, payout.Outputs[0].Value.Satoshi);
        Assert.AreEqual(BitcoinAddress.Create(source, Network.RegTest).ScriptPubKey, payout.Outputs[0].ScriptPubKey);
        var releaseRetry = await provider.UnlockTokenAsync(unlocking);
        Assert.IsFalse(releaseRetry.IsError, releaseRetry.Message);
        Assert.AreEqual(releaseId.ToString(), releaseRetry.Result.TransactionResult);
        await core.SendCommandAsync("generatetoaddress", 1, miner);
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"The owned regtest fixture must configure {name}.");
}

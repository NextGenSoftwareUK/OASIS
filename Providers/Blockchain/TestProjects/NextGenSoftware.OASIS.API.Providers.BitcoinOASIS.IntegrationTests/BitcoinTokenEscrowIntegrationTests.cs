using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using Key = NBitcoin.Key;
using NBitcoin.RPC;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Text.Json;
using NextGenSoftware.OASIS.API.DNA;
using System.Reflection;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using WalletFileProvider = NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests;

[TestClass]
public sealed class BitcoinTokenEscrowIntegrationTests
{
    [TestMethod]
    public Task NativeBitcoinEscrowConsumesOnlyTheAuthorizedDepositAndReturnsExactAmount() =>
        VerifyEscrowLifecycleAsync("BTC");

    [TestMethod]
    public Task NativeRuneEscrowConsumesOnlyTheAuthorizedDepositAndReturnsExactUnits() =>
        VerifyEscrowLifecycleAsync(Required("OASIS_BITCOIN_RUNE_ID"));

    private static async Task VerifyEscrowLifecycleAsync(string token)
    {
        var api = Required("OASIS_BITCOIN_REGTEST_API");
        var ord = Required("OASIS_BITCOIN_ORD_API");
        var miner = Required("OASIS_BITCOIN_ORD_MINER");
        var pool = Required("OASIS_BITCOIN_BRIDGE_POOL_ADDRESS");
        var source = Required("OASIS_BITCOIN_ORD_RECIPIENT");
        var core = new RPCClient(Required("OASIS_BITCOIN_CORE_RPC_AUTH"),
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
        providers.RegisterProvider(provider);

        var rune = token != "BTC";
        if (rune)
        {
            var minted = await provider.MintTokenAsync(new MintWeb3TokenRequest
            {
                TokenAddress = token, Amount = 7.25m, ToWalletAddress = source
            });
            Assert.IsFalse(minted.IsError, minted.Message);
            await MineAndWaitAsync(core, reader, miner, api, ord);
        }

        var asset = Guid.NewGuid();
        var lockId = Guid.NewGuid();
        var amount = rune ? 2.5m : 0.00025m;
        var depositOutput = rune ? 1 : 0;
        var locking = new LockWeb4TokenRequest
        {
            ProviderType = new(ProviderType.BitcoinOASIS),
            LockId = lockId, Web3TokenId = asset, LockedByAvatarId = avatar,
            TokenAddress = token, Amount = amount, EscrowContractAddress = pool,
            FromWalletAddress = source, FromWalletPrivateKey = sourceKey.GetWif(Network.RegTest).ToString()
        };
        var deposit = await manager.LockTokenAsync(locking);
        Assert.IsFalse(deposit.IsError, deposit.Message);
        var depositId = uint256.Parse(deposit.Result.TransactionResult);
        var onChainDeposit = await core.GetRawTransactionAsync(depositId);
        if (!rune)
            Assert.AreEqual(25_000L, onChainDeposit.Outputs[depositOutput].Value.Satoshi);
        var lockRetry = await manager.LockTokenAsync(locking);
        Assert.IsFalse(lockRetry.IsError, lockRetry.Message);
        Assert.AreEqual(depositId.ToString(), lockRetry.Result.TransactionResult);
        locking.Amount = amount + 0.01m;
        Assert.IsTrue((await manager.LockTokenAsync(locking)).IsError);

        var unlocking = new UnlockWeb4TokenRequest
        {
            ProviderType = new(ProviderType.BitcoinOASIS),
            LockId = lockId, Web3TokenId = asset, UnlockedByAvatarId = avatar,
            TokenAddress = token, EscrowContractAddress = pool
        };
        Assert.IsTrue((await manager.UnlockTokenAsync(unlocking)).IsError);
        await MineAndWaitAsync(core, reader, miner, api, ord);
        if (rune)
            await AssertRuneOutputAsync(reader, ord, depositId, depositOutput, 250);

        unlocking.UnlockedByAvatarId = Guid.NewGuid();
        Assert.IsTrue((await manager.UnlockTokenAsync(unlocking)).IsError);
        Assert.AreEqual(0, (await core.GetRawMempoolAsync()).Length);
        unlocking.UnlockedByAvatarId = avatar;
        var released = await manager.UnlockTokenAsync(unlocking);
        Assert.IsFalse(released.IsError, released.Message);
        var releaseId = uint256.Parse(released.Result.TransactionResult);
        var payout = await core.GetRawTransactionAsync(releaseId);
        Assert.AreEqual(new OutPoint(depositId, (uint)depositOutput), payout.Inputs[0].PrevOut);
        Assert.AreEqual(onChainDeposit.Outputs[depositOutput].Value.Satoshi, payout.Outputs[0].Value.Satoshi);
        Assert.AreEqual(BitcoinAddress.Create(source, Network.RegTest).ScriptPubKey, payout.Outputs[0].ScriptPubKey);
        var releaseRetry = await manager.UnlockTokenAsync(unlocking);
        Assert.IsFalse(releaseRetry.IsError, releaseRetry.Message);
        Assert.AreEqual(releaseId.ToString(), releaseRetry.Result.TransactionResult);
        await MineAndWaitAsync(core, reader, miner, api, ord);
        if (rune)
            await AssertRuneOutputAsync(reader, ord, releaseId, 0, 250);
    }

    private static async Task MineAndWaitAsync(RPCClient core, HttpClient reader, string miner, string api, string ord)
    {
        await core.SendCommandAsync("generatetoaddress", 1, miner);
        var tip = (await core.GetBestBlockHashAsync()).ToString();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if ((await reader.GetStringAsync(api + "/blocks/tip/hash")).Trim() == tip &&
                (await reader.GetStringAsync(ord + "/r/blockhash")).Trim().Trim('"') == tip)
                return;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        Assert.Fail("Bitcoin Core, Esplora and ord did not converge on the expected chain tip.");
    }

    private static async Task AssertRuneOutputAsync(HttpClient reader, string ord, uint256 txId, int output, int units)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ord}/output/{txId}:{output}");
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await reader.SendAsync(request);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var entries = document.RootElement.GetProperty("runes").EnumerateObject().ToArray();
        Assert.AreEqual(1, entries.Length);
        Assert.AreEqual(units, entries[0].Value.GetProperty("amount").GetInt32());
    }

    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"The owned regtest fixture must configure {name}.");
}

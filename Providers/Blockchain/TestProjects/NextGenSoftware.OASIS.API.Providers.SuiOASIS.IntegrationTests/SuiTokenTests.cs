using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiTokenTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real Sui test setting {name} is required.");

    [TestMethod]
    public async Task ProviderCommitsTokenMintTransferEscrowUnlockAndBurnWithAuthorization()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        var path = Required("OASIS_SUI_TEST_DEPLOYMENT");
        using var deployment = JsonDocument.Parse(File.ReadAllText(path));
        using var walletCache = JsonDocument.Parse(File.ReadAllText(path + ".token-wallet.json"));
        var settings = deployment.RootElement;
        Assert.AreEqual(settings.GetProperty("chainId").GetString(), walletCache.RootElement.GetProperty("chainId").GetString());
        var recipient = walletCache.RootElement.GetProperty("wallet");
        var recipientAddress = recipient.GetProperty("address").GetString()!;
        SuiOASIS Provider(string key) => new(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!, key, settings.GetProperty("storageObjectId").GetString()!,
            settings.GetProperty("currencyObjectId").GetString()!);
        using var issuer = Provider(settings.GetProperty("privateKey").GetString()!);
        using var holder = Provider(recipient.GetProperty("privateKey").GetString()!);
        var issuerKey = await issuer.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);
        Assert.IsFalse(issuerKey.IsError, issuerKey.Message);
        var issuerAddress = issuerKey.Result.WalletAddressLegacy;
        var coinType = settings.GetProperty("coinType").GetString()!;
        async Task<decimal> Balance(SuiOASIS provider, string address, bool locked = false)
        {
            var result = await provider.GetTokenBalanceAsync(address, coinType, locked);
            Assert.IsFalse(result.IsError, result.Message);
            return result.Result;
        }
        async Task Burn(SuiOASIS provider) {
            var result = await provider.BurnTokenAsync(new BurnWeb3TokenRequest
            { TokenAddress = coinType, OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = "" });
            Assert.IsFalse(result.IsError, result.Message);
        }
        // This retained synthetic fixture is exclusively used for token evidence.
        foreach (var (provider, address) in new[] { (issuer, issuerAddress), (holder, recipientAddress) })
        {
            if (await Balance(provider, address, true) > 0)
                Assert.IsFalse((await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = coinType })).IsError);
            if (await Balance(provider, address) > 0) await Burn(provider);
        }
        try
        {
            var minted = await issuer.MintTokenAsync(new MintWeb3TokenRequest { Amount = 4m,
                MetaData = new Dictionary<string, string> { ["TokenAddress"] = coinType, ["MintToWalletAddress"] = issuerAddress } });
            Assert.IsFalse(minted.IsError, minted.Message);
            Assert.AreEqual(BridgeTransactionStatus.Completed, (await issuer.GetTransactionStatusAsync(minted.Result.TransactionResult)).Result);
            Assert.AreEqual(4m, await Balance(issuer, issuerAddress));
            var forbidden = await holder.MintTokenAsync(new MintWeb3TokenRequest { Amount = 1m,
                MetaData = new Dictionary<string, string> { ["TokenAddress"] = coinType, ["MintToWalletAddress"] = recipientAddress } });
            Assert.IsTrue(forbidden.IsError);
            StringAssert.Contains(forbidden.Message, "MoveAbort");
            var sent = await issuer.SendTokenAsync(new SendWeb3TokenRequest { FromTokenAddress = coinType,
                FromWalletAddress = issuerAddress, ToWalletAddress = recipientAddress, Amount = 1m });
            Assert.IsFalse(sent.IsError, sent.Message);
            Assert.AreEqual(1m, await Balance(holder, recipientAddress));
            var locked = await holder.LockTokenAsync(new LockWeb3TokenRequest { TokenAddress = coinType, FromWalletAddress = recipientAddress });
            Assert.IsFalse(locked.IsError, locked.Message);
            Assert.AreEqual(0m, await Balance(holder, recipientAddress));
            Assert.AreEqual(1m, await Balance(holder, recipientAddress, true));
            var cannotSpend = await holder.SendTokenAsync(new SendWeb3TokenRequest { FromTokenAddress = coinType,
                FromWalletAddress = recipientAddress, ToWalletAddress = issuerAddress, Amount = 1m });
            Assert.IsTrue(cannotSpend.IsError);
            var unlocked = await holder.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = coinType });
            Assert.IsFalse(unlocked.IsError, unlocked.Message);
            Assert.AreEqual(1m, await Balance(holder, recipientAddress));
            Assert.AreEqual(0m, await Balance(holder, recipientAddress, true));
            await Burn(holder);
            Assert.AreEqual(0m, await Balance(holder, recipientAddress));
            var wrongAsset = await issuer.GetTokenBalanceAsync(issuerAddress, "0x2::sui::SUI");
            Assert.IsTrue(wrongAsset.IsError);
            var precision = await issuer.SendTokenAsync(new SendWeb3TokenRequest { FromTokenAddress = coinType,
                FromWalletAddress = issuerAddress, ToWalletAddress = recipientAddress, Amount = 0.0000000001m });
            Assert.IsTrue(precision.IsError);
            var wrongSigner = await issuer.SendTokenAsync(new SendWeb3TokenRequest { FromTokenAddress = coinType,
                FromWalletAddress = issuerAddress, ToWalletAddress = recipientAddress, Amount = 1m,
                OwnerPrivateKey = recipient.GetProperty("privateKey").GetString() });
            Assert.IsTrue(wrongSigner.IsError);
            StringAssert.Contains(wrongSigner.Message, "does not own sender");
            var wrongPublicKey = await issuer.SendTokenAsync(new SendWeb3TokenRequest { FromTokenAddress = coinType,
                FromWalletAddress = issuerAddress, ToWalletAddress = recipientAddress, Amount = 1m, OwnerPublicKey = "not-this-key" });
            Assert.IsTrue(wrongPublicKey.IsError);
            StringAssert.Contains(wrongPublicKey.Message, "does not match");
            var insufficient = await issuer.SendTokenAsync(new SendWeb3TokenRequest { FromTokenAddress = coinType,
                FromWalletAddress = issuerAddress, ToWalletAddress = recipientAddress, Amount = 100m });
            Assert.IsTrue(insufficient.IsError);
        }
        finally
        {
            foreach (var (provider, address) in new[] { (issuer, issuerAddress), (holder, recipientAddress) })
            {
                if (await Balance(provider, address, true) > 0)
                    Assert.IsFalse((await provider.UnlockTokenAsync(new UnlockWeb3TokenRequest { TokenAddress = coinType })).IsError);
                if (await Balance(provider, address) > 0) await Burn(provider);
            }
        }
    }
}

using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiBridgeTests
{
    [TestMethod]
    public async Task CustodialBridgeMovesActualFundsAndOriginalNftWithBoundSourceReceipt()
    {
        var rpc = Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC") ?? throw new InvalidOperationException("Real Sui RPC is required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        using var deployment = JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")
            ?? throw new InvalidOperationException("Real Sui deployment is required.")));
        var settings = deployment.RootElement;
        var package = settings.GetProperty("packageAddress").GetString()!;
        var custodyKey = settings.GetProperty("privateKey").GetString()!;
        using var provider = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            package, custodyKey, settings.GetProperty("storageObjectId").GetString()!);
        var custody = await provider.RestoreKeyPairAsync(custodyKey);
        var user = await provider.GenerateKeyPairAsync();
        var other = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(custody.IsError, custody.Message);
        Assert.IsFalse(user.IsError, user.Message);
        Assert.IsFalse(other.IsError, other.Message);
        await SuiNativeTransferTests.FundAsync(user.Result.WalletAddressLegacy);
        var poolBefore = await provider.GetAccountBalanceAsync(custody.Result.WalletAddressLegacy);
        Assert.IsFalse(poolBefore.IsError, poolBefore.Message);
        var withdrawn = await provider.WithdrawAsync(0.5m, user.Result.WalletAddressLegacy, user.Result.PrivateKey);
        Assert.IsFalse(withdrawn.IsError, withdrawn.Message);
        Assert.IsTrue(withdrawn.Result.IsSuccessful);
        Assert.AreEqual(BridgeTransactionStatus.Completed, withdrawn.Result.Status);
        var poolAfter = await provider.GetAccountBalanceAsync(custody.Result.WalletAddressLegacy);
        Assert.IsFalse(poolAfter.IsError, poolAfter.Message);
        Assert.AreEqual(0.5m, poolAfter.Result - poolBefore.Result);
        var receiverBefore = await provider.GetAccountBalanceAsync(user.Result.WalletAddressLegacy);
        Assert.IsFalse(receiverBefore.IsError, receiverBefore.Message);
        var deposited = await provider.DepositAsync(0.5m, user.Result.WalletAddressLegacy);
        Assert.IsFalse(deposited.IsError, deposited.Message);
        Assert.AreEqual(BridgeTransactionStatus.Completed, (await provider.GetTransactionStatusAsync(deposited.Result.TransactionId)).Result);
        var receiverAfter = await provider.GetAccountBalanceAsync(user.Result.WalletAddressLegacy);
        Assert.IsFalse(receiverAfter.IsError, receiverAfter.Message);
        Assert.AreEqual(0.5m, receiverAfter.Result - receiverBefore.Result);
        Assert.IsTrue((await provider.WithdrawAsync(0.5m, user.Result.WalletAddressLegacy, other.Result.PrivateKey)).IsError);
        Assert.IsTrue((await provider.DepositAsync(0.0000000001m, user.Result.WalletAddressLegacy)).IsError);
        Assert.IsTrue((await provider.DepositAsync(100000m, user.Result.WalletAddressLegacy)).IsError);
        using (var unconfigured = new SuiOASIS(rpc, "localnet"))
            Assert.IsTrue((await unconfigured.DepositAsync(1m, user.Result.WalletAddressLegacy)).IsError);

        var minted = await provider.MintNFTAsync(new MintWeb3NFTRequest { Title = "Bridge original NFT",
            SendToAddressAfterMinting = user.Result.WalletAddressLegacy });
        Assert.IsFalse(minted.IsError, minted.Message);
        var id = minted.Result.Web3NFT.NFTTokenAddress;
        var ownerKey = user.Result.PrivateKey;
        try
        {
            Assert.IsTrue((await provider.WithdrawNFTAsync(package, id, user.Result.WalletAddressLegacy, other.Result.PrivateKey)).IsError);
            var nftWithdrawal = await provider.WithdrawNFTAsync(package, id, user.Result.WalletAddressLegacy, user.Result.PrivateKey);
            Assert.IsFalse(nftWithdrawal.IsError, nftWithdrawal.Message);
            ownerKey = custodyKey;
            var inCustody = await provider.LoadOnChainNFTDataAsync(id);
            Assert.IsFalse(inCustody.IsError, inCustody.Message);
            Assert.AreEqual(custody.Result.WalletAddressLegacy, inCustody.Result.SendToAddressAfterMinting);
            Assert.IsTrue((await provider.DepositNFTAsync(package, id, user.Result.WalletAddressLegacy, withdrawn.Result.TransactionId)).IsError,
                "An unrelated native receipt must not authorize an NFT release.");
            var nftDeposit = await provider.DepositNFTAsync(package, id, user.Result.WalletAddressLegacy, nftWithdrawal.Result.TransactionId);
            Assert.IsFalse(nftDeposit.IsError, nftDeposit.Message);
            ownerKey = user.Result.PrivateKey;
            var returned = await provider.LoadOnChainNFTDataAsync(id);
            Assert.IsFalse(returned.IsError, returned.Message);
            Assert.AreEqual(minted.Result.Web3NFT.Id, returned.Result.Id);
            Assert.AreEqual(user.Result.WalletAddressLegacy, returned.Result.SendToAddressAfterMinting);
            Assert.IsTrue((await provider.DepositNFTAsync(package, id, user.Result.WalletAddressLegacy, nftWithdrawal.Result.TransactionId)).IsError);
            var secondWithdrawal = await provider.WithdrawNFTAsync(package, id, user.Result.WalletAddressLegacy, user.Result.PrivateKey);
            Assert.IsFalse(secondWithdrawal.IsError, secondWithdrawal.Message);
            ownerKey = custodyKey;
            Assert.IsTrue((await provider.DepositNFTAsync(package, id, user.Result.WalletAddressLegacy, nftWithdrawal.Result.TransactionId)).IsError,
                "An old receipt stays invalid even after the same NFT returns to custody.");
            var released = await provider.DepositNFTAsync(package, id, user.Result.WalletAddressLegacy, secondWithdrawal.Result.TransactionId);
            Assert.IsFalse(released.IsError, released.Message);
            ownerKey = user.Result.PrivateKey;
        }
        finally
        {
            var burned = await provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = id,
                OwnerPrivateKey = ownerKey, OwnerPublicKey = "", OwnerSeedPhrase = "" });
            Assert.IsFalse(burned.IsError, burned.Message);
        }
    }
}

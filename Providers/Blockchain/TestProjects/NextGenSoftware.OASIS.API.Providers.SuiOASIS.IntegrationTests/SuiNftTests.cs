using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiNftTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real Sui setting {name} is required.");

    [TestMethod]
    public async Task ProviderMintsBatchReadsMetadataTransfersAndBurnsRealObjects()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        using var deployment = JsonDocument.Parse(File.ReadAllText(Required("OASIS_SUI_TEST_DEPLOYMENT")));
        var settings = deployment.RootElement;
        using var provider = new SuiOASIS(rpc, "localnet", settings.GetProperty("chainId").GetString()!,
            settings.GetProperty("packageAddress").GetString()!, settings.GetProperty("privateKey").GetString()!,
            settings.GetProperty("storageObjectId").GetString()!);
        var owner = await provider.RestoreKeyPairAsync(settings.GetProperty("privateKey").GetString()!);
        Assert.IsFalse(owner.IsError, owner.Message);
        var recipient = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(recipient.IsError, recipient.Message);
        await SuiNativeTransferTests.FundAsync(recipient.Result.WalletAddressLegacy);
        var minted = await provider.MintNFTAsync(new MintWeb3NFTRequest
        {
            Title = "Sui oak 🌳", Description = "Real serialized metadata", NumberToMint = 2,
            MetaData = new Dictionary<string, string> { ["park"] = "local-chain" },
            SendToAddressAfterMinting = owner.Result.WalletAddressLegacy
        });
        Assert.IsFalse(minted.IsError, minted.Message);
        Assert.AreEqual(2, minted.Result.Web3NFTs.Count);
        var keys = minted.Result.Web3NFTs.ToDictionary(item => item.NFTTokenAddress, _ => settings.GetProperty("privateKey").GetString()!);
        try
        {
            var id = minted.Result.Web3NFT.NFTTokenAddress;
            var loaded = await provider.LoadOnChainNFTDataAsync(id);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual("Sui oak 🌳", loaded.Result.Title);
            Assert.AreEqual("local-chain", loaded.Result.MetaData["park"]);
            var sent = await provider.SendNFTAsync(new SendWeb3NFTRequest
            {
                TokenId = id, FromWalletAddress = owner.Result.WalletAddressLegacy,
                ToWalletAddress = recipient.Result.WalletAddressLegacy, Amount = 1m
            });
            Assert.IsFalse(sent.IsError, sent.Message);
            keys[id] = recipient.Result.PrivateKey;
            Assert.AreEqual(recipient.Result.WalletAddressLegacy, sent.Result.Web3NFT.SendToAddressAfterMinting);
            var unauthorized = await provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = id, OwnerPrivateKey = "", OwnerPublicKey = "", OwnerSeedPhrase = "" });
            Assert.IsTrue(unauthorized.IsError, "Previous owner cannot burn the transferred object.");
        }
        finally
        {
            foreach (var (id, key) in keys)
            {
                var burned = await provider.BurnNFTAsync(new BurnWeb3NFTRequest { NFTTokenAddress = id, OwnerPrivateKey = key, OwnerPublicKey = "", OwnerSeedPhrase = "" });
                Assert.IsFalse(burned.IsError, burned.Message);
                Assert.IsTrue((await provider.LoadOnChainNFTDataAsync(id)).IsError);
            }
        }
    }
}

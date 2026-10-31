using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public class SuiSdkWalletTests
{
    [DataTestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public async Task NonpositiveMintCountsFailBeforeSignerOrRecipientResolution(int count)
    {
        using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet");
        var rejected = await provider.MintNFTAsync(new Core.Objects.NFT.Requests.MintWeb3NFTRequest
        {
            NumberToMint = count,
            MintedByAvatarId = Guid.NewGuid(),
            SendToAvatarAfterMintingId = Guid.NewGuid()
        });
        Assert.IsTrue(rejected.IsError);
        Assert.IsInstanceOfType<ArgumentException>(rejected.Exception);
        StringAssert.Contains(rejected.Message, "Positive NFT mint count is required.");
        Assert.IsNull(rejected.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task NftEconomicAndProviderOptionsAreValidatedBeforeNetworkOrSigning()
    {
        using var provider = new SuiOASIS("http://127.0.0.1:1", "localnet");
        foreach (var request in new[]
        {
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { Price = -1 },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { Discount = -1 },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { RoyaltyPercentage = 101 },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { RoyaltyPercentage = -1 },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { SaleStartDate = DateTime.UnixEpoch.AddDays(1), SaleEndDate = DateTime.UnixEpoch },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { OnChainProvider = Core.Enums.ProviderType.EthereumOASIS },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { NFTStandardType = Core.Enums.NFTStandardType.ERC721 },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { NFTStandardType = Core.Enums.NFTStandardType.ERC1155 },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { NFTStandardType = Core.Enums.NFTStandardType.SPL },
            new Core.Objects.NFT.Requests.MintWeb3NFTRequest { NFTStandardType = (Core.Enums.NFTStandardType)999 }
        })
        {
            var rejected = await provider.MintNFTAsync(request);
            Assert.IsTrue(rejected.IsError);
            Assert.IsInstanceOfType<ArgumentException>(rejected.Exception);
            Assert.IsNull(rejected.Result);
        }
    }

    [TestMethod]
    public async Task CanonicalWalletCanBeRecoveredByOfficialSdkWithoutNodeActivation()
    {
        var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", chainId: "genesis-id-is-not-a-key");
        var generated = await provider.GenerateKeyPairAsync();
        Assert.IsFalse(generated.IsError, generated.Message);
        Assert.IsNotNull(generated.Result);
        StringAssert.StartsWith(generated.Result.PrivateKey, "suiprivkey1");
        Assert.AreEqual(66, generated.Result.WalletAddressLegacy.Length);
        var restored = await provider.RestoreKeyPairAsync(generated.Result.PrivateKey);
        Assert.IsFalse(restored.IsError, restored.Message);
        Assert.AreEqual(generated.Result.WalletAddressLegacy, restored.Result.WalletAddressLegacy);
        Assert.AreEqual(generated.Result.PublicKey, restored.Result.PublicKey);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task ChainLabelCannotBeAcceptedAsSigningCredentials()
    {
        var provider = new SuiOASIS("http://127.0.0.1:1", "localnet");
        var invalid = await provider.RestoreKeyPairAsync("mainnet");
        Assert.IsTrue(invalid.IsError);
        Assert.IsNotNull(invalid.Exception);
        Assert.IsNull(invalid.Result);
    }

    [TestMethod]
    public async Task AccountMnemonicRoundTripsThroughOfficialSuiDerivation()
    {
        var provider = new SuiOASIS("http://127.0.0.1:1", "localnet");
        var created = await provider.CreateAccountAsync();
        Assert.IsFalse(created.IsError, created.Message);
        Assert.AreEqual(24, created.Result.SeedPhrase.Split(' ').Length);
        var restored = await provider.RestoreAccountAsync(created.Result.SeedPhrase);
        Assert.IsFalse(restored.IsError, restored.Message);
        Assert.AreEqual(created.Result.PrivateKey, restored.Result.PrivateKey);
        Assert.AreEqual(created.Result.PublicKey, restored.Result.PublicKey);
        var invalid = await provider.RestoreAccountAsync("not a mnemonic");
        Assert.IsTrue(invalid.IsError);
        Assert.IsNotNull(invalid.Exception);
    }

    [TestMethod]
    public async Task NativeTransferRejectsPrecisionLossAndUnrepresentableAmounts()
    {
        var provider = new SuiOASIS("http://127.0.0.1:1", "localnet", privateKey: "not-needed-for-preflight");
        var address = "0x" + new string('0', 63) + "1";
        foreach (var amount in new[] { 0m, -1m, 0.0000000001m, decimal.MaxValue })
        {
            var invalid = await provider.SendTransactionAsync(address, address, amount);
            Assert.IsTrue(invalid.IsError);
            Assert.IsNotNull(invalid.Exception);
            Assert.IsNull(invalid.Result);
        }
        var memo = await provider.SendTransactionAsync(address, address, 1, "cannot silently discard this");
        Assert.IsTrue(memo.IsError);
        StringAssert.Contains(memo.Message, "no memo field");
    }
}

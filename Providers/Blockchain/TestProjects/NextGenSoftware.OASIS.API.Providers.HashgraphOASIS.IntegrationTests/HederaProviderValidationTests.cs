using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaProviderValidationTests
{
    [TestMethod]
    public async Task NativeNftModelIsAcceptedButForeignModelFailsBeforeSdkSubmission()
    {
        var provider = new HashgraphOASIS(network: "testnet");
        var request = new NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests.MintWeb3NFTRequest {
            CollectionPublicKey = "0.0.1001", NumberToMint = 0,
            NFTStandardType = Core.Enums.NFTStandardType.HederaHTS,
        };
        var native = await provider.MintNFTAsync(request);
        Assert.IsTrue(native.IsError);
        StringAssert.Contains(native.Message, "1 to 10");
        request.NFTStandardType = Core.Enums.NFTStandardType.ERC721;
        var foreign = await provider.MintNFTAsync(request);
        Assert.IsTrue(foreign.IsError);
        StringAssert.Contains(foreign.Message, "HederaHTS");
        Assert.IsNull(native.Result);
        Assert.IsNull(foreign.Result);
    }

    [TestMethod]
    public async Task MintRequiresActualAssetAmountAndAvatarBeforeWalletLookup()
    {
        var provider = new HashgraphOASIS(network: "testnet");
        var missingAsset = await provider.MintTokenAsync(new MintWeb3TokenRequest { Amount = 1m });
        Assert.IsTrue(missingAsset.IsError);
        Assert.IsNull(missingAsset.Result);
        StringAssert.Contains(missingAsset.Message, "TokenAddress");
        var zeroAmount = await provider.MintTokenAsync(new MintWeb3TokenRequest { Amount = 0m });
        Assert.IsTrue(zeroAmount.IsError);
        Assert.IsNull(zeroAmount.Result);
        var missingAvatar = await provider.MintTokenAsync(new MintWeb3TokenRequest
            { Amount = 1m, MetaData = new Dictionary<string, string> { ["TokenAddress"] = "0.0.1234" } });
        Assert.IsTrue(missingAvatar.IsError);
        Assert.IsNull(missingAvatar.Result);
        StringAssert.Contains(missingAvatar.Message, "avatar");
    }
    [TestMethod]
    public async Task InvalidReceiptIdentifierReturnsErrorRatherThanNotFound()
    {
        var result = await new HashgraphOASIS(network: "testnet").GetTransactionStatusAsync("not-a-transaction-id");
        Assert.IsTrue(result.IsError, "SDK parsing failures must not be disguised as authoritative NotFound.");
    }
    [TestMethod]
    public void LocalNetworkRequiresExplicitConsensusNodes()
    {
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "local"));
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "typo"));
    }

    [DataTestMethod]
    [DataRow("remote.example:50211", "0.0.3")]
    [DataRow("127.0.0.1:0", "0.0.3")]
    [DataRow("127.0.0.1:70000", "0.0.3")]
    [DataRow("127.0.0.1:50211", "invalid")]
    public void LocalConsensusConfigurationRejectsInvalidTargets(string endpoint, string account)
    {
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "local",
            consensusNodes: new Dictionary<string, string> { [endpoint] = account }));
    }

    [TestMethod]
    public void PublicNetworkCannotSilentlyUseCustomNodes()
    {
        Assert.ThrowsException<ArgumentException>(() => new HashgraphOASIS(network: "testnet",
            consensusNodes: new Dictionary<string, string> { ["127.0.0.1:50211"] = "0.0.3" }));
    }

    [TestMethod]
    public async Task NativeAmountPrecisionIsRejectedBeforeSdkSubmission()
    {
        var provider = new HashgraphOASIS(network: "testnet");
        var result = await provider.SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002",
            Amount = 0.000000001m, OwnerPrivateKey = "synthetic-invalid-key"
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message, "tinybars");
    }

    [TestMethod]
    public async Task MissingKeyMustNotReturnTransactionSuccess()
    {
        var result = await new HashgraphOASIS(network: "testnet").SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1m
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task InvalidSdkKeyErrorsDoNotExposePrivateInput()
    {
        const string secret = "synthetic-private-key-input-never-log";
        var result = await new HashgraphOASIS(network: "testnet").SendTokenAsync(new SendWeb3TokenRequest
        {
            FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1m, OwnerPrivateKey = secret
        });
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.IsFalse(result.Message.Contains(secret));
        Assert.IsFalse((result.Exception?.ToString() ?? "").Contains(secret));
        StringAssert.Contains(result.Message, "0.0.1001@");
    }
}

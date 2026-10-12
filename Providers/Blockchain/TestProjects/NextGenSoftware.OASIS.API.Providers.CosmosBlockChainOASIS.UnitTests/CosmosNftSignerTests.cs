using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosNftSignerTests
{
    [DataTestMethod]
    [DataRow("a", false)]
    [DataRow("b", true)]
    [DataRow("c", true)]
    public async Task SuppliedNftCustodyProofMustAgreeBeforeRelease(string digestCharacter, bool rejected)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "nft-signer.mjs"));
        try
        {
            using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "test-only-explicit", "fixture-contract");
            var result = await provider.DepositNFTAsync("fixture-contract#fixture-token", "fixture-token", "fixture-recipient", new string(digestCharacter[0], 64));
            Assert.AreEqual(rejected, result.IsError, result.Message);
            if (rejected) Assert.IsNull(result.Result);
            else Assert.AreEqual("fixture-committed-cosmos-nft", result.Result.TransactionId);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", previous); }
    }

    [DataTestMethod]
    [DataRow(false, null)] [DataRow(false, "fixture-public")] [DataRow(false, "wrong-public")]
    [DataRow(true, null)] [DataRow(true, "fixture-public")] [DataRow(true, "wrong-public")]
    public async Task BurnValidatesPublicKeyWithoutTreatingItAsWalletAddress(bool token, string? publicKey)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "nft-signer.mjs"));
        try
        {
            using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "different-configured-key", "fixture-contract");
            if (token)
            {
                var result = await provider.BurnTokenAsync(new Core.Objects.Wallet.Requests.BurnWeb3TokenRequest {
                    TokenAddress = "fixture-token", Amount = 1, OwnerPrivateKey = "test-only-explicit", OwnerSeedPhrase = null, OwnerPublicKey = publicKey });
                Assert.AreEqual(publicKey == "wrong-public", result.IsError, result.Message);
                if (publicKey == "wrong-public") { Assert.IsNull(result.Result); StringAssert.Contains(result.Message, "public key does not match"); }
                else Assert.AreEqual("fixture-committed-cosmos-nft", result.Result.TransactionResult);
            }
            else
            {
                var result = await provider.BurnNFTAsync(new BurnWeb3NFTRequest {
                    NFTTokenAddress = "fixture-contract#fixture-token", OwnerPrivateKey = "test-only-explicit", OwnerSeedPhrase = null, OwnerPublicKey = publicKey });
                Assert.AreEqual(publicKey == "wrong-public", result.IsError, result.Message);
                if (publicKey == "wrong-public") { Assert.IsNull(result.Result); StringAssert.Contains(result.Message, "public key does not match"); }
                else Assert.AreEqual("fixture-committed-cosmos-nft", result.Result.TransactionResult);
            }
        }
        finally { Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", previous); }
    }

    [DataTestMethod]
    [DataRow(null, "fixture-contract#fixture-token", false)]
    [DataRow(" ", "fixture-contract#fixture-token", false)]
    [DataRow("fixture-contract#fixture-token", "fixture-contract#fixture-token", false)]
    [DataRow("fixture-contract#different-token", "fixture-contract#fixture-token", true)]
    [DataRow(" ", null, true)]
    public async Task NftSendRequiresAgreementBetweenNonblankAddressSelectors(string? first, string? second, bool rejected)
    {
        var previous = Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", Path.Combine(AppContext.BaseDirectory, "Fixtures", "nft-signer.mjs"));
        try
        {
            using var provider = new CosmosBlockChainOASIS("http://127.0.0.1:1", "fixture-chain", "different-configured-key", "fixture-contract");
            var result = await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenAddress = first, FromNFTTokenAddress = second,
                FromWalletAddress = "fixture-owner", ToWalletAddress = "fixture-recipient", OwnerPrivateKey = "test-only-explicit", Amount = 1 });
            Assert.IsTrue(result.IsError);
            if (rejected) { Assert.IsNull(result.Result); StringAssert.Contains(result.Message, "consistent Cosmos NFT address"); }
            else { Assert.AreEqual("fixture-committed-cosmos-nft", result.Result.TransactionResult); StringAssert.Contains(result.Message, "readback failed"); }
        }
        finally { Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE", previous); }
    }

    [DataTestMethod]
    [DataRow("", "", "fixture-owner", null)]
    [DataRow("fixture-recipient", "fixture-recipient", "fixture-recipient", null)]
    [DataRow("fixture-recipient", "another-recipient", null, "different Cosmos wallets")]
    [DataRow("fixture-recipient", " ", null, "has no address")]
    public void MintRecipientResolutionRequiresAgreement(string first, string second, string? expected, string? error)
    {
        var addresses=first.Length==0 ? new List<string>() : new List<string> { first,second };
        var method=typeof(CosmosBlockChainOASIS).GetMethod("ResolveMintRecipientAddresses",
            System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
        if(error==null)
            Assert.AreEqual(expected,method.Invoke(null,new object[] { addresses,"fixture-owner" }));
        else
        {
            var failure=Assert.ThrowsException<System.Reflection.TargetInvocationException>(
                ()=>method.Invoke(null,new object[] { addresses,"fixture-owner" }));
            StringAssert.Contains(failure.InnerException!.Message,error);
        }
    }

    [TestMethod]
    public async Task MintPreservesEconomicAndSaleMetadataInSerializedRequest()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try
        {
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain","test-only-explicit","fixture-contract");
            var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Title="Economic metadata",Price=12.5m,Discount=2m,
                RoyaltyPercentage=7,IsForSale=true,SaleStartDate=DateTime.UnixEpoch,SaleEndDate=DateTime.UnixEpoch.AddDays(1),MemoText="mint memo" });
            Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result.Web3NFT);
            Assert.AreEqual(12.5m,result.Result.Web3NFT.Price);
            Assert.AreEqual(7,result.Result.Web3NFT.RoyaltyPercentage);
            Assert.AreEqual(700u,result.Result.Web3NFT.SellerFeeBasisPoints);
            Assert.AreEqual(DateTime.UnixEpoch,result.Result.Web3NFT.SaleStartDate);
        }
        finally {Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 101)]
    [DataRow(0, -1)]
    public async Task InvalidMintEconomicsAreRejectedBeforeSdkAccess(int price, int royalty)
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Price=price,RoyaltyPercentage=royalty });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message,"royalty percentage");
    }

    [TestMethod]
    public async Task MintRejectsForeignOnChainProviderBeforeSdkAccess()
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { OnChainProvider=Core.Enums.ProviderType.SuiOASIS });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message,"OnChainProvider");
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("fixture-public")]
    [DataRow("wrong-public")]
    public async Task ExplicitSignerOverridesConfiguredKeyAndPublicMismatchFailsBeforeTransfer(string? publicKey)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try
        {
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain","different-configured-key","fixture-contract");
            var result=await provider.SendNFTAsync(new SendWeb3NFTRequest { FromWalletAddress="fixture-owner",ToWalletAddress="fixture-recipient",
                FromNFTTokenAddress="fixture-contract#fixture-token",OwnerPrivateKey="test-only-explicit",OwnerPublicKey=publicKey,Amount=1 });
            Assert.IsTrue(result.IsError);
            if(publicKey=="wrong-public")
            {
                Assert.IsNull(result.Result);StringAssert.Contains(result.Message,"public key does not match");
            }
            else
            {
                Assert.IsNotNull(result.Result);Assert.AreEqual("fixture-committed-cosmos-nft",result.Result.TransactionResult);
                Assert.AreEqual("fixture-committed-cosmos-nft",result.Result.SendNFTTransactionResult);
                StringAssert.Contains(result.Message,"fixture-committed-cosmos-nft");
            }
            Assert.IsFalse(result.Message.Contains("test-only-explicit"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }
}

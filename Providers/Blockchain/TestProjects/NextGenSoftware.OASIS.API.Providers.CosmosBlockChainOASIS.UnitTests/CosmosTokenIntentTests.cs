using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public sealed class CosmosTokenIntentTests
{
    [DataTestMethod]
    [DataRow(false, false)] [DataRow(true, false)] [DataRow(false, true)] [DataRow(true, true)]
    public async Task MintAndBurnPreserveOriginalSymbolAndMintMetadata(bool synchronous, bool burning)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try {
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain","test-only-explicit","fixture-contract");
            var mint=new MintWeb3TokenRequest { Symbol="intent-original",Amount=1.23m,Title="original-title",MemoText="Original seed 🌱 memo",
                MetaData=new Dictionary<string,string>{{"canary","original"}} };
            var burn=new BurnWeb3TokenRequest { TokenAddress="intent-original",Amount=1.23m,
                OwnerPrivateKey="test-only-explicit",OwnerPublicKey="fixture-public",OwnerSeedPhrase="" };
            NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse> result;
            if(synchronous)result=burning?provider.BurnToken(burn):provider.MintToken(mint);
            else {
                var pending=burning?provider.BurnTokenAsync(burn):provider.MintTokenAsync(mint);
                Assert.IsFalse(pending.IsCompleted,"Must exercise signer await.");
                mint.Symbol="intent-mutated";mint.Title="changed";mint.Amount=99;mint.MetaData["canary"]="changed";
                mint.MemoText="changed";
                burn.TokenAddress="intent-mutated";burn.Amount=99;
                result=await pending;
            }
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(new string('a',64),result.Result.TransactionResult);
            Assert.IsFalse(provider.IsProviderActivated);
        }finally{Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }
    [TestMethod]
    public async Task MintRejectsMalformedUnicodeBeforeSignerLookup()
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.MintTokenAsync(new MintWeb3TokenRequest { Symbol="TREE",Amount=1,MemoText="\ud800" });
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(System.Text.EncoderFallbackException));
        Assert.IsNull(result.Result);Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidUtf16MemoIsRejectedBeforeSignerLookup(bool native)
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest{FromTokenAddress=native?"uatom":"custom",FromWalletAddress="sender",
            ToWalletAddress="recipient",Amount=1,MemoText="\ud800"});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(System.Text.EncoderFallbackException));Assert.IsNull(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow("-1")] [DataRow("0.000000001")]
    public async Task InvalidCustomAmountIsRejectedBeforeSignerLookup(string amount)
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest{FromTokenAddress="custom",FromWalletAddress="sender",ToWalletAddress="recipient",
            Amount=decimal.Parse(amount,System.Globalization.CultureInfo.InvariantCulture)});
        Assert.IsTrue(result.IsError);Assert.IsTrue(result.Exception is ArgumentException);Assert.IsNull(result.Result);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task CustomTokenSendPreservesMemoAndIntentBeforeSignerAwait(bool synchronous)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try{
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain","different-configured-key","fixture-contract");
            var request=new SendWeb3TokenRequest{FromTokenAddress="intent-original",FromWalletAddress="fixture-owner",ToWalletAddress="fixture-recipient",
                Amount=1.23m,OwnerPrivateKey="test-only-explicit",OwnerPublicKey="fixture-public",MemoText="Original seed 🌱 memo"};
            NextGenSoftware.OASIS.Common.OASISResult<Core.Interfaces.Wallet.Responses.ITransactionResponse> result;
            if(synchronous)result=provider.SendToken(request);
            else{
                var pending=provider.SendTokenAsync(request);Assert.IsFalse(pending.IsCompleted,"Fixture must exercise signer await.");
                request.Amount=99;request.FromTokenAddress="intent-mutated";request.FromWalletAddress="changed-owner";
                request.ToWalletAddress="changed-recipient";request.MemoText="changed";
                result=await pending;
            }
            Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);
            Assert.AreEqual(new string('a',64),result.Result.TransactionResult);Assert.IsFalse(provider.IsProviderActivated);
        }finally{Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }
}

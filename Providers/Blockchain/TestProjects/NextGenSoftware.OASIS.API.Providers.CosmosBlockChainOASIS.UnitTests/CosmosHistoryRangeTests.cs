using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public sealed class CosmosHistoryRangeTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InclusiveRangeIsForwardedThroughPublicInterface(bool synchronous)
    {
        var prior=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","history-range.mjs"));
        try{
            using var provider=new CosmosBlockChainOASIS();IOASISBlockchainStorageProvider api=provider;
            var request=new GetWeb3TransactionsRequest{WalletAddress="controlled-wallet",FromBlock=40,ToBlock=45};
            var result=synchronous?api.GetTransactions(request):await api.GetTransactionsAsync(request);
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);
            Assert.IsFalse(provider.IsProviderActivated);
        }finally{Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",prior);}
    }

    [DataTestMethod]
    [DataRow(false,"46","45")] [DataRow(true,"46","45")]
    [DataRow(false,"9007199254740992","9007199254740992")] [DataRow(true,"9007199254740992","9007199254740992")]
    [DataRow(false,"0","18446744073709551615")] [DataRow(true,"0","18446744073709551615")]
    public async Task ReversedOrInexactRangeIsRejectedBeforeSdk(bool synchronous,string from,string to)
    {
        using var provider=new CosmosBlockChainOASIS(rpcEndpoint:"http://127.0.0.1:1");IOASISBlockchainStorageProvider api=provider;
        var request=new GetWeb3TransactionsRequest{WalletAddress="controlled-wallet",FromBlock=ulong.Parse(from),ToBlock=ulong.Parse(to)};
        var result=synchronous?api.GetTransactions(request):await api.GetTransactionsAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}

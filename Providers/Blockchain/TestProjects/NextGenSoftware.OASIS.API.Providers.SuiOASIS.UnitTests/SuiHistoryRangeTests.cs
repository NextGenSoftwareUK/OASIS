using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public sealed class SuiHistoryRangeTests
{
    [DataTestMethod]
    [DataRow(false,"40","45","1")] [DataRow(true,"40","45","1")]
    [DataRow(false,"0","0","2")] [DataRow(true,"0","0","2")]
    [DataRow(false,"9007199254740993","18446744073709551615","3")] [DataRow(true,"9007199254740993","18446744073709551615","3")]
    [DataRow(false,"","","4")] [DataRow(true,"","","4")]
    public async Task PublicCheckpointRangePreservesExactU64AndOptionalBounds(bool synchronous,string from,string to,string suffix)
    {
        var prior=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","history-range.mjs"));
        try{
            using var provider=new SuiOASIS(rpcEndpoint:"http://127.0.0.1:1");IOASISBlockchainStorageProvider api=provider;
            var request=new GetWeb3TransactionsRequest{WalletAddress="0x"+new string('0',63)+suffix,
                FromBlock=from.Length==0?null:ulong.Parse(from),ToBlock=to.Length==0?null:ulong.Parse(to)};
            var result=synchronous?api.GetTransactions(request):await api.GetTransactionsAsync(request);
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(0,result.Result.Count);Assert.IsFalse(provider.IsProviderActivated);
        }finally{Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",prior);}
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ReversedCheckpointRangeFailsBeforeSdk(bool synchronous)
    {
        using var provider=new SuiOASIS(rpcEndpoint:"http://127.0.0.1:1");IOASISBlockchainStorageProvider api=provider;
        var request=new GetWeb3TransactionsRequest{WalletAddress="0x"+new string('1',64),FromBlock=46,ToBlock=45};
        var result=synchronous?api.GetTransactions(request):await api.GetTransactionsAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.IsFalse(provider.IsProviderActivated);
    }
}

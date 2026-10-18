using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass]
public class CosmosRetainedActivationTests
{
    [TestMethod]
    public async Task RetainedContractActivationChecksActualOwnerWithoutChangingBankBalance()
    {
        const string rpc="http://127.0.0.1:26657";const string chain="oasis-cosmos-local";
        var contract=Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_CONTRACT")??throw new InvalidOperationException("Existing retained contract required; do not deploy a replacement.");
        var key=string.Join(" ",Enumerable.Repeat("abandon",23))+" art"; // Public local development identity only.
        var owner=(await CosmosTransferTests.InvokeSdk("restoreKey",new{})).GetProperty("address").GetString()!;
        using var provider=new CosmosBlockChainOASIS(rpc,chain,key,contract,"wasm","0.025stake","stake",0);
        var before=await provider.GetAccountBalanceAsync(owner);Assert.IsFalse(before.IsError,before.Message);Assert.IsTrue(before.Result>0);
        var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Message);Assert.IsTrue(provider.IsProviderActivated);
        var after=await provider.GetAccountBalanceAsync(owner);Assert.IsFalse(after.IsError,after.Message);Assert.AreEqual(before.Result,after.Result);
        using var foreign=new CosmosBlockChainOASIS(rpc,chain,new string('0',63)+"2",contract,"wasm","0.025stake","stake",0);
        var rejected=await foreign.ActivateProviderAsync();Assert.IsTrue(rejected.IsError);Assert.IsFalse(foreign.IsProviderActivated);
        var unchanged=await provider.GetAccountBalanceAsync(owner);Assert.IsFalse(unchanged.IsError,unchanged.Message);Assert.AreEqual(before.Result,unchanged.Result);
        Console.WriteLine("Retained Cosmos owner activation and foreign signer rejection used read-only queries; exact stake balance unchanged.");
    }
}

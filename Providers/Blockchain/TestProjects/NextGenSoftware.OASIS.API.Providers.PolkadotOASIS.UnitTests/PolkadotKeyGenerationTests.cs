using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class PolkadotKeyGenerationTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task UnactivatedGenerationFailsWithoutTransport(bool synchronous,bool requestOverload)
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1");
        var result=requestOverload ? (synchronous ? provider.GenerateKeyPair(new GetWeb3WalletBalanceRequest()) : await provider.GenerateKeyPairAsync(new GetWeb3WalletBalanceRequest()))
            : (synchronous ? provider.GenerateKeyPair() : await provider.GenerateKeyPairAsync());
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task AllLegacyEntryPointsMapSdkValuesWithoutBitcoinArtifacts(bool synchronous,bool requestOverload)
        => await WithFixture("accepted",async provider=>{
            var result=requestOverload ? (synchronous ? provider.GenerateKeyPair(new GetWeb3WalletBalanceRequest()) : await provider.GenerateKeyPairAsync(new GetWeb3WalletBalanceRequest()))
                : (synchronous ? provider.GenerateKeyPair() : await provider.GenerateKeyPairAsync());
            Assert.IsFalse(result.IsError,result.Exception?.ToString());
            Assert.IsTrue(result.Result.PrivateKey=="0x"+new string('2',64),"Seed mapping differs (value withheld).");
            Assert.AreEqual("0x"+new string('3',64),result.Result.PublicKey);
            Assert.AreEqual("5"+new string('c',47),result.Result.WalletAddressLegacy);
            Assert.IsNull(result.Result.WalletAddressSegwitP2SH);
        });
    [DataTestMethod][DataRow("generation-error")][DataRow("generation-network")][DataRow("generation-crypto")]
    [DataRow("generation-prefix")][DataRow("generation-seed")][DataRow("generation-address")]
    public async Task InvalidSdkResponseCannotBecomeAUsableWallet(string mode)
        => await WithFixture(mode,async provider=>{
            var result=await provider.GenerateKeyPairAsync();Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        });
    [TestMethod]
    public async Task ExistingAddressIsNotSilentlyIgnoredOrReplaced()
    {
        using var provider=new PolkadotOASIS("http://127.0.0.1:1");PolkadotAssetMutationTests.Activate(provider);
        var result=await provider.GenerateKeyPairAsync(new GetWeb3WalletBalanceRequest{WalletAddress="existing"});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));
    }
    private static async Task WithFixture(string mode,Func<PolkadotOASIS,Task> check)
    {
        // Controlled replies only, NOT crypto proof. Actual SDK/signatures are tested independently.
        var bridge=Environment.GetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE");
        var outcome=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME");
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"submission-process-fixture.mjs"));
        Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",mode);
        try{using var provider=new PolkadotOASIS("http://127.0.0.1:1");PolkadotAssetMutationTests.Activate(provider);await check(provider);}
        finally{Environment.SetEnvironmentVariable("OASIS_POLKADOT_SDK_BRIDGE",bridge);Environment.SetEnvironmentVariable("OASIS_POLKADOT_TEST_OUTCOME",outcome);}
    }
}
